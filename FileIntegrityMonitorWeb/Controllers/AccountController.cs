using FileIntegrityMonitor.Models;
using FileIntegrityMonitor.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FileIntegrityMonitor.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Create the user object
                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName
                    };

                    // Try to create the user
                    var result = await _userManager.CreateAsync(user, model.Password);

                    // Log the result for debugging
                    Console.WriteLine($"User creation attempt for {model.Email}: {result.Succeeded}");

                    if (result.Succeeded)
                    {
                        // Verify the user was saved to database
                        var createdUser = await _userManager.FindByEmailAsync(model.Email);
                        if (createdUser != null)
                        {
                            Console.WriteLine($"User {model.Email} was successfully saved with ID: {createdUser.Id}");

                            // Save changes to ensure everything is persisted
                            await _context.SaveChangesAsync();

                            // Sign in the user
                            await _signInManager.SignInAsync(user, isPersistent: true);

                            // Redirect to home page
                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            Console.WriteLine($"ERROR: User was created but cannot be retrieved from database");
                            ModelState.AddModelError(string.Empty, "User was created but cannot be found in the database.");
                        }
                    }
                    else
                    {
                        // Log the errors
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"User creation error: {error.Description}");
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log any exceptions
                    Console.WriteLine($"Exception during user registration: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Clear any existing authentication
            await _signInManager.SignOutAsync();

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if user exists first
                    var user = await _userManager.FindByEmailAsync(model.Email);

                    if (user == null)
                    {
                        Console.WriteLine($"Login failure: No user found with email {model.Email}");
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        return View(model);
                    }

                    Console.WriteLine($"Found user with email {model.Email}, ID: {user.Id}");

                    // Try to sign in
                    var result = await _signInManager.PasswordSignInAsync(
                        model.Email,
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: false);

                    Console.WriteLine($"Sign-in result for {model.Email}: {result.Succeeded}");

                    if (result.Succeeded)
                    {
                        // Additional verification that we're signed in
                        var isSignedIn = _signInManager.IsSignedIn(User);
                        Console.WriteLine($"After login, IsSignedIn: {isSignedIn}");

                        return RedirectToLocal(returnUrl);
                    }
                    else
                    {
                        // Check specific failure reasons
                        if (result.IsLockedOut)
                        {
                            Console.WriteLine($"User {model.Email} is locked out");
                            ModelState.AddModelError(string.Empty, "This account is locked out.");
                        }
                        else if (result.IsNotAllowed)
                        {
                            Console.WriteLine($"User {model.Email} is not allowed to sign in");
                            ModelState.AddModelError(string.Empty, "You are not allowed to log in.");
                        }
                        else
                        {
                            Console.WriteLine($"Login failed for {model.Email} - password likely incorrect");
                            ModelState.AddModelError(string.Empty, "Invalid email or password.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception during login: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public async Task<IActionResult> DatabaseTest()
        {
            // Count users in the database
            int userCount = await _context.Users.CountAsync();

            // Get a list of all users
            var users = await _context.Users.ToListAsync();

            ViewData["UserCount"] = userCount;
            ViewData["Users"] = users;

            return View();
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}