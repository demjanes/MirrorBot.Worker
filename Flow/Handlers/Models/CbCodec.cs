using System.Text;

namespace MirrorBot.Worker.Flow.Handlers.Models
{
    public class CbData(string section, string action, string[] args)
    {
        public string Section { get; set; } = section;
        public string Action { get; set; } = action;
        public string[] Args { get; set; } = args;
    }

    public static class CbCodec
    {
        public static string Pack(string section, string action, params string[] args)
        {
            var s = args is { Length: > 0 }
                ? $"{section}:{action}:{string.Join(":", args)}"
                : $"{section}:{action}";

            var bytes = Encoding.UTF8.GetByteCount(s);
            if (bytes is < 1 or > 64) // Telegram limit [web:420]
                throw new InvalidOperationException($"callback_data too long: {bytes} bytes: '{s}'");

            return s;
        }

        public static CbData? TryUnpack(string? data)
        {
            if (string.IsNullOrWhiteSpace(data)) return null;

            var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;

            var section = parts[0];
            var action = parts[1];
            var args = parts.Length > 2 ? parts[2..] : Array.Empty<string>();

            return new CbData(section, action, args);
        }
    }
}
