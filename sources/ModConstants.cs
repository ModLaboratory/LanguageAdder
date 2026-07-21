using System.IO;

namespace LanguageAdder;

public static class ModConstants
{
    /// <summary>
    /// Path to your game root directory.
    /// </summary>
    public static readonly string GamePath = Directory.GetCurrentDirectory();

    /// <summary>
    /// The name of language data folder.
    /// </summary>
    public const string DataFolderName = "Language_Data";

    /// <summary>
    /// Path to the language data folder.
    /// </summary>
    public static readonly string DataFolderPath = Path.Combine(GamePath, DataFolderName);

    public static string LegacyCurrentExampleLanguageFileName => $"{TranslationController.Instance.currentLanguage.languageID}_Example.lang";
    public static string LegacyExampleLanguageFilePath => Path.Combine(DataFolderPath, LegacyCurrentExampleLanguageFileName);

    public const string LegacyRegisteredLanguageFileName = "Languages.json";
    public static string LegacyRegisteredLanguageFilePath => Path.Combine(DataFolderPath, LegacyRegisteredLanguageFileName);

    public const string LastLanguageFileName = "LastLanguage.dat";
    public static string LastLanguageFilePath => Path.Combine(DataFolderPath, LastLanguageFileName);

    public const string TranslationDataFileName = "Translation.json";
    public const string CustomReplacementRuleFileName = "ReplacementConfigs.json";
}
