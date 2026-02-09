using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.Referral
{
    /// <summary>
    /// Парсер реферальных кодов из /start параметров.
    /// </summary>
    public static class ReferralCodeParser
    {
        /// <summary>
        /// Пытается извлечь TelegramId владельца из start-параметра.
        /// Поддерживает форматы:
        /// - "8196455030"          → 8196455030
        /// - "friend8196455030"   → 8196455030
        /// - "ref8196455030"      → 8196455030
        /// - "owner_8196455030"   → 8196455030 (старый формат)
        /// </summary>
        /// <param name="startParameter">Параметр после /start</param>
        /// <returns>TelegramId владельца или null если не удалось распарсить</returns>
        public static long? TryParseOwnerTelegramId(string? startParameter)
        {
            if (string.IsNullOrWhiteSpace(startParameter))
                return null;

            var trimmed = startParameter.Trim();

            // Убираем известные префиксы
            if (trimmed.StartsWith("friend", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring("friend".Length);
            else if (trimmed.StartsWith("ref", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring("ref".Length);
            else if (trimmed.StartsWith("owner_", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring("owner_".Length);

            // Пытаемся распарсить число
            if (long.TryParse(trimmed, out var ownerId))
                return ownerId;

            return null;
        }
    }
}
