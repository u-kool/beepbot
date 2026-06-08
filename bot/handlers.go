package bot

import (
	"strings"

	"github.com/gempir/go-twitch-irc/v4"
)

func (b *Bot) handleMessage(msg twitch.PrivateMessage) {
	if len(msg.Message) < 2 || (!strings.HasPrefix(msg.Message, "!") && !strings.HasPrefix(msg.Message, "@")) {
		return
	}

	msgSlice := strings.Fields(msg.Message)
	if len(msgSlice) == 0 {
		return
	}

	if len(msgSlice) > 2 && strings.HasPrefix(msgSlice[0], "@") {
		msgSlice = msgSlice[1:]
		msg.Message = strings.Join(msgSlice, " ")
	}

	command := strings.ToLower(msgSlice[0])

	if command == "!m" {
		b.playSound(msg)
	}
}

func (b *Bot) HandleLoop(msgChan <-chan twitch.PrivateMessage) {
	for msg := range msgChan {
		b.handleMessage(msg)
	}
}
