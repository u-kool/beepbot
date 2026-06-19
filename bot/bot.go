package bot

import (
	"log"
	"sync"
	"sync/atomic"

	"github.com/gempir/go-twitch-irc/v4"
	"github.com/gopxl/beep/v2"
)

type Bot struct {
	Client         *twitch.Client
	Channel        string
	soundsBuffer   map[string]*beep.Buffer
	mtx            sync.RWMutex
	fileMtx        sync.Mutex
	queue          []beep.Streamer
	queueEnabled   bool
	queueIsPlaying bool
	speakerIsMuted bool
	erIsOn         bool
	ttsLanguages   map[string]string
	ttsCounter     atomic.Uint64
	volume         int
}

func New(channel string, soundsBuffer map[string]*beep.Buffer, ttsLanguages map[string]string, volume int) *Bot {
	b := &Bot{
		Client:         twitch.NewAnonymousClient(),
		Channel:        channel,
		soundsBuffer:   soundsBuffer,
		queue:          make([]beep.Streamer, 0, 50),
		queueEnabled:   false,
		queueIsPlaying: false,
		speakerIsMuted: false,
		erIsOn:         true,
		ttsLanguages:   ttsLanguages,
		volume:         volume,
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

func (b *Bot) printState() {
	b.mtx.RLock()
	defer b.mtx.RUnlock()
	log.Printf("Status -> Mute: %t | Queue: %t | Er: %t | Volume: %d", b.speakerIsMuted, b.queueEnabled, b.erIsOn, b.volume)
}
