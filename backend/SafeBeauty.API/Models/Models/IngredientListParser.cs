using System.Text.RegularExpressions;

namespace SafeBeauty.API.Models;

public static class IngredientListParser
{
    private static readonly char[] Separators = [',', '•', ';', '\r', '\n'];
    private const char ProtectedDot = '\u2024';
    private static readonly Regex FilAbbreviation = new(
        @"F\.I\.L\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex PeriodSeparator = new(
        @"\.\s+(?=[A-Za-z])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex Whitespace = new(
        @"\s+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex AquaSynonym = new(
        @"\bAQUA\s*/\s*WATER(?:\s*/\s*EAU)?\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex TitaniumNanoSynonym = new(
        @"\bTITANIUM DIOXIDE\s*\[NANO\]\s*/\s*TITANIUM DIOXIDE\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static List<string> Parse(IEnumerable<string> entries) => entries
        .SelectMany(SplitEntry)
        .Where(entry => !string.IsNullOrWhiteSpace(entry))
        .ToList();

    // A very long entry with many words but no reliable punctuation is usually
    // an OCR/copy-paste result whose original line breaks were lost. It cannot
    // be split on every space because official INCI names contain spaces too.
    public static bool LooksLikeUnseparatedList(string entry) =>
        entry.Length >= 120 && Whitespace.Split(entry.Trim()).Length >= 12;

    public static List<string> SegmentByKnownNames(
        string entry,
        IEnumerable<string> knownNames)
    {
        var preparedEntry = PrepareForSegmentation(entry);
        var tokens = Whitespace.Split(preparedEntry)
            .Where(token => token.Length > 0)
            .ToArray();

        var candidates = knownNames
            .Select(IngredientNormalizer.Normalize)
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Select(name => new Candidate(name, Whitespace.Split(name)))
            .GroupBy(candidate => candidate.Tokens[0], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group
                .OrderByDescending(candidate => candidate.Tokens.Length)
                .ToArray(), StringComparer.Ordinal);

        var best = new Segmentation?[tokens.Length + 1];
        best[tokens.Length] = new Segmentation(0, []);

        for (var index = tokens.Length - 1; index >= 0; index--)
        {
            // Unknown tokens are retained, never discarded. Consecutive ones
            // are combined into one Unknown entry after reconstruction.
            var unknownTail = best[index + 1]!;
            var bestAtIndex = new Segmentation(
                unknownTail.Score,
                [new Segment(tokens[index], false), .. unknownTail.Segments]);

            if (candidates.TryGetValue(tokens[index], out var startingCandidates))
            {
                foreach (var candidate in startingCandidates)
                {
                    if (!Matches(tokens, index, candidate.Tokens)) continue;

                    var tail = best[index + candidate.Tokens.Length]!;
                    // Matched-token coverage is the primary goal. Squaring the
                    // length makes one complete multi-word INCI name preferable
                    // to several shorter accidental matches covering the same words.
                    var score = tail.Score
                        + candidate.Tokens.Length * 1000
                        + candidate.Tokens.Length * candidate.Tokens.Length;
                    if (score <= bestAtIndex.Score) continue;

                    bestAtIndex = new Segmentation(
                        score,
                        [new Segment(candidate.Name, true), .. tail.Segments]);
                }
            }

            best[index] = bestAtIndex;
        }

        var result = new List<string>();
        var unknownTokens = new List<string>();

        foreach (var segment in best[0]!.Segments)
        {
            if (!segment.IsKnown)
            {
                unknownTokens.Add(segment.Value);
                continue;
            }

            FlushUnknownTokens(result, unknownTokens);
            result.Add(segment.Value);
        }

        FlushUnknownTokens(result, unknownTokens);
        return result;
    }

    private static IEnumerable<string> SplitEntry(string? entry)
    {
        var protectedEntry = FilAbbreviation.Replace(
            entry ?? string.Empty,
            match => match.Value.Replace('.', ProtectedDot));

        return protectedEntry
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(part => PeriodSeparator.Split(part))
            .Select(part => part.Replace(ProtectedDot, '.').Trim())
            .Where(part => part.Length > 0);
    }

    private static string PrepareForSegmentation(string entry)
    {
        var prepared = entry.Trim().ToUpperInvariant();
        prepared = TitaniumNanoSynonym.Replace(prepared, "TITANIUM DIOXIDE (NANO)");
        prepared = AquaSynonym.Replace(prepared, "AQUA");
        prepared = prepared.Replace("[NANO]", "(NANO)", StringComparison.Ordinal);
        return Whitespace.Replace(prepared, " ").Trim().TrimEnd('.');
    }

    private static bool Matches(string[] input, int start, string[] candidate)
    {
        if (start + candidate.Length > input.Length) return false;

        for (var offset = 0; offset < candidate.Length; offset++)
        {
            if (!string.Equals(input[start + offset], candidate[offset], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static void FlushUnknownTokens(List<string> result, List<string> unknownTokens)
    {
        if (unknownTokens.Count == 0) return;
        result.Add(string.Join(' ', unknownTokens));
        unknownTokens.Clear();
    }

    private sealed record Candidate(string Name, string[] Tokens);
    private sealed record Segment(string Value, bool IsKnown);
    private sealed record Segmentation(int Score, List<Segment> Segments);
}
