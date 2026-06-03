package bot

import (
	"beepbot/audio"
	"beepbot/tts"
	"fmt"
	"strings"
	"sync"

	"github.com/gempir/go-twitch-irc/v4"
	"github.com/gopxl/beep/v2"
	"github.com/gopxl/beep/v2/speaker"
)

type ttsTask struct {
	tempKey string
	req     *tts.Request
}

func (b *Bot) playSound(msg twitch.PrivateMessage) {
	msgSlice := strings.Fields(msg.Message)[1:]
	if len(msgSlice) == 0 {
		return
	}
	command := strings.ToLower(msgSlice[0])

	succeed := b.handleAdminCommand(msg, command)

	if succeed {
		return
	}

	if b.IsMuted() {
		return
	}

	rewrittenSlice, keysToDelete, tasks := b.parseWithTts(msgSlice)

	if len(keysToDelete) > 0 {
		defer b.deleteKeys(keysToDelete)
	}

	if len(tasks) > 0 {
		var wg sync.WaitGroup

		for _, task := range tasks {
			wg.Add(1)
			go b.downloadTtsTask(task, &wg)
		}
		wg.Wait()
	}

	finalStreamer := b.assembleStreamer(rewrittenSlice)
	if finalStreamer == nil {
		return
	}

	if b.IsQueueEnabled() {
		b.pushToQueue(finalStreamer)
	} else {
		speaker.Play(finalStreamer)
	}
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

func (b *Bot) handleAdminCommand(msg twitch.PrivateMessage, command string) bool {
	if msg.User.IsBroadcaster || msg.User.IsMod {
		switch command {
		case "mute":
			speaker.Clear()
			b.SetMuted(true)
			b.mtx.Lock()
			b.queue = b.queue[:0]
			b.queueIsPlaying = false
			b.mtx.Unlock()
			b.printState()
			return true
		case "unmute":
			b.SetMuted(false)
			b.printState()
			return true
		case "qon":
			b.SetQEnabled(true)
			b.printState()
			return true
		case "qoff":
			b.SetQEnabled(false)
			b.printState()
			return true
		case "stop":
			speaker.Clear()
			b.mtx.Lock()
			b.queue = b.queue[:0]
			b.queueIsPlaying = false
			b.mtx.Unlock()
			return true
		case "skip":
			speaker.Clear()
			if b.IsQueueEnabled() {
				b.mtx.Lock()
				if len(b.queue) > 0 {
					b.mtx.Unlock()
					b.playNext()
				} else {
					b.queueIsPlaying = false
					b.mtx.Unlock()
				}
			}
			return true
		case "eron":
			b.mtx.Lock()
			b.erIsOn = true
			b.mtx.Unlock()
			b.printState()
			return true
		case "eroff":
			b.mtx.Lock()
			b.erIsOn = false
			b.mtx.Unlock()
			b.printState()
			return true
		}
	}
	return false
}

func (b *Bot) assembleStreamer(msgSlice []string) beep.Streamer {
	b.mtx.RLock()
	defer b.mtx.RUnlock()
	streamersSlice := make([]beep.Streamer, 0, len(msgSlice))
	for _, s := range msgSlice {
		sound := audio.CreateSoundWithParam(s, b.soundsBuffer, b.erIsOn)
		streamer, err := audio.CreateStreamerWithParameter(sound, b.soundsBuffer)
		if err != nil {
			continue
		}
		streamersSlice = append(streamersSlice, streamer)
	}

	if len(streamersSlice) < 1 {
		return nil
	}

	return beep.Seq(streamersSlice...)
}

func (b *Bot) pushToQueue(s beep.Streamer) {
	b.mtx.Lock()
	if len(b.queue) >= 50 {
		b.mtx.Unlock()
		return
	}
	b.queue = append(b.queue, s)
	if !b.queueIsPlaying {
		b.queueIsPlaying = true
		b.mtx.Unlock()
		b.playNext()
		return
	}
	b.mtx.Unlock()
}

func (b *Bot) playNext() {
	b.mtx.Lock()
	if len(b.queue) == 0 {
		b.queueIsPlaying = false
		b.mtx.Unlock()
		return
	}
	nextSound := b.queue[0]

	b.queue[0] = nil

	b.queue = b.queue[1:]

	b.mtx.Unlock()
	speaker.Play(beep.Seq(nextSound, beep.Callback(func() {
		go b.playNext()
	})))
}

func (b *Bot) parseWithTts(msgSlice []string) ([]string, []string, []ttsTask) {
	resultSlice := make([]string, 0, len(msgSlice))
	keysToDelete := make([]string, 0)
	tasks := make([]ttsTask, 0)

	inTtsMode := false
	activeTtsLang := ""
	activeTtsEffects := ""
	activeTtsStaticSounds := []string{}

	accumulatedWords := make([]string, 0)

	for _, word := range msgSlice {
		parts := strings.Split(word, "-")
		prefix := parts[0]
		subParts := strings.Split(prefix, "+")

		hasLang := false
		langCode := ""
		staticSounds := make([]string, 0)

		for _, sub := range subParts {
			b.mtx.RLock()
			fullLang, isLang := b.ttsLanguages[sub]

			_, isSound := b.soundsBuffer[sub]
			b.mtx.RUnlock()

			if isLang {
				hasLang = true
				langCode = fullLang
			} else if isSound {
				staticSounds = append(staticSounds, sub)
			}
		}

		if hasLang {
			if inTtsMode {
				task, commandWord := b.prepareTtsTask(activeTtsLang, activeTtsStaticSounds, accumulatedWords, activeTtsEffects)
				if commandWord != "" {
					resultSlice = append(resultSlice, commandWord)
				}

				if task.tempKey != "" {
					keysToDelete = append(keysToDelete, task.tempKey)
					tasks = append(tasks, task)
				}

				accumulatedWords = accumulatedWords[:0]
			}
			inTtsMode = true
			activeTtsLang = langCode
			activeTtsStaticSounds = staticSounds
			if len(parts) > 1 {
				activeTtsEffects = strings.Join(parts[1:], "-")
			} else {
				activeTtsEffects = ""
			}
			continue
		}

		isPureSound := len(staticSounds) == len(subParts) && len(staticSounds) > 0

		if isPureSound {
			if inTtsMode {
				task, commandWord := b.prepareTtsTask(activeTtsLang, activeTtsStaticSounds, accumulatedWords, activeTtsEffects)
				if commandWord != "" {
					resultSlice = append(resultSlice, commandWord)
				}

				if task.tempKey != "" {
					keysToDelete = append(keysToDelete, task.tempKey)
					tasks = append(tasks, task)
				}
				accumulatedWords = accumulatedWords[:0]
				inTtsMode = false
			}
			resultSlice = append(resultSlice, word)
			continue
		}
		if inTtsMode {
			accumulatedWords = append(accumulatedWords, word)
		}
	}
	if inTtsMode {
		task, commandWord := b.prepareTtsTask(activeTtsLang, activeTtsStaticSounds, accumulatedWords, activeTtsEffects)
		if commandWord != "" {
			resultSlice = append(resultSlice, commandWord)
		}

		if task.tempKey != "" {
			keysToDelete = append(keysToDelete, task.tempKey)
			tasks = append(tasks, task)
		}
	}

	return resultSlice, keysToDelete, tasks
}

func (b *Bot) prepareTtsTask(lang string, staticSounds []string, accumulatedWords []string, effects string) (ttsTask, string) {
	commandWord := ""
	if len(accumulatedWords) == 0 {
		if len(staticSounds) > 0 {
			commandWord = strings.Join(staticSounds, "+")
			if effects != "" {
				commandWord += "-" + effects
			}
			return ttsTask{}, commandWord
		}

		return ttsTask{}, ""
	}

	sentence := strings.Join(accumulatedWords, " ")

	req := tts.New(lang, sentence)

	tempKey := fmt.Sprintf("tts_temp_%d", b.ttsCounter.Add(1))

	commandWord = tempKey

	if len(staticSounds) > 0 {
		commandWord += "+" + strings.Join(staticSounds, "+")
	}

	if effects != "" {
		commandWord += "-" + effects
	}
	return ttsTask{
		tempKey: tempKey,
		req:     req,
	}, commandWord
}

func (b *Bot) deleteKeys(keys []string) {
	b.mtx.Lock()
	defer b.mtx.Unlock()
	for _, key := range keys {
		delete(b.soundsBuffer, key)
	}
}
