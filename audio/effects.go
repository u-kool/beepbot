package audio

import (
	"time"

	"github.com/gopxl/beep/v2"
	"github.com/gopxl/beep/v2/effects"
	"github.com/gopxl/beep/v2/generators"
)

type ReversedStreamer struct {
	data [][2]float64
	pos  int
}

func applyReverse(buffer *beep.Buffer, start, end int) beep.Streamer {
	stream := buffer.Streamer(start, end)
	data := make([][2]float64, end-start)
	stream.Stream(data)
	pos := len(data) - 1
	return &ReversedStreamer{data, pos}
}

func (r *ReversedStreamer) Stream(samples [][2]float64) (n int, ok bool) {
	for i := range samples {
		if r.pos < 0 {
			return i, false
		}
		samples[i] = r.data[r.pos]
		r.pos--
	}
	return len(samples), true
}

func (r *ReversedStreamer) Err() error {
	return nil
}

func (r *ReversedStreamer) Len() int {
	return r.pos + 1
}

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

func applyLowQuality(streamer beep.Streamer) beep.Streamer {
	streamer = beep.Resample(1, 44100, 4000, streamer)
	streamer = beep.Resample(1, 4000, 44100, streamer)
	streamer = &effects.Volume{
		Streamer: streamer,
		Base:     2,
		Volume:   1,
	}
	return streamer
}

func applyEarRape(streamer beep.Streamer) beep.Streamer {
	streamer = &effects.Gain{
		Streamer: streamer,
		Gain:     40.0,
	}

	streamer = &effects.Volume{
		Streamer: streamer,
		Base:     2.0,
		Volume:   -2.0,
	}
	return streamer
}

func applyDelay(streamer beep.Streamer) beep.Streamer {
	sr := beep.SampleRate(44100)
	delayMs := []int{120, 280, 480}
	volumes := []float64{-1.0, -2.2, -3.6}
	current := streamer
	var echoes []beep.Streamer
	for i := range delayMs {
		var e beep.Streamer
		current, e = beep.Dup(current)
		delay := sr.N(time.Duration(delayMs[i]) * time.Millisecond)
		s := beep.Seq(generators.Silence(delay), e)
		v := &effects.Volume{
			Streamer: s,
			Base:     2.0,
			Volume:   volumes[i],
		}
		echoes = append(echoes, v)
	}
	streamer = beep.Mix(append([]beep.Streamer{current}, echoes...)...)
	return streamer
}
