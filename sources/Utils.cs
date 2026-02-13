namespace LanguageAdder
{
    public static class Utils
    {
        public static TranslatedImageSet ToTranslationImageSet(this SupportedLangs lang) => Patch.ToTranslationImageSet(lang);
        public static SupportedLangs? ToSupportedLangs(this TranslatedImageSet lang) => Patch.ToSupportedLangs(lang);
    }
}
