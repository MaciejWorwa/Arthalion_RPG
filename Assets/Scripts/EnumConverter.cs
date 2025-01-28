using System;

public static class EnumConverter
{
    // Uniwersalna metoda do konwersji string na dowolny enum
    public static T? ParseEnum<T>(string value) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, true, out var result))
        {
            return result; // Zwróć wynik, jeśli konwersja się powiodła
        }

        return null; // Zwróć null, jeśli wartość jest nieprawidłowa
    }

    // Metoda z domyślną wartością, jeśli konwersja się nie uda
    public static T ParseEnumOrDefault<T>(string value, T defaultValue) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, true, out var result))
        {
            return result; // Zwróć wynik, jeśli konwersja się powiodła
        }

        return defaultValue; // Zwróć wartość domyślną
    }
}
