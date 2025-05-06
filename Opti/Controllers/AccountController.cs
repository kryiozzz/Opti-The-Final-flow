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
                            new Claim(ClaimTypes.Role, user.Role)
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

                        // Redirect to the home page or dashboard
                        return RedirectToAction("Index", "Home");
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
            // Debug information
            _logger.LogInformation("Register method called with model: IsValid={IsValid}", ModelState.IsValid);

            if (ModelState.IsValid)
            {
                // Check if username or email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Username or Email is already taken.");
                    return View(model);
                }

                // Create new user from view model
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = "User", // Default role
                    CreatedAt = DateTime.UtcNow
                };

                // Add the user to the database
                try
                {
                    _logger.LogInformation("Attempting to register new user: {Username}", model.Username);

                    _context.Users.Add(user);
                    int result = await _context.SaveChangesAsync();

                    _logger.LogInformation("Registration result: {Result} rows affected", result);

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
                    // If an error occurs, log and show the error message
                    _logger.LogError(ex, "Error registering user {Username}: {Message}", model.Username, ex.Message);
                    ModelState.AddModelError(string.Empty, $"An error occurred while saving: {ex.Message}");
                    return View(model);
                }
            }
            else
            {
                // Log the validation errors
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogWarning("Validation error: {ErrorMessage}", error.ErrorMessage);
                    }
                }
            }

            return View(model);
        }

        // Log out and redirect to login page
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}