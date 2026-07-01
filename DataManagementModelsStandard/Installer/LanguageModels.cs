using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Installer
{
    /// <summary>Represents a language supported by the application.</summary>
    public class AppLanguage
    {
        public string Code { get; set; } = "";          // "en", "ar", "fr"
        public string Name { get; set; } = "";          // "English", "العربية", "Français"
        public string NativeName { get; set; } = "";    // Self-referential name
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; }
        public string FlagIcon { get; set; } = "";       // Emoji or icon path
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>Represents a single translatable string key-value pair for a specific language.</summary>
    public class TranslationString
    {
        public string Key { get; set; } = "";            // "Btn_Save", "Msg_Welcome"
        public string LanguageCode { get; set; } = "";   // "en", "ar"
        public string Value { get; set; } = "";          // The translated text
        public string Category { get; set; } = "";       // "Buttons", "Messages", "Errors"
        public string Description { get; set; } = "";    // What this string is used for
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string UpdatedBy { get; set; } = "";
    }

    /// <summary>Category grouping for translation strings.</summary>
    public class TranslationCategory
    {
        public string Name { get; set; } = "";           // "Buttons", "Messages", "Labels"
        public int StringCount { get; set; }
        public int TranslatedCount { get; set; }
        public double CompletionPercent => StringCount > 0 ? TranslatedCount * 100.0 / StringCount : 100;
    }
}
