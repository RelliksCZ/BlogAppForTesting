using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogApp.Models;
using System.Collections.Generic;
using System.Linq;
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

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("ArticlesApiController je načten");
        }

        // GET: /api/articles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ArticleDto>>> GetArticles()
        {
            var articles = await _context.Articles
                .Include(a => a.Author)
                .Select(a => new ArticleDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Subtitle = a.Subtitle,
                    Content = a.Content,
                    CreatedAt = a.CreatedAt,
                    Author = a.Author != null ? a.Author.UserName : "Neznámý",
                    FileName = a.FileName != null ? a.FileName : "Bez přílohy"
                })
                .ToListAsync();

            return Ok(articles);
        }

        // GET: /api/articles/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ArticleDto>> GetArticle(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Author)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
                return NotFound();

            var dto = new ArticleDto
            {
                Id = article.Id,
                Title = article.Title,
                Subtitle = article.Subtitle,
                Content = article.Content,
                CreatedAt = article.CreatedAt,
                Author = article.Author != null ? article.Author.UserName : "Neznámý",
                FileName = article.FileName != null ? article.FileName : "Bez přílohy"
            };

            return Ok(dto);
        }

        // POST: /api/articles
        [HttpPost]
        public async Task<ActionResult<ArticleDto>> PostArticle([FromBody] Article article)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            article.CreatedAt = DateTime.Now;
            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            var dto = new ArticleDto
            {
                Id = article.Id,
                Title = article.Title,
                Subtitle = article.Subtitle,
                Content = article.Content,
                CreatedAt = article.CreatedAt,
                Author = "Neznámý", // nebo najdi autora podle AuthorId
                FileName = article.FileName
            };

            return CreatedAtAction(nameof(GetArticle), new { id = article.Id }, dto);
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
