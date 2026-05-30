package audio

import "github.com/gopxl/beep/v2"

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
		if samples[i][0] < -0.05 {
			samples[i][0] = -0.05
		}
		if samples[i][0] > 0.05 {
			samples[i][0] = 0.05
		}
		samples[i][0] *= 5
		if samples[i][1] < -0.05 {
			samples[i][1] = -0.05
		}
		if samples[i][1] > 0.05 {
			samples[i][1] = 0.05
		}
		samples[i][1] *= 5

	}
	return n, ok
}

func (e *earRapeStreamer) Err() error {
	return e.streamer.Err()
}
