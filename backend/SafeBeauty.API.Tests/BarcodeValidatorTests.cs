using SafeBeauty.API.Services;

namespace SafeBeauty.API.Tests;

public class BarcodeValidatorTests
{
    [Theory]
    [InlineData("4006381333931")] // EAN-13
    [InlineData("96385074")]      // EAN-8
    [InlineData("036000291452")]  // UPC-A
    [InlineData("04210007")]      // UPC-E (expands to 042000001007)
    public void TryValidate_AcceptsSupportedBarcode(string barcode)
    {
        var isValid = BarcodeValidator.TryValidate(barcode, out var error);

        Assert.True(isValid);
        Assert.Empty(error);
    }

    [Theory]
    [InlineData(null, "Barcode cannot be empty.")]
    [InlineData("", "Barcode cannot be empty.")]
    [InlineData("400638133393X", "Barcode must contain digits only.")]
    [InlineData("1234567890", "Barcode must contain 8, 12, or 13 digits.")]
    [InlineData("4006381333932", "Barcode check digit is invalid.")]
    [InlineData("96385075", "Barcode check digit is invalid.")]
    [InlineData("036000291453", "Barcode check digit is invalid.")]
    [InlineData("04210008", "Barcode check digit is invalid.")]
    public void TryValidate_RejectsInvalidBarcode(string? barcode, string expectedError)
    {
        var isValid = BarcodeValidator.TryValidate(barcode, out var error);

        Assert.False(isValid);
        Assert.Equal(expectedError, error);
    }
}
