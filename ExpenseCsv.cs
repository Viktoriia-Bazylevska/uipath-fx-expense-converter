using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ExpenseClaimConverter
{
    public static class ExpenseCsv
    {
        private static readonly char[] CharactersRequiringQuotes = { ',', '"', '\r', '\n' };

        public static IReadOnlyList<ExpenseLine> Read(string inputPath)
        {
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException("Input CSV file was not found.", inputPath);
            }

            var records = ParseRecords(File.ReadAllText(inputPath));
            if (records.Count == 0)
            {
                throw new InvalidDataException("Input CSV is empty.");
            }

            var header = records[0].Select(value => (value ?? string.Empty).Trim().Trim('\uFEFF')).ToList();
            var columnIndexes = BuildColumnIndexes(header);
            var result = new List<ExpenseLine>();

            for (var recordIndex = 1; recordIndex < records.Count; recordIndex++)
            {
                var record = records[recordIndex];
                if (IsBlankRecord(record))
                {
                    continue;
                }

                if (record.Count != header.Count)
                {
                    throw new InvalidDataException(
                        $"CSV row {recordIndex + 1} has {record.Count} fields; expected {header.Count}.");
                }

                result.Add(new ExpenseLine
                {
                    LineId = record[columnIndexes["LineId"]],
                    ExpenseDate = record[columnIndexes["ExpenseDate"]],
                    Description = record[columnIndexes["Description"]],
                    Amount = record[columnIndexes["Amount"]],
                    Currency = record[columnIndexes["Currency"]]
                });
            }

            return result;
        }

        public static void Write(
            string outputPath,
            IReadOnlyList<ExpenseLine> lines,
            decimal total,
            string targetCurrency)
        {
            var fullOutputPath = Path.GetFullPath(outputPath);
            var outputDirectory = Path.GetDirectoryName(fullOutputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            using (var writer = new StreamWriter(fullOutputPath, false, new UTF8Encoding(false)))
            {
                writer.WriteLine(SerializeRow(new[]
                {
                    "LineId", "ExpenseDate", "Description", "Amount", "Currency",
                    "Rate", "RateDate", "ConvertedAmount", "Status", "Message"
                }));

                foreach (var line in lines)
                {
                    writer.WriteLine(SerializeRow(new[]
                    {
                        line.LineId,
                        line.ExpenseDate,
                        line.Description,
                        line.Amount,
                        line.Currency,
                        FormatDecimal(line.Rate, "0.#####"),
                        line.RateDate.HasValue
                            ? line.RateDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                            : string.Empty,
                        FormatDecimal(line.ConvertedAmount, "0.00"),
                        line.Status,
                        line.Message
                    }));
                }

                var totalRow = new string[10];
                totalRow[0] = "TOTAL";
                totalRow[7] = total.ToString("0.00", CultureInfo.InvariantCulture);
                totalRow[8] = "Total";
                totalRow[9] = "Total in " + targetCurrency;
                writer.WriteLine(SerializeRow(totalRow));
            }
        }

        private static Dictionary<string, int> BuildColumnIndexes(IReadOnlyList<string> header)
        {
            var requiredColumns = new[] { "LineId", "ExpenseDate", "Description", "Amount", "Currency" };
            var indexes = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var index = 0; index < header.Count; index++)
            {
                if (!indexes.TryAdd(header[index], index))
                {
                    throw new InvalidDataException("Input CSV contains duplicate column: " + header[index]);
                }
            }

            foreach (var requiredColumn in requiredColumns)
            {
                if (!indexes.ContainsKey(requiredColumn))
                {
                    throw new InvalidDataException("Input CSV is missing column: " + requiredColumn);
                }
            }

            return indexes;
        }

        private static bool IsBlankRecord(IReadOnlyList<string> record)
        {
            return record.Count == 0 || record.All(string.IsNullOrWhiteSpace);
        }

        private static List<List<string>> ParseRecords(string content)
        {
            var records = new List<List<string>>();
            var fields = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;

            for (var index = 0; index < content.Length; index++)
            {
                var current = content[index];
                if (inQuotes)
                {
                    if (current == '"')
                    {
                        if (index + 1 < content.Length && content[index + 1] == '"')
                        {
                            field.Append('"');
                            index++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(current);
                    }
                }
                else if (current == '"' && field.Length == 0)
                {
                    inQuotes = true;
                }
                else if (current == ',')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else if (current == '\r' || current == '\n')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                    records.Add(fields);
                    fields = new List<string>();
                    if (current == '\r' && index + 1 < content.Length && content[index + 1] == '\n')
                    {
                        index++;
                    }
                }
                else
                {
                    field.Append(current);
                }
            }

            if (field.Length > 0 || fields.Count > 0 || (content.Length > 0 && content[content.Length - 1] != '\n' && content[content.Length - 1] != '\r'))
            {
                fields.Add(field.ToString());
                records.Add(fields);
            }

            if (inQuotes)
            {
                throw new InvalidDataException("Input CSV contains an unterminated quoted field.");
            }

            return records;
        }

        private static string SerializeRow(IReadOnlyList<string> values)
        {
            var builder = new StringBuilder();
            for (var index = 0; index < values.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append(Escape(values[index]));
            }

            return builder.ToString();
        }

        private static string Escape(string value)
        {
            var normalizedValue = value ?? string.Empty;
            if (normalizedValue.IndexOfAny(CharactersRequiringQuotes) < 0)
            {
                return normalizedValue;
            }

            return "\"" + normalizedValue.Replace("\"", "\"\"") + "\"";
        }

        private static string FormatDecimal(decimal? value, string format)
        {
            return value.HasValue
                ? value.Value.ToString(format, CultureInfo.InvariantCulture)
                : string.Empty;
        }
    }
}
