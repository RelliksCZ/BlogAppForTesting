using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace BlogApp.Controllers
{
    [Route("api/articles")]
    [ApiController]
    public class ArticlesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ArticlesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // TEST: /api/articles/test
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("ArticlesApiController je načten");
        }

        // GET: /api/articles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Article>>> GetArticles()
        {
            return await _context.Articles.ToListAsync();
        }

        // GET: /api/articles/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Article>> GetArticle(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null)
                return NotFound();

            return article;
        }

        // POST: /api/articles
        [HttpPost]
        public async Task<ActionResult<Article>> PostArticle([FromBody] Article article)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            article.CreatedAt = DateTime.Now;
            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetArticle), new { id = article.Id }, article);
        }

        // DELETE: /api/articles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null)
                return NotFound();

            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
