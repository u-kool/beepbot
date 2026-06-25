package audio

import (
	"fmt"
	"math/rand/v2"
	"os"
	"path/filepath"
	"strings"

	"github.com/gopxl/beep/v2"
	"github.com/gopxl/beep/v2/mp3"
	"github.com/gopxl/beep/v2/wav"
)

func CreateSoundsBuffer() (map[string]*beep.Buffer, []error, error) {
	bufferCache := make(map[string]*beep.Buffer)
	dir, err := os.ReadDir("./sounds")
	if err != nil {
		finalErr := fmt.Errorf("failed to read sounds directory: %w", err)
		return map[string]*beep.Buffer{}, nil, finalErr
	}
	format := beep.Format{
		SampleRate:  44100,
		NumChannels: 2,
		Precision:   2,
	}

	errors := []error{}
	for _, file := range dir {
		fileName := file.Name()

		if filepath.Ext(fileName) != ".wav" && filepath.Ext(fileName) != ".mp3" {
			continue
		}
		data, err := os.Open("./sounds/" + fileName)
		if err != nil {
			e := fmt.Errorf("failed to open audio file: %w", err)
			errors = append(errors, e)
			continue
		}

		check := checkSoundFormat(data)

		var track beep.StreamSeekCloser
		var rawFormat beep.Format
		switch check {
		case "wav":
			track, rawFormat, err = wav.Decode(data)
		case "mp3":
			track, rawFormat, err = mp3.Decode(data)
		default:
			data.Close()
			continue
		}

		if err != nil {
			e := fmt.Errorf("failed to decode audio file: %w", err)
			errors = append(errors, e)
			data.Close()
			continue
		}
		trackBuff := beep.NewBuffer(format)
		if rawFormat.SampleRate != 44100 {
			resampledTrack := beep.Resample(4, rawFormat.SampleRate, format.SampleRate, track)
			trackBuff.Append(resampledTrack)
		} else {
			trackBuff.Append(track)
		}

		name := strings.TrimSuffix(fileName, filepath.Ext(fileName))
		name = strings.ToLower(name)
		bufferCache[name] = trackBuff
		data.Close()
	}
	if len(bufferCache) == 0 {
		finalErr := fmt.Errorf("sound list is empty")
		return bufferCache, nil, finalErr
	}
	return bufferCache, errors, nil
}

func getRandomName(buffer map[string]*beep.Buffer) string {
	if len(buffer) == 0 {
		return ""
	}
	r := rand.IntN(len(buffer))
	i := 0
	var finalName string
	for name := range buffer {
		if i == r {
			finalName = name
			break
		}
		i++
	}
	return finalName
}

func checkSoundFormat(data *os.File) string {
	defer data.Seek(0, 0)
	var check [12]byte

	_, err := data.Read(check[:])
	if err != nil {
		return ""
	}
	firstWavCheck := string(check[0:4])
	secWavCheck := string(check[8:12])

	firstMp3Check := string(check[0:3])

	if firstWavCheck == "RIFF" && secWavCheck == "WAVE" {
		return "wav"
	}
	if firstMp3Check == "ID3" || (check[0] == 0xff && check[1] >= 0xf0) {
		return "mp3"
	}
	return ""
}
