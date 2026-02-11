using MirrorBot.Worker.Data.Models.English;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MirrorBot.Worker.Services.AI.Implementations;
using MirrorBot.Worker.Services.AI.Interfaces;
using MirrorBot.Worker.Services.English.Prompts;
using MirrorBot.Worker.Services.Subscr;
using System.Security.Cryptography;
using System.Text;

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
        private readonly ISubscriptionService _subscriptionService; // ✅ ИЗМЕНЕНО
        private readonly IUserSettingsRepository _settingsRepo;
        private readonly ILogger<EnglishTutorService> _logger;
        private readonly ICacheService _cacheService;

        public EnglishTutorService(
            IAIProvider aiProvider,
            ISpeechProvider speechProvider,
            ConversationManager conversationManager,
            GrammarAnalyzer grammarAnalyzer,
            VocabularyExtractor vocabularyExtractor,
            IVocabularyRepository vocabularyRepo,
            IUserProgressRepository progressRepo,
            ISubscriptionService subscriptionService, // ✅ ИЗМЕНЕНО
            IUserSettingsRepository settingsRepo,
            ICacheService cacheService,
            ILogger<EnglishTutorService> logger)
        {
            _aiProvider = aiProvider;
            _speechProvider = speechProvider;
            _conversationManager = conversationManager;
            _grammarAnalyzer = grammarAnalyzer;
            _vocabularyExtractor = vocabularyExtractor;
            _vocabularyRepo = vocabularyRepo;
            _progressRepo = progressRepo;
            _subscriptionService = subscriptionService; // ✅ ИЗМЕНЕНО
            _settingsRepo = settingsRepo;
            _cacheService = cacheService;
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
                // ✅ ИЗМЕНЕНО: Проверка подписки через новый сервис
                var (canSend, errorMessage) = await _subscriptionService.CanSendMessageAsync(
                    userId,
                    isVoice: false,
                    cancellationToken);

                if (!canSend)
                {
                    return new EnglishTutorResponse
                    {
                        Success = false,
                        ErrorMessage = errorMessage
                    };
                }

                // Получить диалог
                var conversation = await _conversationManager.GetOrCreateConversationAsync(
                    userId,
                    botId,
                    cancellationToken: cancellationToken);

                // Добавить сообщение пользователя
                await _conversationManager.AddUserMessageAsync(
                    conversation,
                    userMessage,
                    cancellationToken: cancellationToken);

                // Получить контекст
                var contextMessages = await _conversationManager.GetContextMessagesAsync(
                    conversation,
                    cancellationToken);

                // Анализ грамматики (всегда выполняется, не кэшируется)
                var corrections = await _grammarAnalyzer.AnalyzeAsync(
                    userMessage,
                    cancellationToken);

                // Системный промпт по текущему режиму диалога
                var systemPrompt = EnglishTutorPrompts.GetPromptByMode(conversation.Mode);

                // Хеш контекста для кэша
                var contextHash = ComputeContextHash(systemPrompt, contextMessages);

                // Попытаться найти ответ в кэше
                var cached = await _cacheService.GetAsync(
                    userMessage,
                    conversation.Mode,
                    contextHash,
                    _aiProvider.ProviderName,
                    cancellationToken);

                string answerText;
                int tokensUsed;
                string? cachedVoiceFileId = null;
                string? cacheKey = null;

                if (cached != null)
                {
                    // КЭШ-ХИТ: используем сохранённый текст ответа
                    answerText = cached.ResponseText;
                    tokensUsed = cached.TokensUsed;
                    cachedVoiceFileId = cached.VoiceFileId;
                    cacheKey = cached.CacheKey;
                }
                else
                {
                    // КЭШ-МИСС: генерируем ответ у провайдера
                    var aiRequest = new AIRequest
                    {
                        SystemPrompt = systemPrompt,
                        Messages = contextMessages,
                        Temperature = 0.7,
                        MaxTokens = 1000
                    };

                    var aiResponse = await _aiProvider.GenerateResponseAsync(
                        aiRequest,
                        cancellationToken);

                    if (!aiResponse.Success)
                    {
                        return new EnglishTutorResponse
                        {
                            Success = false,
                            ErrorMessage = aiResponse.ErrorMessage
                        };
                    }

                    answerText = aiResponse.Content;
                    tokensUsed = aiResponse.TokensUsed;

                    // Генерируем ключ для будущего обновления voiceFileId
                    using var sha = SHA256.Create();
                    var raw = $"{conversation.Mode}||{_aiProvider.ProviderName}||{contextHash}||{userMessage}";
                    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                    cacheKey = Convert.ToHexString(bytes);

                    // Сохранить ответ в кэш (пока без voiceFileId)
                    await _cacheService.SaveAsync(
                        userMessage,
                        conversation.Mode,
                        contextHash,
                        _aiProvider.ProviderName,
                        answerText,
                        voiceFileId: null,
                        tokensUsed,
                        cancellationToken);
                }

                // Извлечь новые слова (на основе итогового текста ответа)
                var newWords = await _vocabularyExtractor.ExtractAsync(
                    userMessage,
                    answerText,
                    cancellationToken);

                // Сохранить ответ ассистента в диалоге
                await _conversationManager.AddAssistantMessageAsync(
                    conversation,
                    answerText,
                    corrections,
                    tokensUsed,
                    cancellationToken: cancellationToken);

                // Обновить прогресс
                await _progressRepo.IncrementMessagesAsync(userId, false, cancellationToken);

                if (corrections.Count > 0)
                {
                    await _progressRepo.AddCorrectionsAsync(
                        userId,
                        corrections.Count,
                        cancellationToken);
                }

                // Добавить слова в словарь (если включено в настройках)
                var settings = await _settingsRepo.GetByUserIdAsync(userId, cancellationToken);

                if (settings?.AutoAddToVocabulary == true)
                {
                    await AddWordsToVocabularyAsync(userId, newWords, cancellationToken);
                }

                // ✅ ИЗМЕНЕНО: Использовать сообщение через новый сервис
                await _subscriptionService.UseMessageAsync(
                    userId,
                    isVoice: false,
                    tokensUsed: tokensUsed,
                    cancellationToken);

                // Настройки для голосового ответа
                byte[]? voiceResponse = null;
                string? newVoiceFileId = null;

                if (settings?.AutoVoiceResponse == true)
                {
                    // Проверяем, есть ли уже кэшированный voiceFileId
                    if (!string.IsNullOrEmpty(cachedVoiceFileId))
                    {
                        // Используем кэшированный FileId (не генерируем заново)
                        newVoiceFileId = cachedVoiceFileId;
                    }
                    else
                    {
                        // Генерируем новый голосовой ответ
                        var ttsRequest = new TextToSpeechRequest
                        {
                            Text = answerText,
                            Voice = settings.PreferredVoice,
                            Speed = settings.SpeechSpeed
                        };

                        var ttsResponse = await _speechProvider.GenerateSpeechAsync(
                            ttsRequest,
                            cancellationToken);

                        if (ttsResponse.Success)
                        {
                            voiceResponse = ttsResponse.AudioData;
                        }
                    }
                }

                return new EnglishTutorResponse
                {
                    TextResponse = answerText,
                    VoiceResponse = voiceResponse,
                    CachedVoiceFileId = cachedVoiceFileId,
                    CacheKey = cacheKey,
                    Corrections = corrections,
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
                // ✅ ИЗМЕНЕНО: Проверка подписки через новый сервис
                var (canSend, errorMessage) = await _subscriptionService.CanSendMessageAsync(
                    userId,
                    isVoice: true,
                    cancellationToken);

                if (!canSend)
                {
                    return new EnglishTutorResponse
                    {
                        Success = false,
                        ErrorMessage = errorMessage
                    };
                }

                // Speech-to-Text с анализом произношения
                var sttRequest = new SpeechToTextRequest
                {
                    AudioData = audioData,
                    Language = "en-US",
                    AnalyzePronunciation = true
                };

                var sttResponse = await _speechProvider.TranscribeAudioAsync(
                    sttRequest,
                    cancellationToken);

                if (!sttResponse.Success || string.IsNullOrWhiteSpace(sttResponse.Text))
                {
                    return new EnglishTutorResponse
                    {
                        Success = false,
                        ErrorMessage = "Could not transcribe audio. Please try again."
                    };
                }

                // Получить диалог для контекста
                var conversation = await _conversationManager.GetOrCreateConversationAsync(
                    userId,
                    botId,
                    cancellationToken: cancellationToken);

                var contextMessages = await _conversationManager.GetContextMessagesAsync(
                    conversation,
                    cancellationToken);

                var systemPrompt = EnglishTutorPrompts.GetPromptByMode(conversation.Mode);
                var contextHash = ComputeContextHash(systemPrompt, contextMessages);

                // Проверить кэш
                var cached = await _cacheService.GetAsync(
                    sttResponse.Text,
                    conversation.Mode,
                    contextHash,
                    _aiProvider.ProviderName,
                    cancellationToken);

                EnglishTutorResponse textResponse;

                if (cached != null)
                {
                    // КЭШ-ХИТ: восстанавливаем ответ из кэша
                    textResponse = new EnglishTutorResponse
                    {
                        TextResponse = cached.ResponseText,
                        CachedVoiceFileId = cached.VoiceFileId,
                        CacheKey = cached.CacheKey,
                        PronunciationFeedback = CacheService.ConvertFromCachedPronunciation(
                            cached.PronunciationAnalysis)
                            ?? sttResponse.Pronunciation,
                        Success = true
                    };

                    var corrections = await _grammarAnalyzer.AnalyzeAsync(
                        sttResponse.Text,
                        cancellationToken);

                    var newWords = await _vocabularyExtractor.ExtractAsync(
                        sttResponse.Text,
                        cached.ResponseText,
                        cancellationToken);

                    textResponse.Corrections = corrections;
                    textResponse.NewVocabulary = newWords.Select(w => w.Word).ToList();
                }
                else
                {
                    // КЭШ-МИСС: обработать как обычный текст
                    textResponse = await ProcessTextMessageAsync(
                        userId,
                        botId,
                        sttResponse.Text,
                        cancellationToken);

                    if (!textResponse.Success)
                    {
                        return textResponse;
                    }

                    if (sttResponse.Pronunciation != null)
                    {
                        textResponse.PronunciationFeedback = sttResponse.Pronunciation;

                        await _cacheService.SaveWithPronunciationAsync(
                            sttResponse.Text,
                            conversation.Mode,
                            contextHash,
                            _aiProvider.ProviderName,
                            textResponse.TextResponse,
                            textResponse.CachedVoiceFileId,
                            0,
                            sttResponse.Pronunciation,
                            cancellationToken);

                        await _progressRepo.UpdatePronunciationScoreAsync(
                            userId,
                            sttResponse.Pronunciation.Score,
                            cancellationToken);
                    }
                }

                // ✅ ИЗМЕНЕНО: Использовать голосовое сообщение через новый сервис
                await _subscriptionService.UseMessageAsync(
                    userId,
                    isVoice: true,
                    tokensUsed: 0,
                    cancellationToken);

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

        private static string ComputeContextHash(string systemPrompt, List<ChatMessage> contextMessages)
        {
            var sb = new StringBuilder();

            // Включаем системный промпт
            sb.Append(systemPrompt);

            // Включаем роли и тексты сообщений (без таймстампов — они не должны влиять на кэш)
            foreach (var msg in contextMessages)
            {
                sb.Append("||")
                  .Append(msg.Role)
                  .Append(":")
                  .Append(msg.Content);
            }

            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(bytes);
        }
    }
}
