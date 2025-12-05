using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestaurantReservation.Web.Models.Entities;
using RestaurantReservation.Web.Models.ViewModels;
using RestaurantReservation.Web.Services.Interfaces;

namespace RestaurantReservation.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOtpService _otpService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOtpService otpService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _otpService = otpService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Try to find user by username or email
        var user = await _userManager.FindByNameAsync(model.Username)
                   ?? await _userManager.FindByEmailAsync(model.Username);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Your account has been deactivated.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            
            _logger.LogInformation("User {UserId} logged in.", user.Id);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            // Redirect based on role
            if (await _userManager.IsInRoleAsync(user, "SuperAdmin") ||
                await _userManager.IsInRoleAsync(user, "RestaurantManager") ||
                await _userManager.IsInRoleAsync(user, "BranchManager"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home");
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToAction(nameof(LoginWith2fa), new { model.ReturnUrl, model.RememberMe });
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {UserId} account locked out.", user.Id);
            return RedirectToAction(nameof(Lockout));
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Username,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            FirstName = model.FirstName,
            LastName = model.LastName,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} created a new account.", user.Id);
            
            await _userManager.AddToRoleAsync(user, "Guest");

            // For email verification, send confirmation email
            // For now, auto-confirm
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            await _signInManager.SignInAsync(user, isPersistent: false);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult GuestLogin(string? returnUrl = null)
    {
        return View(new GuestLoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuestLogin(GuestLoginViewModel model)
    {
        if (string.IsNullOrEmpty(model.Email) && string.IsNullOrEmpty(model.Phone))
        {
            ModelState.AddModelError("", "Please enter email or phone number");
            return View(model);
        }

        ApplicationUser? user = null;

        if (!string.IsNullOrEmpty(model.Email))
        {
            user = await _userManager.FindByEmailAsync(model.Email);
        }
        else if (!string.IsNullOrEmpty(model.Phone))
        {
            user = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == model.Phone);
        }

        if (user == null)
        {
            // Create a new guest user
            user = new ApplicationUser
            {
                UserName = model.Email ?? $"guest_{Guid.NewGuid():N}",
                Email = model.Email,
                PhoneNumber = model.Phone,
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "Guest");
        }

        // Send OTP
        var otpResult = await _otpService.SendOtpAsync(user.Id, model.Email, model.Phone);
        if (!otpResult.Success)
        {
            ModelState.AddModelError("", otpResult.ErrorMessage ?? "Failed to send verification code");
            return View(model);
        }

        return RedirectToAction(nameof(VerifyOtp), new { userId = user.Id, returnUrl = model.ReturnUrl });
    }

    [HttpGet]
    public IActionResult VerifyOtp(string userId, string? returnUrl = null)
    {
        return View(new OtpVerificationViewModel { UserId = userId, ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(OtpVerificationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _otpService.VerifyOtpAsync(model.UserId, model.Otp);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.ErrorMessage ?? "Invalid verification code");
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user != null)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            
            _logger.LogInformation("User {UserId} verified OTP and logged in.", user.Id);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult LoginWith2fa(string? returnUrl = null, bool rememberMe = false)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["RememberMe"] = rememberMe;
        return View();
    }

    [HttpGet]
    public IActionResult Lockout()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string? phoneNumber)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = phoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = "Profile updated successfully";
        }
        else
        {
            TempData["Error"] = "Failed to update profile";
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            ModelState.AddModelError("", "Email is required");
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        // Generate password reset token and send email
        // For now, just redirect to confirmation
        _logger.LogInformation("Password reset requested for {Email}", email);

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string? code = null)
    {
        if (code == null)
        {
            return BadRequest("A code must be supplied for password reset.");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string email, string password, string confirmPassword, string code)
    {
        if (password != confirmPassword)
        {
            ModelState.AddModelError("", "Passwords do not match");
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        var result = await _userManager.ResetPasswordAsync(user, code, password);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View();
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }
}
