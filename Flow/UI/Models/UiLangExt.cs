using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Flow.UI.Models
{
    public static class UiLangExt
    {
        public static UiLang ParseOrDefault(string? code, UiLang def = UiLang.Ru) =>
            code?.ToLowerInvariant() switch
            {
                "ru" => UiLang.Ru,
                "en" => UiLang.En,
                _ => def
            };

        public static string ToCode(this UiLang lang) => lang == UiLang.En ? "en" : "ru";
    }
}
