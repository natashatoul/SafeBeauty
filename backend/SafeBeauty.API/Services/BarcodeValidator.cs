namespace SafeBeauty.API.Services;

public static class BarcodeValidator
{
    private static readonly HashSet<int> SupportedLengths = [8, 12, 13];

    public static bool TryValidate(string? barcode, out string error)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            error = "Barcode cannot be empty.";
            return false;
        }

        if (!barcode.All(char.IsAsciiDigit))
        {
            error = "Barcode must contain digits only.";
            return false;
        }

        if (!SupportedLengths.Contains(barcode.Length))
        {
            error = "Barcode must contain 8, 12, or 13 digits.";
            return false;
        }

        var isValid = barcode.Length == 8
            ? HasValidGtinCheckDigit(barcode) || IsValidUpcE(barcode)
            : HasValidGtinCheckDigit(barcode);

        error = isValid ? string.Empty : "Barcode check digit is invalid.";
        return isValid;
    }

    private static bool HasValidGtinCheckDigit(string barcode)
    {
        var sum = 0;
        var position = 0;

        for (var index = barcode.Length - 2; index >= 0; index--, position++)
        {
            var digit = barcode[index] - '0';
            sum += digit * (position % 2 == 0 ? 3 : 1);
        }

        var expectedCheckDigit = (10 - sum % 10) % 10;
        return expectedCheckDigit == barcode[^1] - '0';
    }

    private static bool IsValidUpcE(string barcode)
    {
        var expanded = ExpandUpcE(barcode);
        return expanded is not null && HasValidGtinCheckDigit(expanded);
    }

    private static string? ExpandUpcE(string barcode)
    {
        var numberSystem = barcode[0];
        if (numberSystem is not ('0' or '1')) return null;

        var data = barcode.Substring(1, 6);
        var checkDigit = barcode[7];
        var lastDataDigit = data[5];

        var upcAPayload = lastDataDigit switch
        {
            '0' or '1' or '2' => $"{numberSystem}{data[..2]}{lastDataDigit}0000{data[2..5]}",
            '3' => $"{numberSystem}{data[..3]}00000{data[3..5]}",
            '4' => $"{numberSystem}{data[..4]}00000{data[4]}",
            _ => $"{numberSystem}{data[..5]}0000{lastDataDigit}"
        };

        return $"{upcAPayload}{checkDigit}";
    }
}
