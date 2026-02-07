using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services
{
    /// <summary>
    /// Утилита для разбиения длинных сообщений на части в соответствии с лимитами Telegram API.
    /// Telegram ограничивает длину сообщения 4096 символами.
    /// </summary>
    public static class MessageSplitter
    {
        /// <summary>
        /// Максимальная длина сообщения в Telegram (в символах).
        /// </summary>
        public const int TelegramMessageMaxLength = 4096;

        /// <summary>
        /// Разбивает текст на части размером не больше TelegramMessageMaxLength.
        /// Старается разбивать по переносам строк, чтобы не разбивать слова посередине.
        /// </summary>
        /// <param name="text">Исходный текст для разбиения.</param>
        /// <returns>Список частей текста, каждая не больше 4096 символов.</returns>
        public static List<string> Split(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string> { text ?? string.Empty };

            var result = new List<string>();

            // Если текст влезает в лимит — возвращаем как есть
            if (text.Length <= TelegramMessageMaxLength)
            {
                result.Add(text);
                return result;
            }

            // Разбиваем по частям
            var remaining = text;
            while (remaining.Length > 0)
            {
                if (remaining.Length <= TelegramMessageMaxLength)
                {
                    result.Add(remaining);
                    break;
                }

                // Берём первые 4096 символов
                var chunk = remaining.Substring(0, TelegramMessageMaxLength);

                // Пытаемся найти последний перевод строки в этом куске
                var lastNewline = chunk.LastIndexOf('\n');
                if (lastNewline > TelegramMessageMaxLength / 2)
                {
                    // Нашли перевод строки в "разумном" месте (не ближе чем на 50% от начала)
                    // Берём текст до этого перевода строки
                    chunk = chunk.Substring(0, lastNewline + 1).TrimEnd(); // убираем пробелы в конце
                    result.Add(chunk);
                    remaining = remaining.Substring(lastNewline + 1).TrimStart(); // пропускаем сам перевод строки
                }
                else
                {
                    // Не найдено подходящее место для разбиения по переносам
                    // Пытаемся разбить по пробелу
                    var lastSpace = chunk.LastIndexOf(' ');
                    if (lastSpace > TelegramMessageMaxLength / 2)
                    {
                        chunk = chunk.Substring(0, lastSpace).TrimEnd();
                        result.Add(chunk);
                        remaining = remaining.Substring(lastSpace + 1).TrimStart();
                    }
                    else
                    {
                        // Не найдено подходящего места — просто берём 4096 символов
                        result.Add(chunk);
                        remaining = remaining.Substring(TelegramMessageMaxLength);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Проверяет, превышает ли текст лимит Telegram.
        /// </summary>
        public static bool ExceedsLimit(string? text) =>
            !string.IsNullOrEmpty(text) && text.Length > TelegramMessageMaxLength;
    }
}
