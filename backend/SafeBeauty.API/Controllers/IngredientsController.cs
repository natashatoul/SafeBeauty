using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeBeauty.API.Data;
using SafeBeauty.API.DTOs;
using SafeBeauty.API.Models;
using SafeBeauty.API.Services;

namespace SafeBeauty.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientsController : ControllerBase
    {
        private readonly SafeBeautyDbContext _context;
        private readonly IngredientAnalysisService _analysisService;

        public IngredientsController(SafeBeautyDbContext context, IngredientAnalysisService analysisService)
        {
            _context = context;
            _analysisService = analysisService;
        }

        // GET: api/Ingredients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<IngredientDto>>> GetIngredients([FromQuery] string? search)
        {
            var query = _context.Ingredients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = IngredientNormalizer.Normalize(search);
                query = query.Where(i =>
                    i.NormalizedInciName.Contains(normalizedSearch));
            }

            return await query
                .Take(50)
                .Select(i => new IngredientDto
                {
                    Id = i.Id,
                    InciName = i.InciName,
                    CasNumber = i.CasNumber,
                    Function = i.Function,
                    SafetyRating = i.SafetyRating.ToString(),
                    Source = i.Source,
                    Categories = i.CategoryMappings.Select(m => new IngredientCategoryDto
                    {
                        Id = m.Category.Id,
                        Name = m.Category.Name,
                        Description = m.Category.Description
                    }).ToList()
                })
                .ToListAsync();
        }

        // GET: api/Ingredients/5
        [HttpGet("{id}")]
        public async Task<ActionResult<IngredientDto>> GetIngredient(int id)
        {
            var ingredient = await _context.Ingredients
                .Where(i => i.Id == id)
                .Select(i => new IngredientDto
                {
                    Id = i.Id,
                    InciName = i.InciName,
                    CasNumber = i.CasNumber,
                    Function = i.Function,
                    SafetyRating = i.SafetyRating.ToString(),
                    Source = i.Source,
                    Categories = i.CategoryMappings.Select(m => new IngredientCategoryDto
                    {
                        Id = m.Category.Id,
                        Name = m.Category.Name,
                        Description = m.Category.Description
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (ingredient == null)
            {
                return NotFound();
            }

            return ingredient;
        }

        // POST: api/Ingredients/analyse
        // Accept a list of INCI names and return safety classification for each
        [HttpPost("analyse")]
        public async Task<ActionResult<AnalyseResponse>> Analyse([FromBody] AnalyseRequest request)
        {
            if (request.Ingredients == null || request.Ingredients.Count == 0)
            {
                return BadRequest("Ingredient list cannot be empty.");
            }

            var response = await _analysisService.AnalyseAsync(
                request.Ingredients,
                request.UserConditions,
                request.AgeGroup,
                request.Gender);
            return Ok(response);
        }
    }
}
