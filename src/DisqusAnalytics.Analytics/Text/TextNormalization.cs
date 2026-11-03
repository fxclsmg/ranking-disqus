using System.Globalization;
using System.Text;

namespace DisqusAnalytics.Analytics.Text;

public static class TextNormalization
{
    /// <summary>
    /// Remove os acentos de um texto.
    /// Ex.: "não" -> "nao".
    /// Utilizado para comparar palavras sem depender da acentuação.
    /// </summary>
    public static string RemoveDiacritics(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var decomposed = value.Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}
