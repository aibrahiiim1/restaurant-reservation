using System.ComponentModel.DataAnnotations;
using RestaurantReservation.Web.Models.Entities;

namespace RestaurantReservation.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required]
    [Display(Name = "Username or Email")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class OtpVerificationViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    [Display(Name = "Verification Code")]
    public string Otp { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class GuestLoginViewModel
{
    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    public string? ReturnUrl { get; set; }
}
