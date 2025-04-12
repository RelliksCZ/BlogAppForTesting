using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogApp.Models;
using System.Linq;
using System.Threading.Tasks;

namespace BlogApp.Controllers
{
    [Authorize] // Vyžaduje přihlášení pro všechny akce v tomto kontroleru
    public class ArticlesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ArticlesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Articles
        [AllowAnonymous] // Seznam článků je veřejně dostupný
        public async Task<IActionResult> Index()
        {
            var articles = await _context.Articles
                .Include(a => a.Author) // Načtení autora článku
                .ToListAsync();
            return View(articles);
        }

        // GET: Articles/Details/5
        [AllowAnonymous] // Detail článku je veřejně dostupný
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var article = await _context.Articles
                .Include(a => a.Author)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (article == null) return NotFound();

            return View(article);
        }

        // GET: Articles/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Subtitle,Content")] Article article, IFormFile? UploadFile)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                article.AuthorId = user.Id;
                article.CreatedAt = DateTime.Now;

                if (UploadFile != null && UploadFile.Length > 0)
                {
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    Directory.CreateDirectory(uploadsPath);

                    var fileName = Path.GetFileName(UploadFile.FileName);
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await UploadFile.CopyToAsync(stream);
                    }

                    article.FileName = fileName;
                }

                _context.Add(article);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(article);
        }



        // GET: Articles/Edit/5
        // GET: Articles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var article = await _context.Articles.FindAsync(id);
            if (article == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            bool isAdmin = User.IsInRole("Admin");
            bool isAuthor = user != null && article.AuthorId == user.Id;

            if (!isAuthor && !isAdmin)
            {
                return Forbid(); // Zakázat přístup, pokud není autor nebo admin
            }

            return View(article);
        }

        // POST: Articles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Subtitle,Content,CreatedAt,AuthorId")] Article article)
        {
            if (id != article.Id) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            bool isAdmin = User.IsInRole("Admin");
            bool isAuthor = user != null && article.AuthorId == user.Id;

            if (!isAuthor && !isAdmin)
            {
                return Forbid(); // Zakázat přístup, pokud není autor nebo admin
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(article);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Articles.Any(e => e.Id == article.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(article);
        }



        // GET: Articles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var article = await _context.Articles.FirstOrDefaultAsync(m => m.Id == id);
            if (article == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null || (article.AuthorId != user.Id && !User.IsInRole("Admin")))
            {
                return Forbid(); // Zakázat mazání
            }

            return View(article);
        }

        // POST: Articles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null || (article.AuthorId != user.Id && !User.IsInRole("Admin")))
            {
                return Forbid(); // Uživatel může mazat jen své články, admin všechno
            }

            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
