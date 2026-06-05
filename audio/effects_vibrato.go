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
	streamer    beep.Streamer
	buffer      [1024][2]float64
	writeCount  int
	pos         float64
	sampleCount int
}

func applyVibrato(streamer beep.Streamer) beep.Streamer {
	return &vibratoStreamer{
		streamer:    streamer,
		buffer:      [1024][2]float64{},
		writeCount:  0,
		pos:         0.0,
		sampleCount: 0,
	}
}

func (v *vibratoStreamer) Stream(samples [][2]float64) (n int, ok bool) {
	ok = true
	i := 0
	for i < len(samples) {
		for v.writeCount <= int(v.pos)+1 {
			temp := [1][2]float64{}
			readCount, streamOK := v.streamer.Stream(temp[:])
			if readCount > 0 {
				v.buffer[v.writeCount&1023][0] = temp[0][0]
				v.buffer[v.writeCount&1023][1] = temp[0][1]
				v.writeCount++
			}
			if !streamOK || readCount == 0 {
				ok = false
				break
			}
		}

		if !ok {
			return i, ok
		}
		index1 := int(v.pos)
		index2 := index1 + 1

		wrapIndex1 := index1 & 1023
		wrapIndex2 := index2 & 1023
		frac := v.pos - float64(index1)

		samples[i][0] = v.buffer[wrapIndex1][0]*(1.0-frac) + v.buffer[wrapIndex2][0]*frac
		samples[i][1] = v.buffer[wrapIndex1][1]*(1.0-frac) + v.buffer[wrapIndex2][1]*frac
		timeSec := float64(v.sampleCount) / sampleRate
		step := math.Sin(2.0*math.Pi*freq*timeSec)*depth + 1
		v.pos += step
		v.sampleCount++
		i++
	}
	return i, ok
}

func (v *vibratoStreamer) Err() error {
	return v.streamer.Err()
}
