package tts

import (
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"strings"
)

func NeedTranslate(effects string) (string, bool) {
	effSlice := strings.Split(effects, "-")
	for i, eff := range effSlice {
		if strings.ToLower(eff) == "tr" {
			effSlice[i] = effSlice[len(effSlice)-1]
			effSlice = effSlice[:len(effSlice)-1]
			return strings.Join(effSlice, "-"), true
		}
	}
	return effects, false
}

func getTranslateReq(lang, text string) string {
	v := url.Values{}
	v.Add("client", "gtx")
	v.Add("dt", "t")
	v.Add("sl", "auto")
	v.Add("tl", lang)
	v.Add("q", text)
	return "https://translate.googleapis.com/translate_a/single?" + v.Encode()
}

func Translate(lang, text string) (string, error) {
	reqUrl := getTranslateReq(lang, text)

	resp, err := httpClient.Get(reqUrl)
	if err != nil {
		return "", err
	}

	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return "", fmt.Errorf("status not ok: %d", resp.StatusCode)
	}
	var result []any

	limitData := io.LimitReader(resp.Body, 8<<10)

	data, err := io.ReadAll(limitData)
	if err != nil {
		return "", fmt.Errorf("can't read data: %v", err)
	}

	err = json.Unmarshal(data, &result)
	if err != nil {
		return "", fmt.Errorf("can't decode data: %v", err)
	}
	if len(result) == 0 {
		return "", errors.New("empty translate response")
	}
	var finalResult string

	firstLevel, ok := result[0].([]any)
	if !ok {
		return "", errors.New("bad json structure")
	}
	for i := range firstLevel {
		secondLevel, ok := firstLevel[i].([]any)
		if !ok {
			continue
		}
		result, ok := secondLevel[0].(string)
		if !ok {
			continue
		}
		finalResult += result
	}

	return finalResult, nil
}
