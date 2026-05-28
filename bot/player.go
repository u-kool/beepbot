package bot

import (
	"beepbot/audio"
	"strings"

	"github.com/gempir/go-twitch-irc/v4"
	"github.com/gopxl/beep/v2"
	"github.com/gopxl/beep/v2/speaker"
)

func (b *Bot) PlaySound(msg twitch.PrivateMessage) {
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

	finalStreamer := b.assembleStreamer(msgSlice)

	if finalStreamer == nil {
		return
	}

	if b.IsQueueEnabled() {
		b.PushToQueue(finalStreamer)
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
					b.PlayNext()
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
	isErOn := b.IsErOn()
	streamersSlice := make([]beep.Streamer, 0, len(msgSlice))
	for _, s := range msgSlice {
		sound := audio.CreateSoundWithParam(s, b.SoundsBuffer, isErOn)
		streamer, err := audio.CreateStreamerWithParameter(sound, b.SoundsBuffer)
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

func (b *Bot) PushToQueue(s beep.Streamer) {
	b.mtx.Lock()
	if len(b.queue) >= 50 {
		b.mtx.Unlock()
		return
	}
	b.queue = append(b.queue, s)
	if !b.queueIsPlaying {
		b.queueIsPlaying = true
		b.mtx.Unlock()
		b.PlayNext()
		return
	}
	b.mtx.Unlock()
}

func (b *Bot) PlayNext() {
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
		go b.PlayNext()
	})))
}
