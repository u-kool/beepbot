namespace TwitchIrcMinimal;

public enum TaskType { None, Sound, Tts }

public class PlayTask
{
    public TaskType Type { get; set; }
    public string Content { get; set; } = "";
    public string? Lang { get; set; }
    public string? Effects { get; set; }
    public bool Mix { get; set; }
}

public static class TaskParser
{
    public static List<PlayTask> Parse(string[] words, Bot bot)
    {
        var tasks = new List<PlayTask>();
        int i = 0;
        while (i < words.Length)
        {
            var word = words[i];

            // Handle + mixing: tts-vb+car-dl → ["tts-vb", "car-dl"]
            if (word.Contains('+'))
            {
                var subTokens = word.Split('+');
                foreach (var sub in subTokens)
                {
                    var (type, ok) = Classify(sub, bot);
                    if (!ok) continue;
                    var parts = sub.Split('-', 2);
                    if (type == TaskType.Sound)
                    {
                        tasks.Add(new PlayTask
                        {
                            Type = TaskType.Sound,
                            Content = parts[0],
                            Effects = parts.Length == 2 ? parts[1] : null,
                            Mix = true
                        });
                    }
                    else if (type == TaskType.Tts)
                    {
                        var tParts = sub.Split('-', 2);
                        string? langCode = null;
                        foreach (var lang in tParts[0].Split('+'))
                        {
                            if (bot.TtsLanguages.TryGetValue(lang.ToLowerInvariant(), out var code))
                            { langCode = code; break; }
                        }
                        string? ttsEffects = tParts.Length == 2 ? tParts[1] : null;
                        var textParts = new List<string>();
                        int j = i + 1;
                        while (j < words.Length)
                        {
                            var nextWord = words[j];
                            if (nextWord.Contains('+'))
                            {
                                var innerSubs = nextWord.Split('+');
                                bool allSound = true;
                                foreach (var innerSub in innerSubs)
                                {
                                    var (innerType, innerOk) = Classify(innerSub, bot);
                                    if (innerOk && innerType == TaskType.Sound)
                                    {
                                        var innerParts = innerSub.Split('-', 2);
                                        tasks.Add(new PlayTask
                                        {
                                            Type = TaskType.Sound,
                                            Content = innerParts[0],
                                            Effects = innerParts.Length == 2 ? innerParts[1] : null,
                                            Mix = true
                                        });
                                        allSound = false;
                                    }
                                    else if (innerOk && innerType == TaskType.Tts)
                                    {
                                        if (textParts.Count > 0)
                                        {
                                            tasks.Add(new PlayTask
                                            {
                                                Type = TaskType.Tts,
                                                Content = string.Join(" ", textParts),
                                                Lang = langCode,
                                                Effects = ttsEffects,
                                                Mix = true
                                            });
                                        }
                                        var itParts = innerSub.Split('-', 2);
                                        langCode = null;
                                        foreach (var lang in itParts[0].Split('+'))
                                        {
                                            if (bot.TtsLanguages.TryGetValue(lang.ToLowerInvariant(), out var code))
                                            { langCode = code; break; }
                                        }
                                        ttsEffects = itParts.Length == 2 ? itParts[1] : null;
                                        textParts = new List<string>();
                                        allSound = false;
                                    }
                                    else
                                    {
                                        textParts.Add(innerSub);
                                        allSound = false;
                                    }
                                }
                                j++;
                                if (!allSound) continue;
                                break;
                            }
                            var (_, ok2) = Classify(nextWord, bot);
                            if (!ok2) { textParts.Add(nextWord); j++; }
                            else break;
                        }
                        tasks.Add(new PlayTask
                        {
                            Type = TaskType.Tts,
                            Content = string.Join(" ", textParts),
                            Lang = langCode,
                            Effects = ttsEffects,
                            Mix = true
                        });
                        i = j - 1;
                        break;
                    }
                }
                i++;
                continue;
            }

            var (mainType, mainOk) = Classify(word, bot);
            if (!mainOk) { i++; continue; }

            if (mainType == TaskType.Sound)
            {
                var parts = word.Split('-', 2);
                tasks.Add(new PlayTask
                {
                    Type = TaskType.Sound,
                    Content = parts[0],
                    Effects = parts.Length == 2 ? parts[1] : null
                });
                i++;
                continue;
            }

            if (mainType == TaskType.Tts)
            {
                var initParts = word.Split('-', 2);
                string? langCode = null;
                foreach (var lang in initParts[0].Split('+'))
                {
                    if (bot.TtsLanguages.TryGetValue(lang.ToLowerInvariant(), out var code))
                    { langCode = code; break; }
                }
                string? effects = initParts.Length == 2 ? initParts[1] : null;
                var textParts = new List<string>();
                bool hasMixed = false;
                int j = i + 1;
                while (j < words.Length)
                {
                    var nextWord = words[j];
                    if (nextWord.Contains('+'))
                    {
                        var subTokens = nextWord.Split('+');
                        bool allSound = true;
                        foreach (var sub in subTokens)
                        {
                            var (subType, subOk) = Classify(sub, bot);
                            if (subOk && subType == TaskType.Sound)
                            {
                                var subParts = sub.Split('-', 2);
                                tasks.Add(new PlayTask
                                {
                                    Type = TaskType.Sound,
                                    Content = subParts[0],
                                    Effects = subParts.Length == 2 ? subParts[1] : null,
                                    Mix = true
                                });
                                hasMixed = true;
                                allSound = false;
                            }
                            else if (subOk && subType == TaskType.Tts)
                            {
                                if (textParts.Count > 0)
                                {
                                    tasks.Add(new PlayTask
                                    {
                                        Type = TaskType.Tts,
                                        Content = string.Join(" ", textParts),
                                        Lang = langCode,
                                        Effects = effects,
                                        Mix = true
                                    });
                                }
                                var tParts = sub.Split('-', 2);
                                langCode = null;
                                foreach (var lang in tParts[0].Split('+'))
                                {
                                    if (bot.TtsLanguages.TryGetValue(lang.ToLowerInvariant(), out var code))
                                    { langCode = code; break; }
                                }
                                effects = tParts.Length == 2 ? tParts[1] : null;
                                textParts = new List<string>();
                                hasMixed = true;
                                allSound = false;
                            }
                            else
                            {
                                textParts.Add(sub);
                                allSound = false;
                            }
                        }
                        j++;
                        if (!allSound) continue;
                        break;
                    }
                    var (_, ok2) = Classify(nextWord, bot);
                    if (!ok2) { textParts.Add(nextWord); j++; }
                    else break;
                }
                tasks.Add(new PlayTask
                {
                    Type = TaskType.Tts,
                    Content = string.Join(" ", textParts),
                    Lang = langCode,
                    Effects = effects,
                    Mix = hasMixed
                });
                i = j;
            }
        }
        return tasks;
    }

    private static (TaskType, bool) Classify(string word, Bot bot)
    {
        var parts = word.Split('-', 2);
        var first = parts[0].ToLowerInvariant();
        foreach (var token in first.Split('+'))
        {
            if (token == "rand") return (TaskType.Sound, true);
            if (bot.Engine.GetSoundNames().Contains(token)) return (TaskType.Sound, true);
            if (bot.TtsLanguages.ContainsKey(token)) return (TaskType.Tts, true);
        }
        return (TaskType.None, false);
    }
}
