package bot

import (
	"beepbot/tts"
	"fmt"
	"strings"
	"sync"
)

type TaskType string

type PlayTask struct {
	Type    TaskType
	Content string
	Lang    string
	Effects string
}

type ttsTask struct {
	tempKey string
	req     *tts.Request
}

const (
	TaskSound TaskType = "sound"
	TaskTTS   TaskType = "tts"
	TaskNone  TaskType = "none"
)

func (b *Bot) isCommand(msg string) (TaskType, bool) {
	fullCommand := strings.SplitN(msg, "-", 2)
	comSlice := strings.Split(fullCommand[0], "+")

	for _, comm := range comSlice {
		comm = strings.ToLower(comm)
		_, isSound := b.soundsBuffer[comm]
		_, isTTS := b.ttsLanguages[comm]
		if isSound || comm == "rand" {
			return TaskSound, true
		}
		if isTTS {
			return TaskTTS, true
		}
	}
	return TaskNone, false
}

func (b *Bot) parseMessage(msgSlice []string) []PlayTask {
	b.mtx.RLock()
	defer b.mtx.RUnlock()
	taskSlice := make([]PlayTask, 0, len(msgSlice))
	i := 0
	for i < len(msgSlice) {
		taskType, ok := b.isCommand(msgSlice[i])
		if !ok {
			i++
			continue
		}
		if taskType == TaskSound {
			parts := strings.SplitN(msgSlice[i], "-", 2)

			task := PlayTask{
				Type:    TaskSound,
				Content: parts[0],
			}
			if len(parts) == 2 {
				task.Effects = parts[1]
			}
			taskSlice = append(taskSlice, task)
			i++
			continue
		}
		if taskType == TaskTTS {
			parts := strings.SplitN(msgSlice[i], "-", 2)
			sounds := strings.Split(parts[0], "+")
			task := PlayTask{
				Type: TaskTTS,
			}
			for _, lang := range sounds {
				if langCode, ok := b.ttsLanguages[lang]; ok {
					task.Lang = langCode
					break
				}
			}
			if len(parts) == 2 {
				task.Effects = parts[1]
			}
			textTts := []string{}
			j := i + 1
			for j < len(msgSlice) {
				if _, ok := b.isCommand(msgSlice[j]); !ok {
					textTts = append(textTts, msgSlice[j])
					j++
				} else {
					break
				}
			}
			i = j
			task.Content = strings.Join(textTts, " ")
			taskSlice = append(taskSlice, task)
		}
	}
	return taskSlice
}

func (b *Bot) resolveTasks(taskSlice []PlayTask) ([]PlayTask, []string) {
	var wg sync.WaitGroup
	keysToDelete := make([]string, 0, len(taskSlice))
	for i := range taskSlice {
		if taskSlice[i].Type == TaskTTS {
			eff, translated := tts.NeedTranslate(taskSlice[i].Effects)
			if translated {
				taskSlice[i].Effects = eff
				translation, err := tts.Translate(taskSlice[i].Lang, taskSlice[i].Content)
				if err == nil && translation != "" {
					taskSlice[i].Content = translation
				}
			}
			req := tts.New(taskSlice[i].Lang, taskSlice[i].Content)
			tempKey := fmt.Sprintf("tts_temp_%d", b.ttsCounter.Add(1))
			task := ttsTask{tempKey, req}
			taskSlice[i].Content = tempKey
			taskSlice[i].Type = TaskSound
			keysToDelete = append(keysToDelete, tempKey)
			wg.Add(1)
			go b.downloadTtsTask(task, &wg)
		}
	}
	wg.Wait()
	return taskSlice, keysToDelete
}

func (b *Bot) downloadTtsTask(t ttsTask, wg *sync.WaitGroup) {
	defer wg.Done()
	buf, err := t.req.ToBuffer()
	if err != nil {
		return
	}
	b.mtx.Lock()
	b.soundsBuffer[t.tempKey] = buf
	b.mtx.Unlock()
}
