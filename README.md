[RU](#beepbot-russian-version)

Inspired by `funnebot` by `@Chazoshtare`



# beepbot

beepbot is a lightweight, interactive Twitch sound bot that lets your chat trigger custom sound memes, generate text-to-speech (TTS) voices in multiple languages, and apply audio effects.

> ℹ️ **Translation (v1.4.0):** Now you can translate the text by appending the `-tr` modifier to the language code:
> * `!m en hello chat` — read the text in English.
> * `!m ru-tr hello my friend` — translate the text to Russian ("привет, мой друг") and speak it with the Russian voice.

> Note that translating (`-tr`) may expand your text, potentially cutting it off earlier. [More about limit](#tts-limit-en)

> ℹ️ **Volume Control (v1.2.0):** Change volume with `!m vol [0-200]`— it automatically saves to `config.env` (ensure the bot has write permissions). You can also set it at startup via `VOLUME=`.

---

## Setup & Launch

1. Open `config.env` with a text editor, enter your Twitch channel name (`CHANNEL=your_channel_name`), and optionally set your starting volume (`VOLUME=100`, range 0-200).
2. Place your sound files in **`.wav`** or **`.mp3`** format (44100 Hz recommended) into the `sounds` folder. The filename (excluding the extension) automatically becomes the chat command.
3. Run the executable file.
4. When updating to a new version, you only need to replace the old `beepbot.exe` file with the new one. Do not overwrite your configured `config.env` file or the `sounds` folder to avoid losing your data.

> * **File Duration:** Use short sounds (1–10s). The bot caches all audio into RAM for instant, lag-free playback. Long music tracks will quickly overload your computer's RAM.
> * The release package already includes a `sounds` folder with a sample `beep.wav` file. You can run the bot immediately and test it in your chat using the `!m beep` command.


---

## Chat Commands

The main command for viewers is:
`!m [sound_name_or_language_code]-[effects]`

* `!m rand` — play a random sound from the `sounds` folder.

### 1. Text-To-Speech (TTS)
Specify the language code before the text you want to read:
* `!m en hello chat` — read the text in English.
* `!m jp ohayo` — read the text in Japanese.

[Full list of supported languages](tts/languages.go)

### 2. Combining Sounds & Speech
* **Simultaneous Mix (using `+`):** `!m sound1+sound2-rs` (both sounds will play at the exact same time, reversed).
* **Sequential Chain (using spaces):** `!m sound1-sp150 en hello sound2` (plays sped-up sound1, then reads "hello" in English, and finally plays sound2).

---

## Audio Effects

Viewers can modify any sound or TTS by adding parameters separated by a hyphen `-` (order does not matter):

| Parameter | Effect | Range | Description |
| --- | --- | --- | --- |
| `sp[value]` | Speed | 10 - 200 | Playback speed and pitch (Default: `100`.`sp150` is faster, `sp50` is slower). |
| `cs[value]` | Cut start | 0 - 100 | Cuts the specified percentage of the sound from the start (e.g., `cs20`). |
| `ce[value]` | Cut end | 0 - 100 | Cuts the specified percentage of the sound from the end (e.g., `ce20`). |
| `rs` | Reverse | — | Plays the sound backward. |
| `lq` | Low Quality | — | Applies an 8-bit retro sound effect (bitcrushing). |
| `st` | Stutter | — | Applies a rapid stutter effect to the beginning of the sound. |
| `er` | Ear Rape | — | Applies an extreme volume overdrive. |
| `dl` | Delay | — | Applies a decaying echo effect. |
| `vb` | Vibrato | — | Applies a pitch-vibrating effect. |
| `ga` | Gacha | — | Randomly adds unused effects. The number of added effects depends on how many you already specified (if you have already specified 3 or more, no effects are added unless you trigger a rare 5% jackpot, which adds 1 more) |
| `tr` | Translation | — | **TTS only.** Translates the text into the target language (e.g., `ru-tr hello`). |

*(Examples: `!m ru-sp150 hello`, `!m omg-ga`)*

> ℹ️ *Note:* Trimming (cs/ce) is always applied to the original sound first, before any other effects are processed.

<a name="tts-limit-en"></a>

>  **TTS Length Limit (200 chars):** Due to using free web API it has a strict 200-character limit per request. To bypass this limit, chain multiple TTS commands sequentially in one message:
> * ❌ `!m ru long_text_300_chars` (Bad - will be truncated to 200 chars).
> *  `!m ru text_150_chars ru text_150_chars` (Excellent - plays seamlessly).

---

## Admin Commands (Broadcaster & Moderators Only)

| Command | Description |
| --- | --- |
| `!m mute` / `unmute` | Mutes / unmutes the bot (instantly stops audio, clears the queue). |
| `!m qon` / `qoff` | Enables / disables sequential queue (if `qoff`, sounds will overlap concurrently). |
| `!m eron` / `eroff` | Enables / disables global ear safety (strictly blocks the `er` effect). |
| `!m stop` | Instantly stops currently playing sound and clears the entire queue. |
| `!m skip` | Instantly interrupts current sound and plays the next queued item. |
| `!m vol [value]` | Sets the master volume of the bot (range: 0-200, default: 100). The setting is automatically saved. |

***

<a name="beepbot-russian-version"></a>

# beepbot

beepbot — это легкий интерактивный Twitch-бот, который позволяет зрителям запускать звуковые мемы, озвучивать текст (TTS) на разных языках и накладывать аудиоэффекты.

> ℹ️ **Переводчик (v1.4.0):** Теперь вы можете переводить текст, добавив модификатор `-tr` к коду языка:
> * `!m en hello chat` — озвучить текст на английском.
> * `!m ru-tr hello my friend` — автоматически перевести английский текст на русский («привет, мой друг») и озвучить его русским голосом.

> Учтите, что перевод (`-tr`) может удлинить ваш текст, из-за чего он обрежется раньше. [Подробнее про лимит](#tts-limit-ru)

> ℹ️ **Громкость (v1.2.0):** Меняйте громкость командой `!m vol [0-200]`— значение автоматически запишется в `config.env`(убедитесь, что у бота есть права на запись). Также громкость можно задать при старте через `VOLUME=`.

---

## Настройка и запуск

1. Откройте файл `config.env` текстовым редактором, впишите имя вашего Twitch-канала (`CHANNEL=имя_вашего_канала`) и, по желанию, стартовую громкость (`VOLUME=100`, диапазон 0-200).
2. Положите свои аудиофайлы в формате **`.wav`** или **`.mp3`** (рекомендуется частота 44100 Гц) в папку `sounds`. Название файла (без расширения) становится командой вызова.
3. Запустите исполняемый файл бота.
4. При выходе новой версии достаточно заменить старый файл `beepbot.exe` на новый. Не перезаписывайте уже настроенный файл `config.env` и папку `sounds`, чтобы не потерять свои данные.

> * **Длительность звуков:** Используйте короткие звуки (1–10 сек). Бот хранит аудио в ОЗУ для мгновенного воспроизведения. Длинные треки быстро перегрузят оперативную память вашего компьютера.
> * Релизный архив уже содержит папку `sounds` с тестовым файлом `beep.wav`. Вы можете сразу запустить бота и проверить его работу в чате командой `!m beep`.

---

## Синтаксис команд в чате

Основная команда для зрителей:
`!m [имя_звука_или_код_языка]-[эффекты]`

* `!m rand` — проиграть случайный звук из папки `sounds`.

### 1. Озвучка текста (TTS)
Укажите код языка перед текстом, который нужно озвучить:
* `!m ru привет чат` — озвучить текст на русском.
* `!m jp аниме` — озвучить текст на японском.

[Полный список поддерживаемых языков](tts/languages.go)

### 2. Комбинирование (Миксы и Цепочки)
* **Микс (одновременно через `+`):** `!m sound1+sound2-rs` (звуки запустятся одновременно и оба проиграются реверсом).
* **Цепочка (последовательно через пробел):** `!m sound1-sp150 ru привет sound2` (сначала проиграется ускоренный sound1, затем по-русски озвучится слово «привет», а в конце запустится sound2).

---

## Доступные аудиоэффекты

Эффекты добавляются через дефис `-` после имени звука или кода языка (порядок не имеет значения):

| Параметр | Эффект | Диапазон | Описание |
| --- | --- | --- | --- |
| `sp[число]` | Скорость | 10 - 200 | Скорость и высота воспроизведения (норма: `100`. `sp150` — быстрее и выше, `sp50` — медленнее и ниже). |
| `cs[число]` | Срез начала | 0 - 100 | Отрезать указанный процент звука с начала (например, `cs20`). |
| `ce[число]` | Срез конца | 0 - 100 | Отрезать указанный процент звука с конца (например, `ce20`). |
| `rs` | Реверс | — | Воспроизвести звук задом наперед. |
| `lq` | Лоу-фай | — | Эффект 8-битного ретро-звука (биткрашинг). |
| `er` | Перегруз | — | Экстремальный перегруз громкости (Ear Rape). |
| `st` | Заикание | — | Эффект быстрого заикания в самом начале звука. |
| `dl` | Эхо (Delay) | — | Эффект плавного затухающего эхо. |
| `vb` | Вибрация | — | Эффект плавного дрожания частоты (Vibrato). |
| `ga` | Гача (Gacha) | — | Случайно добавляет неиспользованные эффекты. Количество зависит от того, сколько эффектов вы уже ввели вручную (если введено 3 или более, не добавится ничего, кроме редкого 5% шанса сорвать джекпот и получить +1 эффект). |
| `tr` | Перевод | — | **Только для TTS.** Переводит текст на указанный язык (например, `ru-tr hello`). |

*(Примеры: `!m ru-sp150 привет`, `!m omg-ga`)*

> ℹ️ *Примечание:* Обрезка (cs/ce) всегда применяется к исходному звуку первой, до наложения любых других эффектов.

<a name="tts-limit-ru"></a>

>  **Лимит длины TTS (200 симв.):** Из-за использования бесплатного API лимит озвучки для одного куска текста — строго 200 символов. Чтобы обойти это ограничение, склеивайте команды цепочкой:
> * ❌ `!m ru-sp150 длинный_текст_300_символов` (Плохо — обрежется до 200 симв.).
> *  `!m ru-sp150 текст_150_символов ru-sp150 текст_150_символов` (Отлично — проиграется без швов).

---

## Команды модерирования (Для Стримера и Модераторов)

| Команда | Описание |
| --- | --- |
| `!m mute` / `unmute` | Заглушить / включить бота (при mute текущие звуки обрываются, очередь очищается). |
| `!m qon` / `qoff` | Включить / выключить очередь (при `qoff` звуки в чате накладываются параллельно). |
| `!m eron` / `eroff` | Включить / выключить глобальную безопасность ушей (блокирует эффект `er` для всех). |
| `!m stop` | Мгновенно выключить текущий звук и полностью очистить очередь. |
| `!m skip` | Прервать текущий звук и запустить следующий из очереди. |
| `!m vol [число]` | Устанавливает общую громкость бота (диапазон: 0-200, норма: 100). Значение автоматически сохраняется. |