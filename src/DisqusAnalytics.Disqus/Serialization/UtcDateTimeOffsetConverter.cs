using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DisqusAnalytics.Disqus.Serialization;

/// <summary>
/// O Disqus devolve os horários em UTC, mas sem o indicador de fuso
/// ("2025-08-18T15:44:28"). O conversor padrão do .NET interpretaria
/// esse valor como hora local e o resultado ficaria 3 horas adiantado
/// no horário de Brasília. Este conversor lê o valor como UTC.
/// </summary>
public sealed class UtcDateTimeOffsetConverter
    : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        return DateTimeOffset.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal
                | DateTimeStyles.AdjustToUniversal);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
