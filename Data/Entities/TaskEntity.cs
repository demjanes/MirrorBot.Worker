using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Enums;
using MirrorBot.Worker.Data.Events;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MirrorBot.Worker.Data.Entities
{
    public class TaskEntity : BaseEntity
    {
        public UserEntity? userEntity { get; set; }  
        public MirrorBotEntity? mirrorBotEntity { get; set; }

        public ITelegramBotClient? tGclient { get; set; }
        public Update? tGupdate { get; set; }
        public long? tGchatId { get; set; }
        public Message? tGmessage { get; set; }
        public CallbackQuery? tGcallbackQuery { get; set; }
        public string? tGuserText { get; set; }


        public BotContext? botContext { get; set; }
        public UserSeenEvent? userSeenEvent { get; set; }

        public string? answerText { get; set; }
        public IReplyMarkup? answerKbrd { get; set; }


        public UiLang answerLang 
        { 
            get
            {
                if (userEntity is null) return UiLang.Ru;
                else 
                    return userEntity.PreferredLang;
            }
        }  


    }
}
