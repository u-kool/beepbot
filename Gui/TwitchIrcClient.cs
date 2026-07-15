using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;

namespace TwitchIrcMinimal.Gui;

public class TwitchIrcClient : IDisposable
{
    private TcpClient? _tcp;
    private SslStream? _ssl;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private CancellationTokenSource? _cts;
    private Task? _readLoop;
    private Task? _heartbeat;
    private bool _reconnecting;
    private string _channel = "";

    public bool IsConnected => _tcp?.Connected == true;

    public event Action<string>? Log;
    public event Action<string, string, string, bool, bool>? OnMessage;
    public event Action? OnDisconnected;

    public async Task ConnectAsync(string channel, CancellationToken ct = default)
    {
        await DisconnectAsync();

        _channel = channel;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = _cts.Token;

        var nick = $"justinfan{Random.Shared.Next(10000, 99999)}";
        Log?.Invoke($"Connecting as {nick} to #{channel}...");

        _tcp = new TcpClient();
        await _tcp.ConnectAsync("irc.chat.twitch.tv", 6697, token);

        _ssl = new SslStream(_tcp.GetStream(), false, (_, _, _, _) => true);
        await _ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = "irc.chat.twitch.tv",
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
        }, token);

        _reader = new StreamReader(_ssl, Encoding.UTF8);
        _writer = new StreamWriter(_ssl, new UTF8Encoding(false)) { AutoFlush = true };

        await _writer.WriteAsync($"CAP REQ :twitch.tv/tags twitch.tv/commands\r\n");
        await _writer.WriteAsync("PASS oauth:ANONYMOUS\r\n");
        await _writer.WriteAsync($"NICK {nick}\r\n");
        await _writer.WriteAsync($"JOIN #{channel}\r\n");

        var joinDeadline = DateTime.UtcNow.AddSeconds(5);
        bool joined = false;
        while (DateTime.UtcNow < joinDeadline)
        {
            string? response = null;
            try
            {
                using var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, readCts.Token);
                response = await _reader.ReadLineAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) { continue; }

            if (response == null)
                throw new Exception("Disconnected before JOIN response");

            Log?.Invoke($"IRC: {response}");

            if (response.Contains($"JOIN #{channel}"))
            {
                joined = true;
                break;
            }

            if (response.StartsWith(":tmi.twitch.tv 403") ||
                response.StartsWith(":tmi.twitch.tv 404") ||
                response.Contains("NOTICE") && response.Contains("not found"))
                throw new Exception($"Channel \"{channel}\" not found");
        }

        if (!joined)
            throw new Exception($"Channel \"{channel}\" — no JOIN response (timeout)");

        Log?.Invoke($"Connected to #{channel}!");
        _reconnecting = false;

        _heartbeat = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(4), token);
                    await _writer.WriteAsync("PING :tmi.twitch.tv\r\n");
                }
            }
            catch (OperationCanceledException) { }
            catch
            {
                Log?.Invoke("Heartbeat failed");
                _ = ReconnectAsync();
            }
        }, token);

        _readLoop = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var line = await _reader.ReadLineAsync(token);
                    if (line == null)
                    {
                        Log?.Invoke("[DISCONNECTED]");
                        _ = ReconnectAsync();
                        break;
                    }

                    if (line.StartsWith("PING"))
                    {
                        await _writer.WriteAsync("PONG :tmi.twitch.tv\r\n");
                        continue;
                    }

                    if (line.Contains(":tmi.twitch.tv RECONNECT"))
                    {
                        Log?.Invoke("[RECONNECT requested by server]");
                        _ = ReconnectAsync();
                        break;
                    }

                    var privmsg = ParsePrivmsg(line);
                    if (privmsg != null)
                    {
                        var tags = line.StartsWith('@') ? ParseTags(line) : new Dictionary<string, string>();
                        var displayName = tags.GetValueOrDefault("display-name", privmsg.Value.User);
                        var badges = tags.GetValueOrDefault("badges", "");
                        bool isBroadcaster = badges.Contains("broadcaster");
                        bool isMod = badges.Contains("moderator");
                        OnMessage?.Invoke(privmsg.Value.User, privmsg.Value.Text, displayName, isBroadcaster, isMod);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log?.Invoke($"Read error: {ex.Message}");
                _ = ReconnectAsync();
            }
        }, token);
    }

    private async Task ReconnectAsync()
    {
        if (_reconnecting || _cts?.IsCancellationRequested == true) return;
        _reconnecting = true;

        await DisconnectAsync();

        var delay = 2;
        for (int i = 0; i < 10; i++)
        {
            Log?.Invoke($"Reconnecting in {delay}s (attempt {i + 1}/10)...");
            await Task.Delay(TimeSpan.FromSeconds(delay));

            try
            {
                await ConnectAsync(_channel);
                Log?.Invoke("Reconnected!");
                return;
            }
            catch (Exception ex)
            {
                Log?.Invoke($"Reconnect failed: {ex.Message}");
                delay = Math.Min(delay * 2, 60);
            }
        }

        Log?.Invoke("Reconnect failed after 10 attempts");
        OnDisconnected?.Invoke();
    }

    public async Task DisconnectAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            if (_readLoop != null) try { await _readLoop; } catch { }
            if (_heartbeat != null) try { await _heartbeat; } catch { }
            _cts.Dispose();
            _cts = null;
        }
        _reader?.Dispose(); _reader = null;
        _writer?.Dispose(); _writer = null;
        _ssl?.Dispose(); _ssl = null;
        _tcp?.Dispose(); _tcp = null;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _reader?.Dispose();
        _writer?.Dispose();
        _ssl?.Dispose();
        _tcp?.Dispose();
    }

    private static Dictionary<string, string> ParseTags(string line)
    {
        var tags = new Dictionary<string, string>();
        var spaceIdx = line.IndexOf(' ');
        if (spaceIdx <= 0) return tags;
        foreach (var tag in line[1..spaceIdx].Split(';'))
        {
            var eqIdx = tag.IndexOf('=');
            if (eqIdx >= 0)
                tags[tag[..eqIdx]] = tag[(eqIdx + 1)..];
        }
        return tags;
    }

    private static (string User, string Text)? ParsePrivmsg(string line)
    {
        var match = Regex.Match(line, @":([^!]+)![^@]+@[^ ]+ PRIVMSG #\w+ :(.*)");
        return match.Success ? (match.Groups[1].Value, match.Groups[2].Value) : null;
    }
}
