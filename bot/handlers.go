package bot

import (
	"strings"

	"github.com/gempir/go-twitch-irc/v4"
)

func (b *Bot) HandleMessage(msg twitch.PrivateMessage) {
	if len(msg.Message) == 0 || msg.Message[0] != '!' {
		return
	}

	msgSlice := strings.Fields(msg.Message)
	if len(msgSlice) == 0 {
		return
	}

	command := strings.ToLower(msgSlice[0])

	if command == "!m" {
		b.PlaySound(msg)
	}
}

func (b *Bot) HandleLoop(msgChan <-chan twitch.PrivateMessage) {
	for msg := range msgChan {
		b.HandleMessage(msg)
	}
}
