using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Events;
using MirrorBot.Worker.Flow.UI.Models;
using MongoDB.Bson.Serialization.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MirrorBot.Worker.Data.Models.Core
{
    /// <summary>
    /// Контекст выполнения задачи бота (runtime-объект, не сохраняется в БД)
    /// </summary>
    [BsonIgnoreExtraElements]
    public class BotTask : BaseEntity
    {
        // ============================================
        // ДАННЫЕ ИЗ БД
        // ============================================

        /// <summary>
        /// Пользователь
        /// </summary>
        [BsonElement("user")]
        public User? User { get; set; }

        /// <summary>
        /// Бот-зеркало
        /// </summary>
        [BsonElement("mirrorBot")]
        public BotMirror? BotMirror { get; set; }

        // ============================================
        // TELEGRAM RUNTIME (не сохраняется в БД)
        // ============================================

        /// <summary>
        /// Клиент Telegram Bot API
        /// </summary>
        [BsonIgnore]
        public ITelegramBotClient? TgClient { get; set; }

        /// <summary>
        /// Входящий Update от Telegram
        /// </summary>
        [BsonIgnore]
        public Update? TgUpdate { get; set; }

        /// <summary>
        /// ID чата
        /// </summary>
        [BsonIgnore]
        public long? TgChatId { get; set; }

        /// <summary>
        /// Сообщение от пользователя
        /// </summary>
        [BsonIgnore]
        public Message? TgMessage { get; set; }

        /// <summary>
        /// Callback Query (нажатие на кнопку)
        /// </summary>
        [BsonIgnore]
        public CallbackQuery? TgCallbackQuery { get; set; }

        /// <summary>
        /// Текст сообщения от пользователя
        /// </summary>
        [BsonIgnore]
        public string? TgUserText { get; set; }

        // ============================================
        // КОНТЕКСТ БОТА (runtime)
        // ============================================

        /// <summary>
        /// Контекст бота
        /// </summary>
        [BsonIgnore]
        public BotContext? BotContext { get; set; }

        /// <summary>
        /// Событие посещения пользователя
        /// </summary>
        [BsonIgnore]
        public UserSeenEvent? UserSeenEvent { get; set; }

        // ============================================
        // ОТВЕТ (runtime)
        // ============================================

        /// <summary>
        /// Текст ответа пользователю
        /// </summary>
        [BsonIgnore]
        public string? AnswerText { get; set; }

        /// <summary>
        /// Клавиатура ответа
        /// </summary>
        [BsonIgnore]
        public IReplyMarkup? AnswerKeyboard { get; set; }

        // ============================================
        // COMPUTED PROPERTIES
        // ============================================

        /// <summary>
        /// Язык ответа (берется из пользователя)
        /// </summary>
        [BsonIgnore]
        public UiLang AnswerLang
        {
            get
            {
                if (User is null) return UiLang.Ru;
                return User.PreferredLang;
            }
        }
    }



    //public class TaskEntity : BaseEntity
    //{
    //    public UserEntity? userEntity { get; set; }  
    //    public MirrorBotEntity? mirrorBotEntity { get; set; }

    //    public ITelegramBotClient? tGclient { get; set; }
    //    public Update? tGupdate { get; set; }
    //    public long? tGchatId { get; set; }
    //    public Message? tGmessage { get; set; }
    //    public CallbackQuery? tGcallbackQuery { get; set; }
    //    public string? tGuserText { get; set; }


    //    public BotContext? botContext { get; set; }
    //    public UserSeenEvent? userSeenEvent { get; set; }

    //    public string? answerText { get; set; }
    //    public IReplyMarkup? answerKbrd { get; set; }


    //    public UiLang answerLang 
    //    { 
    //        get
    //        {
    //            if (userEntity is null) return UiLang.Ru;
    //            else 
    //                return userEntity.PreferredLang;
    //        }
    //    }  


    //}
}
