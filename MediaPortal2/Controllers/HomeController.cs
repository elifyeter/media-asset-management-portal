using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MediaPortal2.Models;
using MediaPortal2.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace MediaPortal2.Controllers
{
    [Authorize] 
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            ViewBag.IsAdmin = isAdmin;

            var query = _context.MediaAssets.Include(m => m.ApplicationUser).AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(m => m.UserId == user.Id);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                query = query.Where(m => m.Title.ToLower().Contains(searchString) ||
                                         m.Tags.ToLower().Contains(searchString));
                ViewBag.CurrentSearch = searchString;
            }

            int pageSize = 8;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var data = await query.OrderByDescending(m => m.UploadDate)
                                  .Skip((page - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(data);
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, string title, string tags)
        {
            if (file == null || file.Length == 0) return View();

            var user = await _userManager.GetUserAsync(User);

            var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", user.Id);

            if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsRoot, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var media = new MediaAsset
            {
                FileName = fileName,
                FilePath = $"/uploads/{user.Id}/{fileName}", // Web eriþim yolu
                MediaType = file.ContentType.StartsWith("image") ? "image" : "video",
                Title = title ?? file.FileName, // Baþlýk girilmezse dosya adý olsun
                Tags = tags,
                UserId = user.Id
            };

            _context.MediaAssets.Add(media);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var media = await _context.MediaAssets.FindAsync(id);
            if (media == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (!isAdmin && media.UserId != user.Id) return Forbid();

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", media.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath)) System.IO.File.Delete(physicalPath);

            _context.MediaAssets.Remove(media);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var media = await _context.MediaAssets.FindAsync(id);
            if (media == null) return NotFound();
            return View(media);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string title, string tags)
        {
            var media = await _context.MediaAssets.FindAsync(id);
            if (media != null)
            {
                media.Title = title;
                media.Tags = tags;
                _context.Update(media);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}