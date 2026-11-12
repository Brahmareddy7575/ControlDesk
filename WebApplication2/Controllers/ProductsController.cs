using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDBContext _context;

        public ProductsController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search = null)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    (p.prdname != null && p.prdname.ToLower().Contains(term)) ||
                    (p.prddescription != null && p.prddescription.ToLower().Contains(term)));
            }

            ViewBag.Search = search;
            var products = await query.OrderBy(p => p.prdid).ToListAsync();
            return View(products);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new product());
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(product item)
        {
            if (!ModelState.IsValid)
            {
                return View(item);
            }

            _context.Products.Add(item);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Product '{item.prdname}' added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.Products.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, product item)
        {
            if (id != item.prdid)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(item);
            }

            var existing = await _context.Products.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.prdname = item.prdname;
            existing.prddescription = item.prddescription;
            existing.price = item.price;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Product '{existing.prdname}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Products.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Products.FindAsync(id);
            if (item != null)
            {
                _context.Products.Remove(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Product '{item.prdname}' deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

