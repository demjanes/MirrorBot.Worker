using MirrorBot.Worker.Data.Models.English;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MirrorBot.Worker.Services.AI.Interfaces;

namespace MirrorBot.Worker.Services.English
{
    /// <summary>
    /// Менеджер для управления контекстом диалога
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
        /// Получить или создать активный диалог
        /// </summary>
        public async Task<Conversation> GetOrCreateConversationAsync(
            long userId,
            string botId,
            string mode = "Casual",
            CancellationToken cancellationToken = default)
        {
            var conversation = await _conversationRepo.GetActiveConversationAsync(
                userId, botId, cancellationToken);

            if (conversation != null)
                return conversation;

            conversation = new Conversation
            {
                UserId = userId,
                BotId = botId,
                Mode = mode,
                IsActive = true,
                Messages = new List<EnglishMessage>()
            };

            return await _conversationRepo.CreateAsync(conversation, cancellationToken);
        }

        /// <summary>
        /// Получить контекст для AI (последние N сообщений)
        /// </summary>
        public async Task<List<ChatMessage>> GetContextMessagesAsync(
            Conversation conversation,
            CancellationToken cancellationToken = default)
        {
            var recentMessages = await _conversationRepo.GetRecentMessagesAsync(
                conversation.Id,
                MaxContextMessages,
                cancellationToken);

            return recentMessages.Select(m => new ChatMessage
            {
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.TimestampUtc
            }).ToList();
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

            return await _conversationRepo.AddMessageAsync(
                conversation.Id,
                message,
                cancellationToken);
        }

        /// <summary>
        /// Добавить ответ ассистента
        /// </summary>
        public async Task<bool> AddAssistantMessageAsync(
            Conversation conversation,
            string content,
            List<GrammarCorrection> corrections, // ✅ Из Interfaces - нет конфликта!
            int tokensUsed,
            int? pronunciationScore = null,
            CancellationToken cancellationToken = default)
        {
            var message = new EnglishMessage
            {
                Role = "assistant",
                Content = content,
                // ✅ Маппим в MessageCorrection (модель БД)
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

            return await _conversationRepo.AddMessageAsync(
                conversation.Id,
                message,
                cancellationToken);
        }

        /// <summary>
        /// Сменить режим диалога
        /// </summary>
        public async Task<bool> ChangeModeAsync(
            Conversation conversation,
            string newMode,
            CancellationToken cancellationToken = default)
        {
            return await _conversationRepo.UpdateModeAsync(
                conversation.Id,
                newMode,
                cancellationToken);
        }
    }
}
