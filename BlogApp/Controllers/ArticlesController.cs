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
        public async Task<IActionResult> Index(string sortOrder, int page = 1)
        {
            ViewData["DateSortParm"] = string.IsNullOrEmpty(sortOrder) ? "date_asc" : "";
            int pageSize = 5;

            var articles = from a in _context.Articles.Include(a => a.Author)
                           select a;

            articles = sortOrder == "date_asc"
                ? articles.OrderBy(a => a.CreatedAt)
                : articles.OrderByDescending(a => a.CreatedAt);

            var totalArticles = await articles.CountAsync();
            var items = await articles.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling(totalArticles / (double)pageSize);
            ViewData["SortOrder"] = sortOrder;

            return View(items);
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
        public async Task<IActionResult> Create(
            [Bind("Title,Subtitle,Content")] Article article,
            IFormFile? UploadFile,
            IFormFile? BannerImage)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                article.AuthorId = user.Id;
                article.CreatedAt = DateTime.Now;

                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                Directory.CreateDirectory(uploadsPath);

                // Uložení přílohy
                if (UploadFile != null && UploadFile.Length > 0)
                {
                    var fileName = Path.GetFileName(UploadFile.FileName);
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await UploadFile.CopyToAsync(stream);
                    }

                    article.FileName = fileName;
                }

                // Uložení bannerového obrázku
                if (BannerImage != null && BannerImage.Length > 0)
                {
                    var bannerFileName = Path.GetFileName(BannerImage.FileName);
                    var bannerFilePath = Path.Combine(uploadsPath, bannerFileName);

                    using (var stream = new FileStream(bannerFilePath, FileMode.Create))
                    {
                        await BannerImage.CopyToAsync(stream);
                    }

                    article.BannerImageFileName = bannerFileName;
                }
                else
                {
                    // Pokud nebyl nahrán obrázek, nastav náhodnou barvu
                    var random = new Random();
                    article.BannerColor = $"#{random.Next(0x1000000):X6}";
                }

                _context.Add(article);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(article);
        }




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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Subtitle,Content,CreatedAt,AuthorId")] Article updatedArticle, IFormFile? UploadFile, IFormFile? BannerImage)
        {
            if (id != updatedArticle.Id) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            bool isAdmin = User.IsInRole("Admin");
            bool isAuthor = user != null && updatedArticle.AuthorId == user.Id;

            if (!isAuthor && !isAdmin)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingArticle = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id);
                    if (existingArticle == null) return NotFound();

                    // Aktualizace polí
                    existingArticle.Title = updatedArticle.Title;
                    existingArticle.Subtitle = updatedArticle.Subtitle;
                    existingArticle.Content = updatedArticle.Content;

                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    Directory.CreateDirectory(uploadsPath);

                    if (UploadFile != null && UploadFile.Length > 0)
                    {
                        var fileName = Path.GetFileName(UploadFile.FileName);
                        var filePath = Path.Combine(uploadsPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await UploadFile.CopyToAsync(stream);
                        }

                        existingArticle.FileName = fileName;
                    }

                    if (BannerImage != null && BannerImage.Length > 0)
                    {
                        var bannerFileName = Path.GetFileName(BannerImage.FileName);
                        var bannerFilePath = Path.Combine(uploadsPath, bannerFileName);

                        using (var stream = new FileStream(bannerFilePath, FileMode.Create))
                        {
                            await BannerImage.CopyToAsync(stream);
                        }

                        existingArticle.BannerImageFileName = bannerFileName;
                    }

                    _context.Update(existingArticle);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
            }

            return View(updatedArticle);
        }




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

        public async Task<IActionResult> MyArticles(int page = 1, int pageSize = 10)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var query = _context.Articles
                .Where(a => a.AuthorId == user.Id)
                .OrderByDescending(a => a.CreatedAt);

            var totalArticles = await query.CountAsync();
            var articles = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalArticles / pageSize);

            return View(articles);
        }


    }
}
