using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opti.Data;
using Opti.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace Opti.Controllers
{
    public class CustomerOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerOrdersController> _logger;

        public CustomerOrdersController(ApplicationDbContext context, ILogger<CustomerOrdersController> logger = null)
        {
            _context = context;
            _logger = logger;
        }

        // Helper method to get current user ID - centralized to avoid code duplication
        private async Task<(bool success, int userId)> GetCurrentUserId()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return (false, 0);
                }

                // Try to get user ID from claims
                string userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Try alternative claim types if NameIdentifier is null
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    userIdClaim = User.FindFirstValue("sub") ??
                                  User.FindFirstValue("userId") ??
                                  User.FindFirstValue("id") ??
                                  User.Identity.Name;
                }

                // If we have a numeric ID, use it
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedUserId))
                {
                    _logger?.LogInformation("User ID found in claims: {UserId}", parsedUserId);
                    return (true, parsedUserId);
                }

                // If we have a username but not an ID, look up the user
                if (!string.IsNullOrEmpty(User.Identity.Name))
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u =>
                        u.Username == User.Identity.Name ||
                        u.Username == userIdClaim); // Try both options

                    if (user != null)
                    {
                        _logger?.LogInformation("User ID found by username lookup: {UserId}", user.UserId);
                        return (true, user.UserId);
                    }
                }

                _logger?.LogWarning("Failed to identify user. Username: {Username}, Claims: {Claims}",
                    User.Identity.Name,
                    string.Join(", ", User.Claims.Select(c => $"{c.Type}:{c.Value}")));
                return (false, 0);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user ID");
                return (false, 0);
            }
        }

        // GET: CustomerOrders
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var (success, userId) = await GetCurrentUserId();

            if (!success)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "CustomerOrders") });
            }

            // Get only THIS user's orders
            var applicationDbContext = _context.CustomerOrders
                .Where(c => c.UserId == userId) // Filter by current user ID
                .Include(c => c.Product)
                .Include(c => c.User);

            return View(await applicationDbContext.ToListAsync());
        }

        // POST: CustomerOrders/UpdateQuantity/5?change=1
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateQuantity(int id, int change)
        {
            var (success, userId) = await GetCurrentUserId();

            if (!success)
            {
                return Json(new { success = false, message = "Please log in to update your cart", requireLogin = true });
            }

            try
            {
                // Find the order
                var order = await _context.CustomerOrders
                    .Include(o => o.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                // Verify this order belongs to current user
                if (order.UserId != userId)
                {
                    _logger?.LogWarning("User {UserId} attempted to update order {OrderId} belonging to user {OrderUserId}",
                        userId, id, order.UserId);
                    return Json(new { success = false, message = "You don't have permission to modify this order" });
                }

                // Calculate new quantity
                int newQuantity = order.Quantity + change;

                // Don't allow negative quantities
                if (newQuantity <= 0)
                {
                    return Json(new { success = false, message = "Quantity must be at least 1. Use remove button to delete." });
                }

                // Get the product to check stock
                var product = order.Product;
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Check if we're increasing or decreasing quantity
                if (change > 0)
                {
                    // Check if there's enough stock available
                    if (product.StockQuantity < change)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Sorry, only {product.StockQuantity} additional items available in stock"
                        });
                    }

                    // Reduce stock quantity
                    product.StockQuantity -= change;
                }
                else // change < 0, we're decreasing the quantity
                {
                    // Return items to stock
                    product.StockQuantity += Math.Abs(change);
                }

                // Update order quantity and total
                order.Quantity = newQuantity;
                order.TotalAmount = newQuantity * product.Price;

                // Save changes
                _context.Update(product);
                _context.Update(order);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Quantity updated successfully",
                    productName = product.ProductName,
                    newQuantity = order.Quantity,
                    newTotal = order.TotalAmount,
                    stockRemaining = product.StockQuantity
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating order quantity. User: {UserId}, Order: {OrderId}", userId, id);
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // POST: CustomerOrders/RemoveFromCart/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var (success, userId) = await GetCurrentUserId();

            if (!success)
            {
                return Json(new { success = false, message = "Please log in to remove items from your cart", requireLogin = true });
            }

            try
            {
                // Find the order
                var order = await _context.CustomerOrders
                    .Include(o => o.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                // Verify this order belongs to current user
                if (order.UserId != userId)
                {
                    _logger?.LogWarning("User {UserId} attempted to remove order {OrderId} belonging to user {OrderUserId}",
                        userId, id, order.UserId);
                    return Json(new { success = false, message = "You don't have permission to remove this order" });
                }

                // Get the product to update stock
                var product = order.Product;
                if (product != null)
                {
                    // Return the quantity back to the product stock
                    product.StockQuantity += order.Quantity;
                    _context.Update(product);
                }

                // Remove the order
                _context.CustomerOrders.Remove(order);
                await _context.SaveChangesAsync();

                // Get updated order count
                var orderCount = await _context.CustomerOrders
                    .Where(o => o.UserId == userId)
                    .CountAsync();

                return Json(new
                {
                    success = true,
                    message = "Item removed from cart",
                    orderCount = orderCount
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing order from cart. User: {UserId}, Order: {OrderId}", userId, id);
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // GET: CustomerOrders/Cart
        [Authorize]
        public async Task<IActionResult> Cart()
        {
            var (success, userId) = await GetCurrentUserId();

            if (!success)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Cart", "CustomerOrders") });
            }

            // Get only THIS user's orders
            var userOrders = await _context.CustomerOrders
                .Where(c => c.UserId == userId) // Filter by current user ID
                .Include(c => c.Product)
                .Include(c => c.User)
                .ToListAsync();

            return View(userOrders);
        }

        private bool CustomerOrderExists(int id)
        {
            return _context.CustomerOrders.Any(e => e.OrderId == id);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var (success, userId) = await GetCurrentUserId();

            if (!success)
            {
                return Json(new { success = false, message = "Please log in to add items to your cart", requireLogin = true });
            }

            try
            {
                // Find the product
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Check if product is in stock
                if (product.StockQuantity < quantity)
                {
                    return Json(new { success = false, message = $"Only {product.StockQuantity} items available in stock" });
                }

                // Check if order already exists for this product and user
                var existingOrder = await _context.CustomerOrders
                    .FirstOrDefaultAsync(o => o.ProductId == productId && o.UserId == userId);

                if (existingOrder != null)
                {
                    // Update existing order
                    existingOrder.Quantity += quantity;
                    existingOrder.TotalAmount = existingOrder.Quantity * product.Price;
                    _context.Update(existingOrder);
                }
                else
                {
                    // Create new order
                    var customerOrder = new CustomerOrder
                    {
                        ProductId = productId,
                        UserId = userId,
                        Quantity = quantity,
                        TotalAmount = product.Price * quantity,
                        OrderDate = DateTime.Now
                    };
                    _context.Add(customerOrder);
                }

                // Reduce stock quantity
                product.StockQuantity -= quantity;
                _context.Update(product);
                await _context.SaveChangesAsync();

                // Get total orders count for user
                var orderCount = await _context.CustomerOrders
                    .Where(o => o.UserId == userId)
                    .CountAsync();

                return Json(new
                {
                    success = true,
                    message = $"{product.ProductName} added to your cart",
                    orderCount = orderCount,
                    productName = product.ProductName,
                    price = product.Price
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding item to cart. User: {UserId}, Product: {ProductId}", userId, productId);
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // Get order count for the current user
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetOrderCount()
        {
            var (success, userId) = await GetCurrentUserId();

            if (!success)
            {
                return Json(new { count = 0 });
            }

            try
            {
                // Get total orders count for user
                var orderCount = await _context.CustomerOrders
                    .Where(o => o.UserId == userId)
                    .CountAsync();

                return Json(new { count = orderCount });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting order count for user: {UserId}", userId);
                return Json(new { count = 0 });
            }
        }

        // Modified: ClearCart now becomes a manual action rather than automatic logout process
        // This method should only be called when a user explicitly wants to clear their cart
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ClearCart()
        {
            var (success, userId) = await GetCurrentUserId();

            if (!success)
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            try
            {
                // Get all orders for this user
                var userOrders = await _context.CustomerOrders
                    .Where(o => o.UserId == userId)
                    .ToListAsync();

                // If there are any orders, restore product quantities
                if (userOrders.Any())
                {
                    foreach (var order in userOrders)
                    {
                        var product = await _context.Products.FindAsync(order.ProductId);
                        if (product != null)
                        {
                            // Return the quantity back to the product stock
                            product.StockQuantity += order.Quantity;
                            _context.Update(product);
                        }

                        // Remove the order
                        _context.CustomerOrders.Remove(order);
                    }

                    // Save changes to database
                    await _context.SaveChangesAsync();
                    _logger?.LogInformation("Cart cleared for user: {UserId}, {Count} items removed",
                        userId, userOrders.Count);
                }

                return Json(new { success = true, message = "Cart cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error clearing cart for user: {UserId}", userId);
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // Add a new method to gracefully handle cart persistence on logout
        // This prevents automatic cart clearing on logout
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PrepareForLogout()
        {
            // Do nothing - cart will be preserved in the database for the next login
            return Json(new { success = true, message = "Cart preserved for next login" });
        }
    }
}