using MirrorBot.Worker.Data.Models.English;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MirrorBot.Worker.Services.AI.Interfaces;

namespace MirrorBot.Worker.Services.English
{
    /// <summary>
    /// Менеджер для управления контекстом диалога (единый для всех ботов)
    /// </summary>
    public class ConversationManager
    {
        private readonly IConversationRepository _conversationRepo;
        private readonly ILogger<ConversationManager> _logger;
        private const int MaxContextMessages = 10;

        public ConversationManager(
            IConversationRepository conversationRepo,
            ILogger<ConversationManager> logger)
        {
            _conversationRepo = conversationRepo;
            _logger = logger;
        }

        /// <summary>
        /// Получить или создать единый диалог пользователя
        /// </summary>
        public async Task<Conversation> GetOrCreateConversationAsync(
            long userId,
            string botId,
            string mode = "Casual",
            CancellationToken cancellationToken = default)
        {
            var conversation = await _conversationRepo.GetByUserAsync(userId, cancellationToken);

            if (conversation != null)
            {
                // Обновляем последний бот
                conversation.LastBotId = botId;
                return conversation;
            }

            // Создаём новый единый контекст
            conversation = new Conversation
            {
                UserId = userId,
                LastBotId = botId,
                Mode = mode,
                IsActive = true,
                Messages = new List<EnglishMessage>(),
                CreatedAtUtc = DateTime.UtcNow,
                LastActivityUtc = DateTime.UtcNow
            };

            return await _conversationRepo.CreateOrUpdateAsync(conversation, cancellationToken);
        }

        /// <summary>
        /// Получить контекст для AI (последние N сообщений)
        /// </summary>
        public async Task<List<ChatMessage>> GetContextMessagesAsync(
            Conversation conversation,
            CancellationToken cancellationToken = default)
        {
            // Берём последние N сообщений из единого контекста
            var recentMessages = conversation.Messages
                .OrderByDescending(m => m.TimestampUtc)
                .Take(MaxContextMessages)
                .OrderBy(m => m.TimestampUtc)
                .Select(m => new ChatMessage
                {
                    Role = m.Role,
                    Content = m.Content,
                    Timestamp = m.TimestampUtc
                })
                .ToList();

            _logger.LogDebug(
                "Retrieved {Count} messages from unified context for user {UserId}",
                recentMessages.Count,
                conversation.UserId);

            return recentMessages;
        }

        /// <summary>
        /// Добавить сообщение пользователя
        /// </summary>
        public async Task<bool> AddUserMessageAsync(
            Conversation conversation,
            string content,
            string? voiceFileId = null,
            CancellationToken cancellationToken = default)
        {
            var message = new EnglishMessage
            {
                Role = "user",
                Content = content,
                VoiceFileId = voiceFileId,
                TimestampUtc = DateTime.UtcNow
            };

            conversation.Messages.Add(message);
            conversation.LastActivityUtc = DateTime.UtcNow;

            await _conversationRepo.CreateOrUpdateAsync(conversation, cancellationToken);

            _logger.LogDebug(
                "Added user message to unified context for user {UserId}",
                conversation.UserId);

            return true;
        }

        /// <summary>
        /// Добавить ответ ассистента
        /// </summary>
        public async Task<bool> AddAssistantMessageAsync(
            Conversation conversation,
            string content,
            List<GrammarCorrection> corrections,
            int tokensUsed,
            int? pronunciationScore = null,
            CancellationToken cancellationToken = default)
        {
            var message = new EnglishMessage
            {
                Role = "assistant",
                Content = content,
                Corrections = corrections.Select(c => new MessageCorrection
                {
                    Original = c.Original,
                    Corrected = c.Corrected,
                    Explanation = c.Explanation,
                    Type = c.Type
                }).ToList(),
                TimestampUtc = DateTime.UtcNow,
                TokensUsed = tokensUsed,
                PronunciationScore = pronunciationScore
            };

            conversation.Messages.Add(message);
            conversation.TotalTokensUsed += tokensUsed;
            conversation.LastActivityUtc = DateTime.UtcNow;

            await _conversationRepo.CreateOrUpdateAsync(conversation, cancellationToken);

            _logger.LogDebug(
                "Added assistant message to unified context for user {UserId}, tokens: {Tokens}",
                conversation.UserId,
                tokensUsed);

            return true;
        }

        /// <summary>
        /// Сменить режим диалога
        /// </summary>
        public async Task<bool> ChangeModeAsync(
            Conversation conversation,
            string newMode,
            CancellationToken cancellationToken = default)
        {
            conversation.Mode = newMode;
            conversation.LastActivityUtc = DateTime.UtcNow;

            await _conversationRepo.CreateOrUpdateAsync(conversation, cancellationToken);

            _logger.LogInformation(
                "Changed mode to {Mode} for user {UserId}",
                newMode,
                conversation.UserId);

            return true;
        }

        /// <summary>
        /// Очистить контекст (начать новый диалог)
        /// </summary>
        public async Task<bool> ClearContextAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var conversation = await _conversationRepo.GetByUserAsync(userId, cancellationToken);

            if (conversation != null)
            {
                conversation.Messages.Clear();
                conversation.LastActivityUtc = DateTime.UtcNow;
                await _conversationRepo.CreateOrUpdateAsync(conversation, cancellationToken);

                _logger.LogInformation("Cleared context for user {UserId}", userId);
                return true;
            }

            return false;
        }
    }
}
