package main

import (
	"beepbot/audio"
	"beepbot/bot"
	"fmt"
	"log"
	"os"
	"time"

	"github.com/gempir/go-twitch-irc/v4"
	"github.com/gopxl/beep/v2"
	"github.com/gopxl/beep/v2/speaker"
	"github.com/joho/godotenv"
)

func main() {
	err := godotenv.Load("config.env")
	if err != nil {
		finalErr := fmt.Errorf("config.env file not found: %w", err)
		exitWithError(finalErr)
	}

	channel := os.Getenv("CHANNEL")
	if channel == "" {
		finalErr := fmt.Errorf("channel is missing in config.env")
		exitWithError(finalErr)
	}

	soundsBuffer, errors, err := audio.CreateSoundsBuffer()
	if err != nil {
		exitWithError(err)
	}
	if len(errors) > 0 {
		for _, e := range errors {
			log.Println(e)
		}
	}

	b := bot.New(channel, soundsBuffer)

	sr := beep.SampleRate(44100)
	if err := speaker.Init(sr, sr.N(time.Second/10)); err != nil {
		finalErr := fmt.Errorf("speaker is failed to init: %w", err)
		exitWithError(finalErr)
	}

	msgChan := make(chan twitch.PrivateMessage, 500)

	for range 5 {
		go b.HandleLoop(msgChan)
	}

	b.Client.OnPrivateMessage(func(msg twitch.PrivateMessage) {
		select {
		case msgChan <- msg:
		default:
		}
	})

	b.Client.OnSelfJoinMessage(func(msg twitch.UserJoinMessage) {
		log.Printf("Successfully joined channel: %s\n", msg.Channel)
	})

	b.Client.Join(b.Channel)
	if err := b.Client.Connect(); err != nil {
		finalErr := fmt.Errorf("failed to join channel: %w", err)
		exitWithError(finalErr)
	}
}
