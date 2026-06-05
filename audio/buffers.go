package audio

import (
	"fmt"
	"math/rand/v2"
	"os"
	"path/filepath"
	"strings"

	"github.com/gopxl/beep/v2"
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

		if filepath.Ext(fileName) != ".wav" {
			continue
		}
		data, err := os.Open("./sounds/" + fileName)
		if err != nil {
			e := fmt.Errorf("failed to open audio file: %w", err)
			errors = append(errors, e)
			continue
		}
		track, _, err := wav.Decode(data)
		if err != nil {
			e := fmt.Errorf("failed to decode audio file: %w", err)
			errors = append(errors, e)
			data.Close()
			continue
		}
		trackBuff := beep.NewBuffer(format)
		trackBuff.Append(track)
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
