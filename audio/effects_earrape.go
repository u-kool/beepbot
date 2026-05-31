package audio

import "github.com/gopxl/beep/v2"

const (
	limit            = 0.05
	volumeMultiplier = 5.0
)

type earRapeStreamer struct {
	streamer beep.Streamer
}

func applyEarRape(streamer beep.Streamer) beep.Streamer {
	return &earRapeStreamer{
		streamer: streamer,
	}
}

func (e *earRapeStreamer) Stream(samples [][2]float64) (n int, ok bool) {
	n, ok = e.streamer.Stream(samples)
	for i := 0; i < n; i++ {
		if samples[i][0] < -limit {
			samples[i][0] = -limit
		}
		if samples[i][0] > limit {
			samples[i][0] = limit
		}
		samples[i][0] *= volumeMultiplier

		if samples[i][1] < -limit {
			samples[i][1] = -limit
		}
		if samples[i][1] > limit {
			samples[i][1] = limit
		}
		samples[i][1] *= volumeMultiplier

	}
	return n, ok
}

func (e *earRapeStreamer) Err() error {
	return e.streamer.Err()
}
