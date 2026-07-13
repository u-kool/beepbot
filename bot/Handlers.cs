using NAudio.Wave;

namespace TwitchIrcMinimal;

public static class Handlers
{
    private static void Log(string msg) => Gui.Log.Info($"[BOT] {msg}");

    public static void HandleMessage(string user, string text, bool isBroadcaster, bool isMod, Bot bot, EnvConfig config)
    {
        if (text.Length < 2 || (!text.StartsWith('!') && !text.StartsWith('@'))) return;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return;

        if (words[0] == "!m")
            PlaySound(user, words[1..], isBroadcaster, isMod, bot, config);
    }

    private static void PlaySound(string user, string[] args, bool isBroadcaster, bool isMod, Bot bot, EnvConfig config)
    {
        if (args.Length == 0) return;
        var command = args[0].ToLowerInvariant();

        if (isBroadcaster || isMod)
        {
            switch (command)
            {
                case "mute":
                    bot.SetMuted(true);
                    Log("Muted");
                    return;
                case "unmute":
                    bot.SetMuted(false);
                    Log("Unmuted");
                    return;
                case "qon":
                    bot.QueueEnabled = true;
                    bot.Player.QueueEnabled = true;
                    Log("Queue ON");
                    return;
                case "qoff":
                    bot.QueueEnabled = false;
                    bot.Player.QueueEnabled = false;
                    Log("Queue OFF");
                    return;
                case "stop":
                    bot.Player.Stop();
                    Log("Stopped");
                    return;
                case "skip":
                    bot.Player.Skip();
                    Log("Skipped");
                    return;
                case "eron":
                    bot.ErEnabled = true;
                    bot.Engine.SetErEnabled(true);
                    Log("Ear safety ON");
                    return;
                case "eroff":
                    bot.ErEnabled = false;
                    bot.Engine.SetErEnabled(false);
                    Log("Ear safety OFF");
                    return;
                case "vol":
                    if (args.Length >= 2 && int.TryParse(args[1], out int vol))
                    {
                        vol = Math.Clamp(vol, 0, 200);
                        bot.Volume = vol;
                        config.SaveVolume(vol);
                        Log($"Volume: {vol}");
                    }
                    return;
            }
        }

        if (bot.IsMuted) return;

        var tasks = TaskParser.Parse(args, bot);
        if (tasks.Count == 0)
        {
            Log($"No tasks parsed from: {string.Join(" ", args)}");
            return;
        }

        Log($"Parsed {tasks.Count} task(s), processing...");
        _ = ProcessTasks(tasks, bot);
    }

    private static async Task ProcessTasks(List<PlayTask> tasks, Bot bot)
    {
        try
        {
            foreach (var task in tasks)
            {
                if (task.Type == TaskType.Tts && !string.IsNullOrEmpty(task.Content) && task.Lang != null)
                {
                    Log($"TTS: lang={task.Lang}, effects={task.Effects}, text={task.Content}");
                    var (eff, translated) = Translator.NeedTranslate(task.Effects);
                    task.Effects = eff;

                    string text = task.Content;
                    if (translated)
                    {
                        Log($"TTS: translating to {task.Lang}...");
                        var translatedText = await Translator.TranslateAsync(task.Lang, text);
                        if (!string.IsNullOrEmpty(translatedText))
                            text = translatedText;
                        Log($"TTS: translated -> {text}");
                    }

                    Log($"TTS: getting audio for '{text}'...");
                    var key = bot.NextTtsKey();
                    var format = bot.Engine.GetDefaultFormat();
                    var samples = await GoogleTtsConverter.GetTtsAsync(task.Lang, text, format);
                    if (samples != null)
                    {
                        bot.Engine.AddSamples(key, samples);
                        Log($"TTS samples loaded: {samples.Length}");
                    }
                    else
                    {
                        Log("TTS: Google returned null");
                    }

                    task.Content = key;
                    task.Type = TaskType.Sound;
                }
            }

            var soundCmds = tasks.Where(t => t.Type == TaskType.Sound)
                .Select(t => new PlayCommand { SoundNames = t.Content, Effects = t.Effects, Mix = t.Mix })
                .ToList();
            if (soundCmds.Count > 0)
            {
                foreach (var cmd in soundCmds)
                    Log($"Playing: {cmd.SoundNames} effects={cmd.Effects}");
                bot.Player.PlayAll(soundCmds, bot.Volume);
            }
        }
        catch (Exception ex)
        {
            Log($"ProcessTasks ERROR: {ex}");
        }
    }
}
