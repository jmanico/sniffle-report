using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace SniffleReport.Api.Services.Ingestion.Connectors;

internal static class CsvRecordReader
{
    public static IReadOnlyList<Dictionary<string, string>> Parse(string csvText)
    {
        using var parser = new TextFieldParser(new StringReader(csvText));
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;

        if (parser.EndOfData)
        {
            return [];
        }

        var headers = parser.ReadFields()?.Select(NormalizeHeader).ToArray() ?? [];
        var records = new List<Dictionary<string, string>>();

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null)
            {
                continue;
            }

            var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length && i < fields.Length; i++)
            {
                record[headers[i]] = fields[i].Trim();
            }

            records.Add(record);
        }

        return records;
    }

    public static string? GetValue(
        IReadOnlyDictionary<string, string> row,
        params string[] candidateHeaders)
    {
        foreach (var header in candidateHeaders)
        {
            var normalized = NormalizeHeader(header);
            if (row.TryGetValue(normalized, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string NormalizeHeader(string header)
    {
        var builder = new StringBuilder(header.Length);
        foreach (var character in header)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }
}
