using System.Text;
using AchApi.Data;

namespace AchApi.Services;

public static class NachaFileGenerator
{
    private const string OdfiRouting = "07600001";
    private const string CompanyId = "1234567890";
    private const string CompanyName = "HERACLES LLC       ";
    private const string ImmediateDest = " 076000010";
    private const string ImmediateOrigin = "1234567890";

    public static string Generate(AchFile file)
    {
        var sb = new StringBuilder();
        var effectiveDate = DateTime.UtcNow.ToString("yyMMdd");
        var creationDate = DateTime.UtcNow.ToString("yyMMdd");
        var creationTime = DateTime.UtcNow.ToString("HHmm");
        var batchNumber = "0000001";
        var serviceClass = "200";
        long totalCredits = 0, totalDebits = 0;
        long entryHash = 0;

        sb.AppendLine(Pad(
            $"101{ImmediateDest}{ImmediateOrigin}{creationDate}{creationTime}A094101" +
            $"{"DEST BANK".PadRight(23)}{"HERACLES LLC".PadRight(23)}{"        "}", 94));

        sb.AppendLine(Pad(
            $"5{serviceClass}{CompanyName}{"".PadRight(20)}{CompanyId}PPD" +
            $"{"PAYROLL".PadRight(10)}{effectiveDate}{effectiveDate}   1{OdfiRouting}{batchNumber}", 94));

        var entries = file.Entries;
        foreach (var entry in entries)
        {
            var routing8 = entry.RoutingNumber[..8];
            var checkDigit = entry.RoutingNumber[8];
            var amountCents = (long)(entry.Amount * 100);
            entryHash += long.Parse(routing8);

            if (entry.TransactionCode == "22") totalCredits += amountCents;
            else totalDebits += amountCents;

            var traceNumber = $"{OdfiRouting}{entry.EntryId.ToString("N")[..7]}";

            sb.AppendLine(Pad(
                $"6{entry.TransactionCode}{routing8}{checkDigit}" +
                $"{entry.AccountNumber.PadRight(17)}" +
                $"{amountCents.ToString().PadLeft(10, '0')}" +
                $"{"".PadRight(15)}" +
                $"{entry.AccountHolderName.PadRight(22)}" +
                $"  0{traceNumber}", 94));
        }

        var hashMod = (entryHash % 10_000_000_000L).ToString().PadLeft(10, '0');
        sb.AppendLine(Pad(
            $"8{serviceClass}{entries.Count.ToString().PadLeft(6, '0')}{hashMod}" +
            $"{totalDebits.ToString().PadLeft(12, '0')}{totalCredits.ToString().PadLeft(12, '0')}" +
            $"{CompanyId}{"".PadRight(25)}{OdfiRouting}{batchNumber}", 94));

        var totalRecords = entries.Count + 4;
        var blockCount = (int)Math.Ceiling((totalRecords + 1) / 10.0);
        sb.AppendLine(Pad(
            $"9000001{blockCount.ToString().PadLeft(6, '0')}" +
            $"{entries.Count.ToString().PadLeft(8, '0')}{hashMod}" +
            $"{totalDebits.ToString().PadLeft(12, '0')}{totalCredits.ToString().PadLeft(12, '0')}" +
            $"{"".PadRight(39)}", 94));

        var currentLines = entries.Count + 4;
        var paddingNeeded = (10 - (currentLines % 10)) % 10;
        for (var i = 0; i < paddingNeeded; i++)
            sb.AppendLine(new string('9', 94));

        return sb.ToString();
    }

    private static string Pad(string s, int length) =>
        s.Length >= length ? s[..length] : s.PadRight(length);
}
