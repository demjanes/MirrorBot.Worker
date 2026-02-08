using MirrorBot.Worker.Data.Models.English;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MirrorBot.Worker.Services.AI.Interfaces;
using MirrorBot.Worker.Services.English.Prompts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.English
{
    /// <summary>
    /// Основной сервис английского тьютора
    /// </summary>
    public class EnglishTutorService : IEnglishTutorService
    {
        private readonly IAIProvider _aiProvider;
        private readonly ISpeechProvider _speechProvider;
        private readonly ConversationManager _conversationManager;
        private readonly GrammarAnalyzer _grammarAnalyzer;
        private readonly VocabularyExtractor _vocabularyExtractor;
        private readonly IVocabularyRepository _vocabularyRepo;
        private readonly IUserProgressRepository _progressRepo;
        private readonly ISubscriptionRepository _subscriptionRepo;
        private readonly IUserSettingsRepository _settingsRepo;
        private readonly ILogger<EnglishTutorService> _logger;

        public EnglishTutorService(
            IAIProvider aiProvider,
            ISpeechProvider speechProvider,
            ConversationManager conversationManager,
            GrammarAnalyzer grammarAnalyzer,
            VocabularyExtractor vocabularyExtractor,
            IVocabularyRepository vocabularyRepo,
            IUserProgressRepository progressRepo,
            ISubscriptionRepository subscriptionRepo,
            IUserSettingsRepository settingsRepo,
            ILogger<EnglishTutorService> logger)
        {
            _aiProvider = aiProvider;
            _speechProvider = speechProvider;
            _conversationManager = conversationManager;
            _grammarAnalyzer = grammarAnalyzer;
            _vocabularyExtractor = vocabularyExtractor;
            _vocabularyRepo = vocabularyRepo;
            _progressRepo = progressRepo;
            _subscriptionRepo = subscriptionRepo;
            _settingsRepo = settingsRepo;
            _logger = logger;
        }

        public async Task<EnglishTutorResponse> ProcessTextMessageAsync(
     long userId,
     string botId,
     string userMessage,
     CancellationToken cancellationToken = default)
        {
            try
            {
                // Проверка подписки
                if (!await _subscriptionRepo.CanSendMessageAsync(userId, cancellationToken))
                {
                    return new EnglishTutorResponse
                    {
                        Success = false,
                        ErrorMessage = "Message limit reached. Please upgrade your subscription."
                    };
                }

                // Получить диалог
                var conversation = await _conversationManager.GetOrCreateConversationAsync(
                    userId, botId, cancellationToken: cancellationToken);

                // Добавить сообщение пользователя
                await _conversationManager.AddUserMessageAsync(
                    conversation, userMessage, cancellationToken: cancellationToken);

                // Получить контекст
                var contextMessages = await _conversationManager.GetContextMessagesAsync(
                    conversation, cancellationToken);

                // Анализ грамматики (возвращает List<GrammarCorrection> из Interfaces)
                var corrections = await _grammarAnalyzer.AnalyzeAsync(
                    userMessage, cancellationToken);

                // Генерация ответа от AI
                var systemPrompt = EnglishTutorPrompts.GetPromptByMode(conversation.Mode);
                var aiRequest = new AIRequest
                {
                    SystemPrompt = systemPrompt,
                    Messages = contextMessages,
                    Temperature = 0.7,
                    MaxTokens = 1000
                };

                var aiResponse = await _aiProvider.GenerateResponseAsync(
                    aiRequest, cancellationToken);

                if (!aiResponse.Success)
                {
                    return new EnglishTutorResponse
                    {
                        Success = false,
                        ErrorMessage = aiResponse.ErrorMessage
                    };
                }

                // Извлечь новые слова
                var newWords = await _vocabularyExtractor.ExtractAsync(
                    userMessage, aiResponse.Content, cancellationToken);

                // Сохранить ответ ассистента
                await _conversationManager.AddAssistantMessageAsync(
                    conversation,
                    aiResponse.Content,
                    corrections,
                    aiResponse.TokensUsed,
                    cancellationToken: cancellationToken);

                // Обновить прогресс
                await _progressRepo.IncrementMessagesAsync(userId, false, cancellationToken);
                if (corrections.Count > 0)
                {
                    await _progressRepo.AddCorrectionsAsync(
                        userId, corrections.Count, cancellationToken);
                }

                // Добавить слова в словарь (если включено в настройках)
                var settings = await _settingsRepo.GetByUserIdAsync(userId, cancellationToken);
                if (settings?.AutoAddToVocabulary == true)
                {
                    await AddWordsToVocabularyAsync(userId, newWords, cancellationToken);
                }

                // Использовать сообщение из лимита
                await _subscriptionRepo.UseMessageAsync(userId, cancellationToken);

                // Получить настройки для голоса
                byte[]? voiceResponse = null;
                if (settings?.AutoVoiceResponse == true)
                {
                    var ttsRequest = new TextToSpeechRequest
                    {
                        Text = aiResponse.Content,
                        Voice = settings.PreferredVoice,
                        Speed = settings.SpeechSpeed
                    };

                    var ttsResponse = await _speechProvider.GenerateSpeechAsync(
                        ttsRequest, cancellationToken);

                    if (ttsResponse.Success)
                    {
                        voiceResponse = ttsResponse.AudioData;
                    }
                }

                // ✅ ИСПРАВЛЕНО
                return new EnglishTutorResponse
                {
                    TextResponse = aiResponse.Content,
                    VoiceResponse = voiceResponse,
                    Corrections = corrections, // ✅ Уже правильного типа
                    NewVocabulary = newWords.Select(w => w.Word).ToList(),
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing text message");
                return new EnglishTutorResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<EnglishTutorResponse> ProcessVoiceMessageAsync(
            long userId,
            string botId,
            byte[] audioData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Проверка подписки
                if (!await _subscriptionRepo.CanSendMessageAsync(userId, cancellationToken))
                {
                    return new EnglishTutorResponse
                    {
                        Success = false,
                        ErrorMessage = "Message limit reached. Please upgrade your subscription."
                    };
                }

                // Speech-to-Text
                var sttRequest = new SpeechToTextRequest
                {
                    AudioData = audioData,
                    Language = "en-US",
                    AnalyzePronunciation = true
                };

                var sttResponse = await _speechProvider.TranscribeAudioAsync(
                    sttRequest, cancellationToken);

                if (!sttResponse.Success || string.IsNullOrWhiteSpace(sttResponse.Text))
                {
                    return new EnglishTutorResponse
                    {
                        Success = false,
                        ErrorMessage = "Could not transcribe audio. Please try again."
                    };
                }

                // Обработать как текст
                var textResponse = await ProcessTextMessageAsync(
                    userId, botId, sttResponse.Text, cancellationToken);

                // Добавить анализ произношения
                if (sttResponse.Pronunciation != null)
                {
                    textResponse.PronunciationFeedback = sttResponse.Pronunciation;

                    // Обновить прогресс произношения
                    await _progressRepo.UpdatePronunciationScoreAsync(
                        userId, sttResponse.Pronunciation.Score, cancellationToken);
                }

                // Обновить счетчик голосовых сообщений
                await _progressRepo.IncrementMessagesAsync(userId, true, cancellationToken);

                return textResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing voice message");
                return new EnglishTutorResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task SetConversationModeAsync(
            long userId,
            string botId,
            ConversationMode mode,
            CancellationToken cancellationToken = default)
        {
            var conversation = await _conversationManager.GetOrCreateConversationAsync(
                userId, botId, cancellationToken: cancellationToken);

            await _conversationManager.ChangeModeAsync(
                conversation, mode.ToString(), cancellationToken);
        }

        public async Task<UserStatistics> GetUserStatisticsAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var progress = await _progressRepo.GetByUserIdAsync(userId, cancellationToken);
            var vocabularySize = await _vocabularyRepo.GetVocabularySizeAsync(userId, cancellationToken);

            if (progress == null)
            {
                return new UserStatistics
                {
                    TotalMessages = 0,
                    VocabularySize = vocabularySize,
                    CorrectionsCount = 0,
                    CurrentLevel = "A1"
                };
            }

            return new UserStatistics
            {
                TotalMessages = progress.TotalMessages,
                VocabularySize = vocabularySize,
                CorrectionsCount = progress.TotalCorrections,
                LastActivity = progress.LastActivityUtc,
                CurrentLevel = progress.CurrentLevel
            };
        }

        private async Task AddWordsToVocabularyAsync(
            long userId,
            List<VocabularyWordDto> words,
            CancellationToken cancellationToken)
        {
            foreach (var wordDto in words)
            {
                var word = new VocabularyWord
                {
                    Word = wordDto.Word,
                    Translation = wordDto.Translation,
                    Context = wordDto.Context,
                    AddedAtUtc = DateTime.UtcNow
                };

                await _vocabularyRepo.AddWordAsync(userId, word, cancellationToken);
            }

            // Обновить размер словаря в прогрессе
            var vocabularySize = await _vocabularyRepo.GetVocabularySizeAsync(userId, cancellationToken);
            await _progressRepo.UpdateVocabularySizeAsync(userId, vocabularySize, cancellationToken);
        }
    }
}
