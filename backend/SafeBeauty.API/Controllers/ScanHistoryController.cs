using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeBeauty.API.Data;
using SafeBeauty.API.DTOs;
using SafeBeauty.API.Models;

namespace SafeBeauty.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ScanHistoryController : ControllerBase
{
    private readonly SafeBeautyDbContext _context;

    public ScanHistoryController(SafeBeautyDbContext context)
    {
        _context = context;
    }

    // The JWT carries the user's Id as ClaimTypes.NameIdentifier (see AccountController.GenerateJwtToken).
    // [Authorize] already guarantees this claim exists, so no null-check is needed here.
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    // use in  HomePage.jsx, HistoryPage.jsx
    public async Task<IActionResult> GetAll()
    {
        var entries = await _context.ScanHistories
            .Where(h => h.UserId == CurrentUserId)
            .OrderByDescending(h => h.ScannedAt)
            .Take(20)
            .ToListAsync();

        return Ok(entries.Select(ToDto));
    }

    [HttpPost]
    // use in ScanPage, ManualInputPage
    public async Task<IActionResult> Create(ScanHistorySaveRequest request)
    {
        var entry = new ScanHistory
        {
            UserId = CurrentUserId,
            ProductName = ReadString(request.AnalysisContext, "productName"),
            Barcode = ReadString(request.AnalysisContext, "barcode"),
            IngredientJson = Pack(request),
            ScannedAt = DateTime.UtcNow
        };

        _context.ScanHistories.Add(entry);
        await _context.SaveChangesAsync();

        return Ok(ToDto(entry));
    }

    [HttpPut("{id:int}")]
    // reload after profile change
    public async Task<IActionResult> Update(int id, ScanHistorySaveRequest request)
    {
        var entry = await FindOwnedAsync(id);
        if (entry == null) return NotFound();

        entry.ProductName = ReadString(request.AnalysisContext, "productName");
        entry.Barcode = ReadString(request.AnalysisContext, "barcode");
        entry.IngredientJson = Pack(request);

        await _context.SaveChangesAsync();
        return Ok(ToDto(entry));
    }

    [HttpDelete("{id:int}")]
    // button to every hisory element
    public async Task<IActionResult> Delete(int id)
    {
        var entry = await FindOwnedAsync(id);
        if (entry == null) return NotFound();

        _context.ScanHistories.Remove(entry);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    // button at the bottom "Delete all history"
    public async Task<IActionResult> DeleteAll()
    {
        var entries = _context.ScanHistories.Where(h => h.UserId == CurrentUserId);
        _context.ScanHistories.RemoveRange(entries);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Ownership check lives in one place so every action uses the same rule:
    // a user can only ever load, update or delete their own rows.
    private Task<ScanHistory?> FindOwnedAsync(int id) =>
        _context.ScanHistories.FirstOrDefaultAsync(h => h.Id == id && h.UserId == CurrentUserId);

    private static string? ReadString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

    private static string Pack(ScanHistorySaveRequest request) =>
        JsonSerializer.Serialize(new { results = request.Results, analysisContext = request.AnalysisContext });

    private static ScanHistoryDto ToDto(ScanHistory entry)
    {
        using var doc = JsonDocument.Parse(entry.IngredientJson);
        var root = doc.RootElement;

        return new ScanHistoryDto
        {
            Id = entry.Id.ToString(),
            CreatedAt = entry.ScannedAt,
            Results = root.GetProperty("results").Clone(),
            AnalysisContext = root.GetProperty("analysisContext").Clone()
        };
    }
}
