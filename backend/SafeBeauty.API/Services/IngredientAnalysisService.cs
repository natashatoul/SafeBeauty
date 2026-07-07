
using Microsoft.EntityFrameworkCore;
using SafeBeauty.API.Data;
using SafeBeauty.API.DTOs;

namespace SafeBeauty.API.Services;

public class IngredientAnalysisService
{
    private readonly SafeBeautyDbContext _context;
    private readonly HuggingFaceService _huggingFace;
    private readonly AiSummaryService _aiSummary;

    // Constructor injection - SafeBeautyDbContext is provided by ASP.NET DI container
    public IngredientAnalysisService(SafeBeautyDbContext context, HuggingFaceService huggingFace, AiSummaryService aiSummary)
    {
        _context = context;
        _huggingFace = huggingFace;
        _aiSummary = aiSummary;
    }

    public async Task<AnalyseResponse> AnalyseAsync(List<string> ingredients)
    {
        var response = new AnalyseResponse();
        foreach (var name in ingredients)
        {
            var cleanedName = name.Trim().ToUpper();

            var ingredient = await _context.Ingredients
            .Include(i => i.Category)
            .ThenInclude(c => c.ConditionRules)
            .FirstOrDefaultAsync(i => i.InciName == cleanedName);

            if (ingredient == null)
            {
                var aiResult = await _huggingFace.ClassifyAsync(cleanedName);
                response.UnknownIngredients.Add(aiResult);
                continue;
            }

            var result = new IngredientResultDto
            {
                InciName = ingredient.InciName,
                SafetyRating = ingredient.SafetyRating.ToString(),
                // ?. (null-conditional operator) if categori is null return null (not program fail) if nor null - take name
                // ?? (null-coalescing operator)
                // Category = ingredient.Category != null ? ingredient.Category.Name : string.Empty
                Category = ingredient.Category?.Name ?? string.Empty, 
                Function = ingredient.Function,
                ConditionFlags = ingredient.Category?.ConditionRules.Select(cr => new ConditionFlagDto
                {
                    Condition = cr.Condition.ToString(),
                    FlagType = cr.FlagType.ToString(),
                    Notes = cr.Notes,
                    EvidenceSource = cr.EvidenceSource
                }).ToList() ?? new()  
            };
            response.Results.Add(result);
        }
        response.AiSummary = await _aiSummary.SummariseAsync(response, new List<string>());
        return response;
        
    }
}