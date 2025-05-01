using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opti.Models; // Change this to your actual namespace where User model is located
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using Opti.Data;
using BCrypt.Net; // Add this line at the top of your file
using Microsoft.CodeAnalysis.Scripting;
namespace Opti.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
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
            if (ModelState.IsValid)
            {
                // Find the user in the database
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                // Check if user exists and the password is correct
                if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    // Create claims
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role) // Assuming roles are stored in your User table
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
                    // If credentials are incorrect, show an error
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View();
                }
            }
            return View();
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model, string confirmPassword)
        {
            if (ModelState.IsValid)
            {
                // Check if passwords match
                if (model.PasswordHash != confirmPassword)
                {
                    ModelState.AddModelError(string.Empty, "The password and confirmation password do not match.");
                    return View(model);
                }

                // Check if username or email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Username or Email is already taken.");
                    return View(model);
                }

                // Hash the password before saving it
                model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);

                // Add user to the database
                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                // Redirect to login page after successful registration
                return RedirectToAction("Login");
            }

            return View(model);
        }
    }
}