package bot

import (
	"log"
	"sync"

	"github.com/gempir/go-twitch-irc/v4"
	"github.com/gopxl/beep/v2"
)

type Bot struct {
	Client         *twitch.Client
	Channel        string
	SoundsBuffer   map[string]*beep.Buffer
	mtx            sync.RWMutex
	queue          []beep.Streamer
	queueEnabled   bool
	queueIsPlaying bool
	speakerIsMuted bool
	erIsOn         bool
}

func New(channel string, soundsBuffer map[string]*beep.Buffer) *Bot {
	b := &Bot{
		Client:         twitch.NewAnonymousClient(),
		Channel:        channel,
		SoundsBuffer:   soundsBuffer,
		queue:          make([]beep.Streamer, 0, 50),
		queueEnabled:   false,
		queueIsPlaying: false,
		speakerIsMuted: false,
		erIsOn:         true,
	}
	return b
}

func (b *Bot) IsMuted() bool {
	b.mtx.RLock()
	defer b.mtx.RUnlock()
	return b.speakerIsMuted
}

func (b *Bot) SetMuted(muted bool) {
	b.mtx.Lock()
	defer b.mtx.Unlock()
	b.speakerIsMuted = muted
}

func (b *Bot) IsQueueEnabled() bool {
	b.mtx.RLock()
	defer b.mtx.RUnlock()
	return b.queueEnabled
}

func (b *Bot) SetQEnabled(enabled bool) {
	b.mtx.Lock()
	defer b.mtx.Unlock()
	b.queueEnabled = enabled
}

func (b *Bot) IsErOn() bool {
	b.mtx.RLock()
	defer b.mtx.RUnlock()
	return b.erIsOn
}

func (b *Bot) printState() {
	b.mtx.RLock()
	defer b.mtx.RUnlock()
	log.Printf("Status -> Mute: %t | Queue: %t | Er: %t", b.speakerIsMuted, b.queueEnabled, b.erIsOn)
}
