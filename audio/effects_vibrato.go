package audio

import (
	"math"

	"github.com/gopxl/beep/v2"
)

const (
	freq       = 8.0
	depth      = 0.08
	sampleRate = 44100.0
)

type vibratoStreamer struct {
	data        [][2]float64
	pos         float64
	sampleCount int
}

func applyVibrato(streamer beep.Streamer) beep.Streamer {
	data := make([][2]float64, 0)
	buffData := make([][2]float64, 512)
	for {
		n, ok := streamer.Stream(buffData)
		if n > 0 {
			data = append(data, buffData[:n]...)
		}
		if !ok || n == 0 {
			break
		}
	}

	return &vibratoStreamer{
		data:        data,
		pos:         0,
		sampleCount: 0,
	}
}

func (v *vibratoStreamer) Stream(samples [][2]float64) (n int, ok bool) {
	ok = true
	i := 0
	for ; i < len(samples); i++ {
		timeSec := float64(v.sampleCount) / sampleRate
		step := math.Sin(2.0*math.Pi*freq*timeSec)*depth + 1
		v.pos += step
		if v.pos > float64(len(v.data)-2) {
			ok = false
			break
		}
		index := int(v.pos)
		frac := v.pos - float64(index)
		samples[i][0] = v.data[index][0]*(1-frac) + v.data[index+1][0]*frac
		samples[i][1] = v.data[index][1]*(1-frac) + v.data[index+1][1]*frac
		v.sampleCount++
	}
	return i, ok
}

func (v *vibratoStreamer) Err() error {
	return nil
}
