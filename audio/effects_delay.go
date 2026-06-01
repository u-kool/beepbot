package audio

import "github.com/gopxl/beep/v2"

const (
	delayBufferSize = 8820
	tailSamplesMax  = 44100
	delayDryGain    = 0.85
	delayWetGain    = 0.4
)

type delayStreamer struct {
	streamer    beep.Streamer
	lBuffer     []float64
	rBuffer     []float64
	counter     int
	tailSamples int
}

func applyDelay(streamer beep.Streamer) beep.Streamer {
	return &delayStreamer{
		streamer:    streamer,
		lBuffer:     make([]float64, delayBufferSize),
		rBuffer:     make([]float64, delayBufferSize),
		counter:     0,
		tailSamples: tailSamplesMax,
	}
}

func (d *delayStreamer) Stream(samples [][2]float64) (n int, ok bool) {
	n, ok = d.streamer.Stream(samples)
	originalOk := ok
	if !originalOk && d.tailSamples > 0 {
		ok = true
	}
	limit := 0
	if originalOk {
		limit = n
	} else {
		limit = len(samples)
	}
	i := 0
	for ; i < limit; i++ {
		if i < n {
			echoL := d.lBuffer[d.counter]
			echoR := d.rBuffer[d.counter]
			samples[i][0] = samples[i][0]*delayDryGain + echoL*delayWetGain
			d.rBuffer[d.counter] = samples[i][0]
			samples[i][1] = samples[i][1]*delayDryGain + echoR*delayWetGain
			d.lBuffer[d.counter] = samples[i][1]
		} else {
			echoL := d.lBuffer[d.counter]
			echoR := d.rBuffer[d.counter]
			samples[i][0] = echoL * delayWetGain
			d.rBuffer[d.counter] = samples[i][0]
			samples[i][1] = echoR * delayWetGain
			d.lBuffer[d.counter] = samples[i][1]
			d.tailSamples--
		}
		d.counter++
		if d.counter >= len(d.lBuffer) {
			d.counter = 0
		}
		if d.tailSamples == 0 {
			i++
			ok = false
			break
		}
	}

	return i, ok
}

func (d *delayStreamer) Err() error {
	return d.streamer.Err()
}
