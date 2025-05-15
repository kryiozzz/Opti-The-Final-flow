using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opti.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using Opti.Data;
using BCrypt.Net;
using Opti.ViewModel; // Ensure you have this namespace for RegisterViewModel
using System.Net.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Opti.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Username and password are required.");
                return View();
            }

            // Find the user in the database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user != null)
            {
                try
                {
                    // Try to verify the password using BCrypt
                    if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                    {
                        // Create claims for authentication
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.Email, user.Email),
                            new Claim(ClaimTypes.Role, user.Role),
                            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())  // Important: Add this claim for user ID
                        };

                        // Create identity
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = rememberMe, // Remember me option
                            ExpiresUtc = DateTime.UtcNow.AddMinutes(30) // Session expiry time
                        };

                        // Sign in the user
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity), authProperties);

                        // Redirect based on user role
                        if (user.Role == "Admin")
                        {
                            return RedirectToAction("Index", "AdminDashboard");
                        }
                        else if (user.Role == "Worker")
                        {
                            return RedirectToAction("Index", "WorkerDashboard");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home"); // Default redirection
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        return View();
                    }
                }
                catch (SaltParseException ex)
                {
                    // Handle the SaltParseException and rehash the password
                    _logger.LogWarning(ex, "SaltParseException occurred for user {Username}. Rehashing password.", username);

                    // Rehash the password and save it in the database
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();

                    // Return a message or log the user out and prompt them to login again
                    ModelState.AddModelError(string.Empty, "Your password was rehashed and updated. Please login again.");
                    return View();
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View();
            }
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Username or Email is already taken.");
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = "Customer", // Default role is Worker, can be changed to Admin when needed
                    CreatedAt = DateTime.UtcNow
                };

                try
                {
                    _context.Users.Add(user);
                    int result = await _context.SaveChangesAsync();

                    if (result > 0)
                    {
                        TempData["SuccessMessage"] = "Registration successful! Please log in.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "No changes were saved to the database.");
                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error registering user {Username}: {Message}", model.Username, ex.Message);
                    ModelState.AddModelError(string.Empty, $"An error occurred while saving: {ex.Message}");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(); // If using standard authentication middleware

            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }


            HttpContext.Session.Clear();

            _logger?.LogInformation("User logged out");

            // Redirect to home page or login page
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    userIdClaim = User.FindFirstValue("UserId");
                    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out userId))
                    {
                        // If we still can't get the ID, try by username
                        var username = User.Identity.Name;
                        var userByName = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                        if (userByName != null)
                        {
                            userId = userByName.UserId;
                        }
                        else
                        {
                            return NotFound("User not found");
                        }
                    }
                }

                // Get user from database with all necessary data
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Get recent orders for this user
                var recentOrders = await _context.CustomerOrders
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();

                // Get total orders count
                var totalOrders = await _context.CustomerOrders
                    .Where(o => o.UserId == userId)
                    .CountAsync();

                // Get current cart items count
                var cartItems = await _context.CustomerOrders
                    .Where(o => o.UserId == userId)
                    .SumAsync(o => o.Quantity);

                // Get total amount spent
                var totalSpent = await _context.CustomerOrders
                    .Where(o => o.UserId == userId)
                    .SumAsync(o => o.TotalAmount);

                // Add these to ViewBag for use in the view
                ViewBag.RecentOrders = recentOrders;
                ViewBag.TotalOrders = totalOrders;
                ViewBag.CartItems = cartItems;
                ViewBag.TotalSpent = totalSpent;

                // Pass the user object directly to the view
                return View(user);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading profile");
                TempData["ErrorMessage"] = "An error occurred while loading your profile";
                return RedirectToAction("Index", "Home");
            }
        }

        // Helper method to hash password
        private string HashPassword(string password)
        {
            // In a real application, use a proper password hashing library
            // This is just a simple example using BCrypt.Net
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Helper method to verify password
        private bool VerifyPassword(string password, string passwordHash)
        {
            // In a real application, use a proper password verification
            // This is just a simple example using BCrypt.Net
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }

}

