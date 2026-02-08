using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.AI.Interfaces
{
    /// <summary>
    /// Интерфейс для работы с речью (STT/TTS)
    /// </summary>
    public interface ISpeechProvider
    {
        /// <summary>
        /// Speech-to-Text (распознавание речи)
        /// </summary>
        Task<SpeechToTextResponse> TranscribeAudioAsync(
            SpeechToTextRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Text-to-Speech (синтез речи)
        /// </summary>
        Task<TextToSpeechResponse> GenerateSpeechAsync(
            TextToSpeechRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Название провайдера
        /// </summary>
        string ProviderName { get; }
    }

    /// <summary>
    /// Запрос на распознавание речи
    /// </summary>
    public class SpeechToTextRequest
    {
        /// <summary>
        /// Аудио данные (OGG, MP3, WAV)
        /// </summary>
        public byte[] AudioData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Формат аудио
        /// </summary>
        public string AudioFormat { get; set; } = "ogg";

        /// <summary>
        /// Язык (en-US, ru-RU)
        /// </summary>
        public string Language { get; set; } = "en-US";

        /// <summary>
        /// Нужен ли анализ произношения
        /// </summary>
        public bool AnalyzePronunciation { get; set; } = false;
    }

    /// <summary>
    /// Результат распознавания речи
    /// </summary>
    public class SpeechToTextResponse
    {
        public string Text { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Уверенность распознавания (0-1)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Анализ произношения (если запрашивался)
        /// </summary>
        public PronunciationAnalysis? Pronunciation { get; set; }
    }

    /// <summary>
    /// Запрос на синтез речи
    /// </summary>
    public class TextToSpeechRequest
    {
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Язык и голос
        /// </summary>
        public string Voice { get; set; } = "en-US-Neural"; // или "alena" для Yandex

        /// <summary>
        /// Скорость речи (0.5 - 2.0)
        /// </summary>
        public double Speed { get; set; } = 1.0;

        /// <summary>
        /// Формат вывода
        /// </summary>
        public string OutputFormat { get; set; } = "ogg_opus";
    }

    /// <summary>
    /// Результат синтеза речи
    /// </summary>
    public class TextToSpeechResponse
    {
        public byte[] AudioData { get; set; } = Array.Empty<byte>();
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string AudioFormat { get; set; } = "ogg_opus";
    }

    /// <summary>
    /// Анализ произношения
    /// </summary>
    public class PronunciationAnalysis
    {
        /// <summary>
        /// Общая оценка (0-100)
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Детали по словам
        /// </summary>
        public List<WordPronunciation> Words { get; set; } = new();
    }

    public class WordPronunciation
    {
        public string Word { get; set; } = string.Empty;
        public int Score { get; set; }
        public string? Feedback { get; set; }
    }
}
