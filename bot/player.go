package bot

import (
	"beepbot/audio"
	"log"
	"math"
	"strconv"
	"strings"

	"github.com/gempir/go-twitch-irc/v4"
	"github.com/gopxl/beep/v2"
	"github.com/gopxl/beep/v2/effects"
	"github.com/gopxl/beep/v2/speaker"
	"github.com/joho/godotenv"
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
			b.isPlayingSound = false
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
			b.isPlayingSound = false
			b.mtx.Unlock()
			return true
		case "skip":
			if b.IsQueueEnabled() {
				b.mtx.Lock()
				if b.isPlayingSound == false {
					b.mtx.Unlock()
					return true
				}
				b.isPlayingSound = false
				b.mtx.Unlock()
			}
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
		case "vol":
			msgSlice := strings.Fields(msg.Message)
			if len(msgSlice) < 3 {
				return false
			}
			vRaw := msgSlice[2]
			v, err := strconv.Atoi(vRaw)
			if err != nil {
				return false
			}
			if v > 200 {
				v = 200
			}
			if v < 0 {
				v = 0
			}
			b.mtx.Lock()
			b.volume = v
			b.mtx.Unlock()
			b.fileMtx.Lock()
			defer b.fileMtx.Unlock()
			envMap, err := godotenv.Read("config.env")
			if err != nil {
				log.Println("failed to save volume to config.env:", err)
				b.printState()
				return true
			}
			envMap["VOLUME"] = strconv.Itoa(v)
			err = godotenv.Write(envMap, "config.env")
			if err != nil {
				log.Println("failed to save volume to config.env:", err)
			}
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
	streamer := beep.Seq(streamersSlice...)

	if b.volume != 100 {
		vol, silent := getVol(b.volume)
		streamer = &effects.Volume{
			Streamer: streamer,
			Base:     2,
			Volume:   vol,
			Silent:   silent,
		}
	}
	return streamer
}

func getVol(vol int) (float64, bool) {
	if vol == 0 {
		return 0, true
	}
	return math.Log2(float64(vol) / 100), false
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

	b.isPlayingSound = true

	b.mtx.Unlock()
	speaker.Play(beep.Seq(nextSound, beep.Callback(func() {
		b.mtx.Lock()
		b.isPlayingSound = false
		b.mtx.Unlock()
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
