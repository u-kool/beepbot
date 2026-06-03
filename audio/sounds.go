package audio

import (
	"errors"
	"strconv"
	"strings"

	"github.com/gopxl/beep/v2"
)

type SoundWithParam struct {
	names           []string
	cutStartPercent int
	cutEndPercent   int
	reversed        bool
	stutter         bool
	lowQuality      bool
	earRape         bool
	delay           bool
	vibrato         bool
	speedRatio      int
}

func CreateSoundWithParam(msg string, trackBuffer map[string]*beep.Buffer, isEarOn bool) *SoundWithParam {
	msgSlice := strings.Split(msg, "-")
	names := msgSlice[0]
	namesSlice := strings.Split(names, "+")
	soundWithParam := &SoundWithParam{
		names:           []string{},
		cutStartPercent: 0,
		cutEndPercent:   0,
		reversed:        false,
		stutter:         false,
		lowQuality:      false,
		earRape:         false,
		delay:           false,
		vibrato:         false,
		speedRatio:      100,
	}
	for _, n := range namesSlice {
		if strings.ToLower(n) == "rand" {
			name := getRandomName(trackBuffer)
			soundWithParam.names = append(soundWithParam.names, name)
			continue
		}

		_, ok := trackBuffer[n]
		if !ok {
			continue
		}
		soundWithParam.names = append(soundWithParam.names, n)
	}

	params := msgSlice[1:]

	parseParam(soundWithParam, params)

	if !isEarOn {
		soundWithParam.earRape = false
	}
	return soundWithParam
}

func parseParam(soundWithParam *SoundWithParam, params []string) {
	for _, p := range params {
		if len(p) < 2 {
			continue
		}
		switch string(p[:2]) {
		case "cs":
			cutStartPercent, err := strconv.ParseInt(string(p[2:]), 10, 64)
			if err != nil {
				continue
			}
			if cutStartPercent < 0 {
				soundWithParam.cutStartPercent = 0
				continue
			}
			if cutStartPercent > 100 {
				soundWithParam.cutStartPercent = 100
				continue
			}
			soundWithParam.cutStartPercent = int(cutStartPercent)

		case "ce":
			cutEndPercent, err := strconv.ParseInt(string(p[2:]), 10, 64)
			if err != nil {
				continue
			}
			if cutEndPercent < 0 {
				soundWithParam.cutEndPercent = 0
				continue
			}
			if cutEndPercent > 100 {
				soundWithParam.cutEndPercent = 100
				continue
			}
			soundWithParam.cutEndPercent = int(cutEndPercent)
		case "rs":
			soundWithParam.reversed = true
		case "st":
			soundWithParam.stutter = true
		case "lq":
			soundWithParam.lowQuality = true
		case "er":
			soundWithParam.earRape = true
		case "dl":
			soundWithParam.delay = true
		case "vb":
			soundWithParam.vibrato = true
		case "sp":
			speedRatio, err := strconv.ParseInt(string(p[2:]), 10, 64)
			if err != nil {
				continue
			}
			if speedRatio < 10 {
				soundWithParam.speedRatio = 10
				continue
			}
			if speedRatio > 200 {
				soundWithParam.speedRatio = 200
				continue
			}
			soundWithParam.speedRatio = int(speedRatio)
		}
	}
}

func CreateStreamerWithParameter(s *SoundWithParam, trackBuffer map[string]*beep.Buffer) (beep.Streamer, error) {
	if len(s.names) < 1 {
		return nil, errors.New("audio is empty")
	}
	var totalLen int
	streamerSlice := make([]beep.Streamer, 0, len(s.names))
	var streamer beep.Streamer
	for _, name := range s.names {
		currBuffer := trackBuffer[name]
		totalLen = currBuffer.Len()
		start := totalLen * s.cutStartPercent / 100
		end := totalLen * (100 - s.cutEndPercent) / 100
		if start >= end {
			start = 0
			end = totalLen
		}
		var str beep.Streamer
		if s.reversed {
			str = applyReverse(currBuffer, start, end)
		} else {
			str = currBuffer.Streamer(start, end)
		}
		streamerSlice = append(streamerSlice, str)
	}
	streamer = beep.Mix(streamerSlice...)
	if s.stutter {
		streamer = applyStutter(streamer)
	}
	if s.lowQuality {
		streamer = applyLowQuality(streamer)
	}
	if s.earRape {
		streamer = applyEarRape(streamer)
	}
	if s.delay {
		streamer = applyDelay(streamer)
	}
	if s.vibrato {
		streamer = applyVibrato(streamer)
	}
	if s.speedRatio != 100 {
		streamer = beep.ResampleRatio(4, float64(s.speedRatio)/100.0, streamer)
	}

	return streamer, nil
}
