package audio

import (
	"errors"
	"math/rand/v2"
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
	gacha           bool
	speedRatio      int
}

func CreateSoundWithParam(sounds string, effects string, trackBuffer map[string]*beep.Buffer, isErOn bool) *SoundWithParam {
	namesSlice := strings.Split(sounds, "+")
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
		gacha:           false,
		speedRatio:      100,
	}
	for _, n := range namesSlice {
		n = strings.ToLower(n)
		if n == "rand" {
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

	params := strings.Split(effects, "-")

	parseParam(soundWithParam, params)

	if !isErOn {
		soundWithParam.earRape = false
	}

	if soundWithParam.gacha {
		soundWithParam.applyRandomEffects(isErOn)
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
		case "ga":
			soundWithParam.gacha = true
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

func (s *SoundWithParam) applyRandomEffects(isErOn bool) {
	appliedC := 0
	candidates := make([]string, 0, 7)
	if s.reversed {
		appliedC++
	} else {
		candidates = append(candidates, "reversed")
	}
	if s.stutter {
		appliedC++
	} else {
		candidates = append(candidates, "stutter")
	}
	if s.lowQuality {
		appliedC++
	} else {
		candidates = append(candidates, "lowQuality")
	}
	if s.earRape {
		appliedC++
	} else {
		if isErOn {
			candidates = append(candidates, "earRape")
		}
	}
	if s.delay {
		appliedC++
	} else {
		candidates = append(candidates, "delay")
	}
	if s.vibrato {
		appliedC++
	} else {
		candidates = append(candidates, "vibrato")
	}
	if s.speedRatio != 100 {
		appliedC++
	} else {
		candidates = append(candidates, "speed")
	}

	limit := 3 - appliedC
	if limit < 0 {
		limit = 0
	}

	limit = isCrit(limit)

	if limit == 0 {
		return
	}

	finalCount := getFinalCount(limit)

	rand.Shuffle(len(candidates), func(i int, j int) {
		candidates[i], candidates[j] = candidates[j], candidates[i]
	})
	for i := 0; i < finalCount; i++ {
		switch candidates[i] {
		case "reversed":
			s.reversed = true
		case "stutter":
			s.stutter = true
		case "lowQuality":
			s.lowQuality = true
		case "earRape":
			s.earRape = true
		case "delay":
			s.delay = true
		case "vibrato":
			s.vibrato = true
		case "speed":
			s.speedRatio = randomSpeedRatio()
		}
	}
}

func isCrit(num int) int {
	roll := rand.IntN(100) + 1
	if roll <= 5 {
		return num + 1
	}
	return num
}

func getFinalCount(n int) int {
	switch n {
	case 1:
		return 1
	case 2:
		roll := rand.IntN(100) + 1
		if roll <= 70 {
			return 1
		} else {
			return 2
		}
	case 3:
		roll := rand.IntN(100) + 1
		if roll <= 50 {
			return 1
		} else if roll <= 85 {
			return 2
		} else {
			return 3
		}
	case 4:
		roll := rand.IntN(100) + 1
		if roll <= 30 {
			return 2
		} else if roll <= 80 {
			return 3
		} else {
			return 4
		}
	}
	return 0
}

func randomSpeedRatio() int {
	speed := 0
	roll := rand.IntN(100) + 1

	if roll <= 45 {
		speed = rand.IntN(80-50+1) + 50
	} else if roll <= 85 {
		speed = rand.IntN(170-120+1) + 120
	} else {
		speed = rand.IntN(45-20+1) + 20
	}
	return speed
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
		if currBuffer == nil {
			return nil, errors.New("corrupted or empty buffer")
		}
		totalLen = currBuffer.Len()
		start := totalLen * s.cutStartPercent / 100
		end := totalLen * (100 - s.cutEndPercent) / 100
		if start >= end {
			start = 0
			end = totalLen
		}
		var str beep.Streamer
		if s.reversed {
			if end-start > maxReverseSamples {
				end = start + maxReverseSamples
			}
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
		streamer = beep.ResampleRatio(3, float64(s.speedRatio)/100.0, streamer)
	}

	return streamer, nil
}
