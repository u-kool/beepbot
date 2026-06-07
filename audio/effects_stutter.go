package audio

import (
	"time"

	"github.com/gopxl/beep/v2"
)

func applyStutter(streamer beep.Streamer) beep.Streamer {
	mainPart, s1 := beep.Dup(streamer)
	s1, s2 := beep.Dup(s1)
	s2, s3 := beep.Dup(s2)
	sr := beep.SampleRate(44100)
	chunkLen := sr.N(140 * time.Millisecond)
	c1 := beep.Take(chunkLen, s1)
	c2 := beep.Take(chunkLen, s2)
	c3 := beep.Take(chunkLen, s3)

	streamer = beep.Seq(c1, c2, c3, mainPart)
	return streamer
}
