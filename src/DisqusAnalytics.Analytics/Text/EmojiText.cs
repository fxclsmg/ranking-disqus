using System.Globalization;
using System.Text;

namespace DisqusAnalytics.Analytics.Text;

/// <summary>
/// Extrai emojis do texto de um comentário.
/// Cada emoji é lido como um "elemento de texto" para que modificadores
/// façam parte do mesmo emoji ("👍🏻", "❤️"), em vez de serem contados
/// como caracteres soltos.
/// </summary>
public static class EmojiText
{
    public static IEnumerable<string> Extract(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        var elements = StringInfo.GetTextElementEnumerator(text);

        while (elements.MoveNext())
        {
            var element = (string)elements.Current;

            if (IsEmoji(element))
            {
                yield return element;
            }
        }
    }

    private static bool IsEmoji(string textElement)
    {
        foreach (var rune in textElement.EnumerateRunes())
        {
            var value = rune.Value;

            if (value is >= 0x1F000 and <= 0x1FAFF)
            {
                return true;
            }

            if (value is >= 0x2600 and <= 0x27BF)
            {
                return true;
            }

            if (value is >= 0x2B00 and <= 0x2BFF)
            {
                return true;
            }
        }

        return false;
    }
}
