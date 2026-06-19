package main

import (
	"beepbot/audio"
	"beepbot/bot"
	"beepbot/tts"
	"fmt"
	"log"
	"os"
	"strconv"
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

	volume, err := strconv.Atoi(os.Getenv("VOLUME"))
	if err != nil {
		volume = 100
	}

	if volume > 200 {
		volume = 200
	}

	if volume < 0 {
		volume = 0
	}

	soundsBuffer, errors, err := audio.CreateSoundsBuffer()
	if err != nil {
		log.Println("sounds folder missing/empty; TTS only active")
	}
	if len(errors) > 0 {
		for _, e := range errors {
			log.Println(e)
		}
	}

	ttsLanguages := tts.NewTtsLanguages()

	b := bot.New(channel, soundsBuffer, ttsLanguages, volume)

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
