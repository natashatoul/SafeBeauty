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
        public async Task<ActionResult<IEnumerable<Ingredient>>> GetIngredients([FromQuery] string? search)
        {
            var query = _context.Ingredients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(i => i.InciName.Contains(search.ToUpper()));
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
            
            var response = await _analysisService.AnalyseAsync(request.Ingredients);
            return Ok(response);
            
            
        }
    }
}
