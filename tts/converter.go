package tts

import (
	"fmt"
	"net/http"
	"net/url"

	"github.com/gopxl/beep/v2"
	"github.com/gopxl/beep/v2/mp3"
)

type Request struct {
	lang string
	text string
}

func New(lang string, text string) *Request {
	return &Request{
		lang: lang,
		text: text,
	}
}

func (r *Request) ToBuffer() (*beep.Buffer, error) {
	reqUrl := getUrlRequest(r.lang, r.text)

	resp, err := http.Get(reqUrl)
	if err != nil {
		return nil, err
	}

	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("status not ok: %d", resp.StatusCode)
	}

	data, rawFormat, err := mp3.Decode(resp.Body)
	if err != nil {
		return nil, err
	}

	format := beep.Format{
		SampleRate:  44100,
		NumChannels: 2,
		Precision:   2,
	}

	finalStreamer := beep.Resample(1, rawFormat.SampleRate, format.SampleRate, data)

	finalBuff := beep.NewBuffer(format)
	finalBuff.Append(finalStreamer)
	return finalBuff, nil
}

func getUrlRequest(lang string, text string) string {
	v := url.Values{}
	v.Add("ie", "UTF-8")
	v.Add("client", "tw-ob")
	v.Add("tl", lang)
	v.Add("q", text)
	return "https://translate.google.com/translate_tts?" + v.Encode()
}
