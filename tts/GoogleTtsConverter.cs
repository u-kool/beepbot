using System.Net;
using System.Web;
using NAudio.Wave;

namespace TwitchIrcMinimal;

public static class GoogleTtsConverter
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

    public static async Task<float[]?> GetTtsAsync(string lang, string text, WaveFormat targetFormat)
    {
        var runes = text.EnumerateRunes().ToList();
        if (runes.Count > 200) runes = runes.Take(200).ToList();
        text = string.Concat(runes.Select(r => r.ToString()));

        var query = HttpUtility.ParseQueryString("");
        query["ie"] = "UTF-8";
        query["client"] = "tw-ob";
        query["tl"] = lang;
        query["q"] = text;
        var url = $"https://translate.google.com/translate_tts?{query}";

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("User-Agent", "Mozilla/5.0");
            var resp = await _http.SendAsync(req);
            if (resp.StatusCode != HttpStatusCode.OK) return null;

            var rawData = await resp.Content.ReadAsByteArrayAsync();
            using var ms = new MemoryStream(rawData);
            using var mp3 = new Mp3FileReader(ms);

            int mp3Channels = mp3.WaveFormat.Channels;
            var resampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(targetFormat.SampleRate, mp3Channels);
            using var resampler = new MediaFoundationResampler(mp3, resampleFormat) { ResamplerQuality = 60 };

            var list = new List<float>();
            var byteBuffer = new byte[targetFormat.SampleRate * mp3Channels * 4];
            int bytesRead;
            while ((bytesRead = resampler.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
            {
                int floatCount = bytesRead / 4;
                for (int i = 0; i < floatCount; i++)
                    list.Add(BitConverter.ToSingle(byteBuffer, i * 4));
            }

            // Ensure stereo output: upmix mono to stereo
            if (mp3Channels == 1 && list.Count > 0)
            {
                var stereo = new List<float>(list.Count * 2);
                foreach (var s in list) { stereo.Add(s); stereo.Add(s); }
                return stereo.ToArray();
            }
            return list.ToArray();
        }
        catch { return null; }
    }
}
