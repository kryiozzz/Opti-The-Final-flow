using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Opti.Data;
using Opti.Models;

namespace Opti.Controllers
{
    public class CustomerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper method for generating page indication (breadcrumbs)
        private void SetPageIndication(string currentPage, string parentPage = null)
        {
            var breadcrumbs = new List<Breadcrumb>
            {
                new Breadcrumb { Title = "Home", Url = Url.Action("Index", "Home") }
            };

            if (!string.IsNullOrEmpty(parentPage))
            {
                switch (parentPage)
                {
                    case "Products":
                        breadcrumbs.Add(new Breadcrumb { Title = "Products", Url = Url.Action("Index", "CustomerDashboard") });
                        break;
                    default:
                        breadcrumbs.Add(new Breadcrumb { Title = parentPage, Url = "#" });
                        break;
                }
            }

            breadcrumbs.Add(new Breadcrumb { Title = currentPage, Url = null }); // Current page (no link)

            ViewData["Breadcrumbs"] = breadcrumbs;
        }

        // GET: CustomerProduct
        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            // Set page indication (breadcrumbs)
            SetPageIndication("Products");

            // Set up ViewData for the products UI
            ViewData["Title"] = "Products";
            ViewData["CurrentPage"] = "Products";
            ViewData["SearchQuery"] = searchString;

            // Base query for products
            var products = from p in _context.Products
                           select p;

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                // Enhanced search - split the search string to search for multiple terms
                var searchTerms = searchString.ToLower().Split(' ');

                products = products.Where(p =>
                    searchTerms.All(term =>
                        p.ProductName.ToLower().Contains(term) ||
                        p.Description.ToLower().Contains(term)
                    )
                );
            }

            // For now, return all products, but you might want to add pagination later
            var productList = await products.OrderByDescending(p => p.CreatedAt).ToListAsync();

            return View(productList);
        }

        // GET: CustomerProduct/Search (handle the search form submission)
        public async Task<IActionResult> Search(string searchString)
        {
            // Redirect to Index with search parameter
            return RedirectToAction("Index", new { searchString = searchString });
        }

        // GET: CustomerProduct/Details/5
        // GET: CustomerDashboard/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            // Return JSON data instead of a View
            return Json(new
            {
                productId = product.ProductId,
                productName = product.ProductName,
                description = product.Description,
                price = product.Price,
                stockQuantity = product.StockQuantity,
                createdAt = product.CreatedAt,
                imagePath = product.ImagePath
            });
        }

        // GET: CustomerProduct/Create
        public IActionResult Create()
        {
            // Set page indication (breadcrumbs) with parent
            SetPageIndication("Create New Product", "Products");
            ViewData["CurrentPage"] = "Create Product";

            return View();
        }

        // POST: CustomerProduct/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,ProductName,Description,Price,StockQuantity,CreatedAt,ImagePath,Category,Tags")] Product product)
        {
            // Set page indication (breadcrumbs) with parent
            SetPageIndication("Create New Product", "Products");
            ViewData["CurrentPage"] = "Create Product";

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: CustomerProduct/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Set page indication (breadcrumbs) with parent
            SetPageIndication($"Edit {product.ProductName}", "Products");
            ViewData["CurrentPage"] = "Edit Product";

            return View(product);
        }

        // POST: CustomerProduct/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,ProductName,Description,Price,StockQuantity,CreatedAt,ImagePath,Category,Tags")] Product product)
        {
            // Set page indication (breadcrumbs) with parent
            SetPageIndication($"Edit {product.ProductName}", "Products");
            ViewData["CurrentPage"] = "Edit Product";

            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: CustomerProduct/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            // Set page indication (breadcrumbs) with parent
            SetPageIndication($"Delete {product.ProductName}", "Products");
            ViewData["CurrentPage"] = "Delete Product";

            return View(product);
        }

        // POST: CustomerProduct/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }

    // Breadcrumb class for page indication
    public class Breadcrumb
    {
        public string Title { get; set; }
        public string Url { get; set; }
    }
}