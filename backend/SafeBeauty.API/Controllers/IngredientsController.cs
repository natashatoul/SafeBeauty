using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeBeauty.API.Data;
using SafeBeauty.API.DTOs;
using SafeBeauty.API.Models;

namespace SafeBeauty.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientsController : ControllerBase
    {
        private readonly SafeBeautyDbContext _context;

        public IngredientsController(SafeBeautyDbContext context)
        {
            _context = context;
        }

        // GET: api/Ingredients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ingredient>>> GetIngredients([FromQuery] string? search)
        {
            var query = _context.Ingredients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(i => i.InciName.Contains(search));
            return await query.Take(50).ToListAsync();
        }

        // GET: api/Ingredients/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Ingredient>> GetIngredient(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);

            if (ingredient == null)
            {
                return NotFound();
            }

            return ingredient;
        }

        // PUT: api/Ingredients/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutIngredient(int id, Ingredient ingredient)
        {
            if (id != ingredient.Id)
            {
                return BadRequest();
            }

            _context.Entry(ingredient).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!IngredientExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Ingredients
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Ingredient>> PostIngredient(Ingredient ingredient)
        {
            _context.Ingredients.Add(ingredient);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetIngredient", new { id = ingredient.Id }, ingredient);
        }

        // DELETE: api/Ingredients/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return NotFound();
            }

            _context.Ingredients.Remove(ingredient);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool IngredientExists(int id)
        {
            return _context.Ingredients.Any(e => e.Id == id);
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
            
            var response = new AnalyseResponse();
            foreach (var name in request.Ingredients)
            {
                var cleanedName = name.Trim().ToUpper();
                // Look up ingredient by INCI name 
                var ingredient = await _context.Ingredients
                .Include(i => i.Category)
                .ThenInclude(c => c.ConditionRules) // load condition rules via category
                .FirstOrDefaultAsync(i => i.InciName == cleanedName);

                if (ingredient == null)
                {
                    // If ingredient is not found in DB - will sent to Hugging Face
                    response.UnknownIngredients.Add(cleanedName);
                    continue;
                }

                var result = new IngredientResultDto
                {
                    InciName = ingredient.InciName,
                    SafetyRating = ingredient.SafetyRating.ToString(),
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
            return Ok(response);
        }
    }
}
