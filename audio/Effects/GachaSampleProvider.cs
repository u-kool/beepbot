using NAudio.Wave;

namespace TwitchIrcMinimal;

public class GachaSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly List<Func<float[], int, int, int, int>> _transforms = new();
    public WaveFormat WaveFormat => _source.WaveFormat;

    public GachaSampleProvider(ISampleProvider source, bool erEnabled)
    {
        _source = source;
        var rng = Random.Shared;

        var candidates = new List<string> { "reversed", "stutter", "lowQuality", "delay", "vibrato", "speed" };
        if (erEnabled) candidates.Add("earRape");

        int limit = 3;
        limit = IsCrit(limit, rng);
        if (limit == 0) return;

        int finalCount = GetFinalCount(limit, rng);
        var shuffled = candidates.OrderBy(_ => rng.Next()).Take(finalCount);

        foreach (var eff in shuffled)
        {
            switch (eff)
            {
                case "lowQuality":
                    _transforms.Add((buf, off, cnt, ch) =>
                    {
                        int frames = cnt / ch;
                        int counter = 0;
                        float holdL = 0, holdR = 0;
                        for (int f = 0; f < frames; f++)
                        {
                            int idx = off + f * ch;
                            if (counter == 0 || counter == 8)
                            {
                                holdL = buf[idx];
                                holdR = (ch >= 2) ? buf[idx + 1] : holdL;
                                counter = 0;
                            }
                            else
                            {
                                buf[idx] = holdL;
                                if (ch >= 2) buf[idx + 1] = holdR;
                            }
                            buf[idx] = MathF.Round(buf[idx] * 64f) / 64f;
                            if (ch >= 2) buf[idx + 1] = MathF.Round(buf[idx + 1] * 64f) / 64f;
                            counter++;
                        }
                        return cnt;
                    });
                    break;
                case "vibrato":
                    _transforms.Add((buf, off, cnt, ch) =>
                    {
                        int frames = cnt / ch;
                        const float freq = 8f;
                        const float depth = 0.08f;
                        int sampleRate = WaveFormat.SampleRate;
                        float[] circularL = new float[1024];
                        float[] circularR = new float[1024];
                        int writeCount = 0;
                        float pos = 0f;

                        for (int f = 0; f < frames; f++)
                        {
                            while (writeCount <= (int)pos + 1)
                            {
                                int srcFrame = writeCount;
                                if (srcFrame < frames)
                                {
                                    int si = off + srcFrame * ch;
                                    circularL[writeCount & 1023] = buf[si];
                                    if (ch >= 2)
                                        circularR[writeCount & 1023] = buf[si + 1];
                                }
                                else
                                {
                                    circularL[writeCount & 1023] = 0f;
                                    circularR[writeCount & 1023] = 0f;
                                }
                                writeCount++;
                            }

                            int idx1 = (int)pos;
                            int idx2 = idx1 + 1;
                            int wrap1 = ((idx1 % 1024) + 1024) % 1024;
                            int wrap2 = ((idx2 % 1024) + 1024) % 1024;
                            float frac = pos - idx1;

                            int oi = off + f * ch;
                            buf[oi] = circularL[wrap1] * (1f - frac) + circularL[wrap2] * frac;
                            if (ch >= 2)
                                buf[oi + 1] = circularR[wrap1] * (1f - frac) + circularR[wrap2] * frac;

                            float timeSec = (float)f / sampleRate;
                            float step = MathF.Sin(2f * MathF.PI * freq * timeSec) * depth + 1f;
                            pos += step;
                        }
                        return cnt;
                    });
                    break;
                case "earRape":
                    _transforms.Add((buf, off, cnt, _) =>
                    {
                        for (int i = off; i < off + cnt; i++)
                        {
                            if (buf[i] < -0.05f) buf[i] = -0.05f;
                            if (buf[i] > 0.05f) buf[i] = 0.05f;
                            buf[i] *= 5.0f;
                        }
                        return cnt;
                    });
                    break;
                case "delay":
                    _transforms.Add((buf, off, cnt, ch) =>
                    {
                        const int delayBufSize = 8820;
                        const float dryGain = 0.85f;
                        const float wetGain = 0.4f;
                        float[] lBuf = new float[delayBufSize];
                        float[] rBuf = new float[delayBufSize];
                        int counter = 0;
                        int frames = cnt / ch;

                        for (int f = 0; f < frames; f++)
                        {
                            int idx = off + f * ch;
                            float inputL = buf[idx];
                            float inputR = (ch >= 2) ? buf[idx + 1] : inputL;

                            float echoL = lBuf[counter];
                            float echoR = rBuf[counter];

                            float outputL = inputL * dryGain + echoL * wetGain;
                            float outputR = inputR * dryGain + echoR * wetGain;

                            rBuf[counter] = outputL;
                            lBuf[counter] = outputR;

                            buf[idx] = outputL;
                            if (ch >= 2) buf[idx + 1] = outputR;

                            counter++;
                            if (counter >= delayBufSize) counter = 0;
                        }
                        return cnt;
                    });
                    break;
                case "reversed":
                    _transforms.Add((buf, off, cnt, _) =>
                    {
                        Array.Reverse(buf, off, cnt);
                        return cnt;
                    });
                    break;
                case "stutter":
                    _transforms.Add((buf, off, cnt, ch) =>
                    {
                        int chunkLen = (int)(WaveFormat.SampleRate * ch * 0.140);
                        if (chunkLen > cnt) chunkLen = cnt;
                        var original = new float[cnt];
                        Array.Copy(buf, off, original, 0, cnt);
                        int pos = 0;
                        for (int rep = 0; rep < 3 && pos < cnt; rep++)
                            for (int j = 0; j < chunkLen && pos < cnt; j++)
                                buf[off + pos++] = original[j];
                        while (pos < cnt)
                        {
                            buf[off + pos] = original[pos - chunkLen * 3 + chunkLen * 3];
                            pos++;
                        }
                        return cnt;
                    });
                    break;
                case "speed":
                    float spd = RandomSpeedRatio(rng) / 100f;
                    _transforms.Add((buf, off, cnt, ch) =>
                    {
                        var original = new float[cnt];
                        Array.Copy(buf, off, original, 0, cnt);
                        int srcFrames = cnt / ch;
                        int written = 0;
                        float pos = 0;
                        while (written + ch <= cnt && (int)pos + 1 < srcFrames)
                        {
                            int srcIdx = (int)pos * ch;
                            float frac = pos - (int)pos;
                            for (int c = 0; c < ch && written + c < cnt; c++)
                            {
                                float s0 = original[srcIdx + c];
                                float s1 = original[Math.Min(srcIdx + ch + c, cnt - 1)];
                                buf[off + written + c] = s0 + (s1 - s0) * frac;
                            }
                            written += ch;
                            pos += spd;
                        }
                        while (written < cnt) { buf[off + written] = 0; written++; }
                        return cnt;
                    });
                    break;
            }
        }
    }

    private static int IsCrit(int num, Random rng)
    {
        int roll = rng.Next(1, 101);
        return roll <= 5 ? num + 1 : num;
    }

    private static int GetFinalCount(int n, Random rng)
    {
        int roll = rng.Next(1, 101);
        return n switch
        {
            1 => 1,
            2 => roll <= 70 ? 1 : 2,
            3 => roll <= 50 ? 1 : roll <= 85 ? 2 : 3,
            4 => roll <= 30 ? 2 : roll <= 80 ? 3 : 4,
            _ => 0
        };
    }

    private static int RandomSpeedRatio(Random rng)
    {
        int roll = rng.Next(1, 101);
        if (roll <= 45) return rng.Next(50, 81);
        if (roll <= 85) return rng.Next(120, 171);
        return rng.Next(20, 46);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        if (_transforms.Count == 0) return read;
        foreach (var t in _transforms)
            t(buffer, offset, read, WaveFormat.Channels);
        return read;
    }
}
