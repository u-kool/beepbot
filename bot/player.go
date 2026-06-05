package bot

import (
	"beepbot/audio"
	"strings"

	"github.com/gempir/go-twitch-irc/v4"
	"github.com/gopxl/beep/v2"
	"github.com/gopxl/beep/v2/speaker"
)

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

	taskSlice := b.parseMessage(msgSlice)

	taskSlice, keysToDelete := b.resolveTasks(taskSlice)

	if len(keysToDelete) > 0 {
		defer b.deleteKeys(keysToDelete)
	}

	finalStreamer := b.assembleStreamer(taskSlice)
	if finalStreamer == nil {
		return
	}

	if b.IsQueueEnabled() {
		b.pushToQueue(finalStreamer)
	} else {
		speaker.Play(finalStreamer)
	}
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

func (b *Bot) assembleStreamer(taskSlice []PlayTask) beep.Streamer {
	b.mtx.RLock()
	defer b.mtx.RUnlock()
	streamersSlice := make([]beep.Streamer, 0, len(taskSlice))
	for _, t := range taskSlice {
		sound := audio.CreateSoundWithParam(t.Content, t.Effects, b.soundsBuffer, b.erIsOn)
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

func (b *Bot) deleteKeys(keys []string) {
	b.mtx.Lock()
	defer b.mtx.Unlock()
	for _, key := range keys {
		delete(b.soundsBuffer, key)
	}
}
