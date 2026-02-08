using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Configs
{
    /// <summary>
    /// Конфигурация Speech сервисов
    /// </summary>
    public class SpeechConfiguration
    {
        public const string SectionName = "Speech";

        public string Provider { get; set; } = "YandexSpeechKit";

        public YandexSpeechKitConfig YandexSpeechKit { get; set; } = new();
        public OpenAISpeechConfig OpenAI { get; set; } = new();
    }

    public class YandexSpeechKitConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string FolderId { get; set; } = string.Empty;
        public string SttUrl { get; set; } = "https://stt.api.cloud.yandex.net/speech/v1/stt:recognize";
        public string TtsUrl { get; set; } = "https://tts.api.cloud.yandex.net/speech/v1/tts:synthesize";
        public string Language { get; set; } = "en-US";
        public string Voice { get; set; } = "jane";
        public double Speed { get; set; } = 1.0;
    }

    public class OpenAISpeechConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string SttModel { get; set; } = "whisper-1";
        public string TtsModel { get; set; } = "tts-1";
        public string Voice { get; set; } = "alloy";
    }
}
