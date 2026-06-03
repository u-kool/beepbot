[RU](#beepbot-russian-version)

# beepbot

This project was inspired by the original `funnebot` by `@Chazoshtare`, written in Rust. The core concept of triggered sounds and audio effects was adapted and rewritten from scratch in Go to explore concurrent programming. It has been extended with native sequential queuing and simultaneous sound mixing.

`beepbot` is a lightweight Twitch sound bot designed to play audio files with real-time effects. It runs on an event-driven model using audio driver callbacks, ensuring minimal CPU usage. It connects anonymously to Twitch IRC, requiring no security OAuth tokens to read chat commands.

---

## Setup & Launch

1. Download the archive from the **Releases** section and unpack it.
2. Open `config.env` with a text editor and enter your Twitch channel name:

   `CHANNEL=your_channel_name`

3. The release includes a pre-created `sounds` folder with a default `test.wav` file so the bot can start successfully. You can run the executable immediately.

4. When updating to a new version, you only need to replace the old `beepbot.exe` file with the new one. Do not overwrite your configured `config.env` file or the `sounds` folder to avoid losing your data.

### Audio Requirements

**Important:** All audio files must be in **`.wav`** format.
> 
> * **Sample Rate:** It is highly recommended to use files with a sample rate of **`44100 Hz`**. The bot can load and play files with other sample rates (e.g., 48000 Hz), but they will play slower and lower because the audio engine is strictly configured for 44100 Hz.
> * **File Duration:** The bot is optimized for short sound effects (memes) ranging from 1 to 10 seconds. Using long music tracks (several minutes long) is highly discouraged, as the bot caches all audio files into RAM for instant, latency-free playback. Loading many long files can significantly overload your computer's RAM.
> * **Trigger Commands:** The filename (excluding the `.wav` extension) becomes the command. For example, `omg.wav` is triggered via `!m omg`.

---

## Chat Commands

The main command for viewers is:
`!m [sound_name]-[effects]`

* `!m rand` — play a random sound from the `sounds` folder.

### Admin Commands (Broadcaster & Moderators Only)

All state changes are automatically logged to the console with timestamps:

| Command | Description |
| --- | --- |
| `!m mute` | Mutes the bot (instantly cuts current audio, clears the queue). |
| `!m unmute` | Unmutes the bot. |
| `!m qon` | Enables sequential queue (sounds play strictly one after another). |
| `!m qoff` | Disables queue (sounds overlay and play concurrently). |
| `!m eron` | Enables the extreme volume boost (`er`) effect (enabled by default). |
| `!m eroff` | Disables the `er` effect globally to protect viewers' ears. |
| `!m stop` | Instantly stops currently playing sound and clears the entire queue. |
| `!m skip` | Instantly interrupts current sound and plays the next queued item. |

---

## Audio Effects

Viewers can modify sounds by appending parameters separated by a hyphen `-` after the sound name. The order of parameters does not matter.

| Parameter | Effect | Range | Description |
| --- | --- | --- | --- |
| `sp[value]` | Speed | 10 - 200 | Playback speed and pitch (Default: 100. `sp150` is faster, `sp50` is slower). |
| `cs[value]` | Cut start | 0 - 100 | Skips the specified percentage of the sound from the start (e.g., `cs20`). |
| `ce[value]` | Cut end | 0 - 100 | Cuts the specified percentage of the sound from the end (e.g., `ce20`). |
| `rs` | Reverse | - | Plays the sound backward. |
| `lq` | Low Quality | - | Applies an 8-bit retro sound effect (bitcrushing). |
| `er` | Ear Rape | - | Applies an extreme volume overdrive (if not globally disabled by `eroff`). |
| `dl` | Delay | - | Applies a decaying echo effect. |
| `st` | Stutter | - | Applies a rapid stutter effect to the beginning of the sound. |

> ℹ️ *Note:* Trimming parameters (`sk` and `ct`) are always applied to the original sound first, before applying reverse (`rs`) or speed (`sp`) modifications.

---

## Combination Scenarios

### 1. Simultaneous Mixing (Layering)
Sounds joined by a plus sign **`+`** will be mixed and played **simultaneously**. All specified parameters at the end are applied to the entire resulting mix.
* *Example:* `!m sound1+sound2+sound3-rs-lq-ct50` 
  (sound1, sound2, and sound3 will start together. The combined mix will be cut in half and played backward).

### 2. Sequential Chain
Sounds separated by a **space** will play sequentially (one after another) within a single chat message. You can combine single sounds and mixed sounds in a chain.
* *Example:* `!m sound1-sp50-rs sound2+sound3-st sound4-sk40`
  (First, a slowed-down and reversed sound1 plays. Immediately after, sound2 and sound3 play together with a stutter. Finally, sound4 plays skipping its first 40%).

***

<a name="beepbot-russian-version"></a>

# beepbot

Этот проект вдохновлен оригинальным ботом `funnebot` от `@Chazoshtare`, написанным на Rust. Я взял его идею воспроизведения звуков в чате и наложения на них различных аудиоэффектов, переписал всё с нуля на Go и добавил собственный функционал очереди воспроизведения и одновременного микширования звуков.

`beepbot` — это легкий Twitch-бот для проигрывания аудиофайлов со звуковыми эффектами в реальном времени. Он не нагружает ваш компьютер фоновыми процессами и работает полностью анонимно (вам не нужно вводить свои пароли или секретные OAuth-токены для чтения чата).

---

## Настройка и запуск

1. Скачайте архив с ботом из раздела **Releases** и распакуйте его в любую папку.
2. Откройте файл `config.env` текстовым редактором и впишите имя вашего Twitch-канала после знака равно:

   `CHANNEL=имя_вашего_канала`

3. В релизном архиве уже создана папка `sounds` и в неё вложен тестовый файл `test.wav`, чтобы бот запустился без ошибок. Вы можете запускать бота и сразу тестировать его.
4. При выходе новой версии достаточно заменить старый файл `beepbot.exe` на новый. Не перезаписывайте уже настроенный файл `config.env` и папку `sounds`, чтобы не потерять свои данные.

### Требования к звукам

**Важно:** Все новые звуки должны быть в формате **`.wav`**.
> 
> * **Частота дискретизации:** Настоятельно рекомендуется использовать файлы с частотой дискретизации **`44100 Гц`**. Бот сможет проиграть файлы и с другой частотой (например, 48000 Гц), но они будут звучать медленнее и ниже, так как аудиодвижок бота жестко настроен на 44100 Гц.
> * **Длительность файлов:** Бот оптимизирован для работы с короткими звуковыми эффектами (мемами) длительностью от 1 до 10 секунд. Использование длинных музыкальных треков (по несколько минут) настоятельно не рекомендуется, так как бот кэширует все файлы в оперативную память для мгновенного воспроизведения без задержек. Загрузка большого количества длинных файлов может перегрузить ОЗУ вашего компьютера.
> * **Команды вызова:** Название файла (без расширения `.wav`) становится командой для его вызова в чате. Например, файл `omg.wav` будет вызываться командой `!m omg`.

---

## Синтаксис команд в чате

Основная команда для зрителей:
`!m [имя_звука]-[эффекты]`

* `!m rand` — воспроизвести случайный звук из папки `sounds`.

### Команды администрирования (Доступны только Стримеру и Модераторам)

Команды управления состоянием выводят подробный статус работы бота прямо в консоль с временными метками:

| Команда | Описание |
| --- | --- |
| `!m mute` | Заглушить бота (текущий звук мгновенно обрывается, а очередь полностью очищается). |
| `!m unmute` | Снять заглушение. |
| `!m qon` | Включить режим очереди (звуки проигрываются строго друг за другом). |
| `!m qoff` | Выключить режим очереди (звуки накладываются параллельно сразу по мере поступления). |
| `!m eron` | Разрешить использование эффекта экстремального перегруза `er` (по умолчанию включено). |
| `!m eroff` | Полностью заблокировать эффект `er` на уровне бота для защиты ушей зрителей. |
| `!m stop` | Мгновенно выключить текущий звук и полностью очистить очередь. |
| `!m skip` | Мгновенно прервать текущий играющий звук и запустить следующий из очереди (сама очередь сохраняется). |

---

## Доступные аудиоэффекты

Зрители могут модифицировать звуки, указывая параметры через дефис `-` после имени звука. Порядок указания параметров не имеет значения.

| Параметр | Эффект | Диапазон | Описание |
| --- | --- | --- | --- |
| `sp[число]` | Скорость | 10 - 200 | Скорость и высота воспроизведения (Норма: 100. `sp150` — быстрее и выше, `sp50` — медленнее и ниже). |
| `cs[число]` | Срез начала | 0 - 100 | Срезать указанный процент звука с начала (например, `cs20` срежет первые 20% длины). |
| `ce[число]` | Срез конца | 0 - 100 | Срезать указанный процент звука с конца (например, `ce20` отрежет последние 20% длины). |
| `rs` | Реверс | - | Проиграть звук задом наперед. |
| `lq` | Лоу-фай | - | Эффект 8-битного ретро-звука (биткрашинг). |
| `er` | Перегруз | - | Экстремальный перегруз громкости (работает, только если не заблокирован командой `eroff`). |
| `dl` | Эхо (Delay) | - | Эффект плавного затухающего эхо. |
| `st` | Заикание | - | Эффект быстрого заикания в самом начале звука. |

> ℹ️ *Примечание:* Параметры обрезки (`sk` и `ct`) всегда применяются к исходному звуку первыми, а уже затем накладываются эффекты реверса (`rs`) и изменения скорости (`sp`).

---

## Продвинутые сценарии комбинирования

### 1. Одновременное наложение (Микширование)
Звуки, соединенные знаком плюс **`+`**, будут автоматически смешаны и воспроизведутся **одновременно**. Все указанные в конце параметры эффектов применятся ко всему получившемуся миксу целиком.
* *Пример:* `!m sound1+sound2+sound3-rs-lq-ct50` 
  (звуки sound1, sound2 и sound3 запустятся одновременно, а получившийся общий микс будет урезан наполовину и развернут задом наперед).

### 2. Последовательная цепочка
Звуки, разделенные обычным **пробелом**, будут воспроизводиться последовательно (один за другим) в рамках одного сообщения. Вы можете комбинировать одиночные звуки и миксы в одну длинную цепочку.
* *Пример:* `!m sound1-sp50-rs sound2+sound3-st sound4-sk40`
  (Сначала проиграется замедленный и развернутый sound1, сразу после его окончания одновременно сыграют sound2 и sound3 с эффектом заикания, а в конце запустится sound4 без первых 40% длины).

***
