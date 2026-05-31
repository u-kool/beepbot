package audio

import (
	"math"

	"github.com/gopxl/beep/v2"
)

const (
	sampleHoldInterval = 8
	bitDepthMultiplier = 64.0
)

type lqStreamer struct {
	streamer   beep.Streamer
	counter    int
	currSample [2]float64
}

func applyLowQuality(streamer beep.Streamer) beep.Streamer {
	return &lqStreamer{
		streamer:   streamer,
		counter:    0,
		currSample: [2]float64{},
	}
}

func (l *lqStreamer) Stream(samples [][2]float64) (n int, ok bool) {
	n, ok = l.streamer.Stream(samples)
	for i := 0; i < n; i++ {
		if l.counter == 0 || l.counter == sampleHoldInterval {
			l.currSample[0] = samples[i][0]
			l.currSample[1] = samples[i][1]
			l.counter = 0
		} else {
			samples[i][0] = l.currSample[0]
			samples[i][1] = l.currSample[1]
		}
		samples[i][0] = bitCrash(samples[i][0])
		samples[i][1] = bitCrash(samples[i][1])
		l.counter++
	}
	return n, ok
}

func (l *lqStreamer) Err() error {
	return l.streamer.Err()
}

func bitCrash(sample float64) float64 {
	return math.Round(sample*bitDepthMultiplier) / bitDepthMultiplier
}
