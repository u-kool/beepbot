package audio

import "github.com/gopxl/beep/v2"

type reversedStreamer struct {
	data [][2]float64
	pos  int
}

func applyReverse(buffer *beep.Buffer, start, end int) beep.Streamer {
	stream := buffer.Streamer(start, end)
	data := make([][2]float64, end-start)
	stream.Stream(data)
	pos := len(data) - 1
	return &reversedStreamer{data, pos}
}

func (r *reversedStreamer) Stream(samples [][2]float64) (n int, ok bool) {
	for i := range samples {
		if r.pos < 0 {
			return i, false
		}
		samples[i] = r.data[r.pos]
		r.pos--
	}
	return len(samples), true
}

func (r *reversedStreamer) Err() error {
	return nil
}
