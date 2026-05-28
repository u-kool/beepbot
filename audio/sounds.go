package audio

import (
	"errors"
	"strconv"
	"strings"

	"github.com/gopxl/beep/v2"
)

type SoundWithParam struct {
	Names       []string
	CutPercent  int
	SkipPercent int
	Reversed    bool
	Stutter     bool
	LowQuality  bool
	EarRape     bool
	Delay       bool
	SpeedRatio  int
}

func CreateSoundWithParam(msg string, trackBuffer map[string]*beep.Buffer, isEarOn bool) *SoundWithParam {
	msgSlice := strings.Split(msg, "-")
	names := msgSlice[0]
	namesSlice := strings.Split(names, "+")
	soundWithParam := &SoundWithParam{
		Names:       []string{},
		CutPercent:  0,
		SkipPercent: 0,
		Reversed:    false,
		Stutter:     false,
		LowQuality:  false,
		EarRape:     false,
		Delay:       false,
		SpeedRatio:  100,
	}
	for _, n := range namesSlice {
		if strings.ToLower(n) == "rand" {
			name := GetRandomName(trackBuffer)
			soundWithParam.Names = append(soundWithParam.Names, name)
			continue
		}

		_, ok := trackBuffer[n]
		if !ok {
			continue
		}
		soundWithParam.Names = append(soundWithParam.Names, n)
	}

	params := msgSlice[1:]

	parseParam(soundWithParam, params)

	if !isEarOn {
		soundWithParam.EarRape = false
	}
	return soundWithParam
}

func parseParam(soundWithParam *SoundWithParam, params []string) {
	for _, p := range params {
		if len(p) < 2 {
			continue
		}
		switch string(p[:2]) {
		case "ct":
			cutPercent, err := strconv.ParseInt(string(p[2:]), 10, 64)
			if err != nil {
				continue
			}
			if cutPercent < 0 {
				soundWithParam.CutPercent = 0
				continue
			}
			if cutPercent > 100 {
				soundWithParam.CutPercent = 100
				continue
			}
			soundWithParam.CutPercent = int(cutPercent)
		case "sk":
			skipPercent, err := strconv.ParseInt(string(p[2:]), 10, 64)
			if err != nil {
				continue
			}
			if skipPercent < 0 {
				soundWithParam.SkipPercent = 0
				continue
			}
			if skipPercent > 100 {
				soundWithParam.SkipPercent = 100
				continue
			}
			soundWithParam.SkipPercent = int(skipPercent)
		case "rs":
			soundWithParam.Reversed = true
		case "st":
			soundWithParam.Stutter = true
		case "lq":
			soundWithParam.LowQuality = true
		case "er":
			soundWithParam.EarRape = true
		case "dl":
			soundWithParam.Delay = true
		case "sp":
			speedRatio, err := strconv.ParseInt(string(p[2:]), 10, 64)
			if err != nil {
				continue
			}
			if speedRatio < 10 {
				soundWithParam.SpeedRatio = 10
				continue
			}
			if speedRatio > 200 {
				soundWithParam.SpeedRatio = 200
				continue
			}
			soundWithParam.SpeedRatio = int(speedRatio)
		}
	}
}

func CreateStreamerWithParameter(s *SoundWithParam, trackBuffer map[string]*beep.Buffer) (beep.Streamer, error) {
	if len(s.Names) < 1 {
		return nil, errors.New("audio is empty")
	}
	var totalLen int
	streamerSlice := make([]beep.Streamer, 0, len(s.Names))
	var streamer beep.Streamer
	for _, name := range s.Names {
		currBuffer := trackBuffer[name]
		totalLen = currBuffer.Len()
		start := totalLen * s.SkipPercent / 100
		end := totalLen * (100 - s.CutPercent) / 100
		if start >= end {
			start = 0
			end = totalLen
		}
		var str beep.Streamer
		if s.Reversed {
			str = applyReverse(currBuffer, start, end)
		} else {
			str = currBuffer.Streamer(start, end)
		}
		streamerSlice = append(streamerSlice, str)
	}
	streamer = beep.Mix(streamerSlice...)
	if s.Stutter {
		streamer = applyStutter(streamer)
	}
	if s.LowQuality {
		streamer = applyLowQuality(streamer)
	}
	if s.EarRape {
		streamer = applyEarRape(streamer)
	}
	if s.Delay {
		streamer = applyDelay(streamer)
	}
	if s.SpeedRatio != 100 {
		streamer = beep.ResampleRatio(4, float64(s.SpeedRatio)/100.0, streamer)
	}

	return streamer, nil
}
