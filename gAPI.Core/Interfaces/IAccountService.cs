using gAPI.Core.Attributes;
using gAPI.Core.Dtos;
using System.ComponentModel.DataAnnotations;

namespace gAPI.Core.Interfaces;

[GenerateMinimalApi]
public interface IAccountService
{
    [IsPage("/Account/Register", "Register", "Register", "")]
    [IsNotAuthorized]
    Task<BaseResponse> RegisterAsync(
        [Title("UserName")]
        [Required(ErrorMessage = "User name is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "User name must be between 3 and 50 characters")]
        string userName,

        [Title("E-mail")]
        [Required(ErrorMessage = "E-mail is required")]
        [EmailAddress(ErrorMessage = "Invalid e-mail address")]
        string email,

        [Title("Password"), IsPassword]
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 8 characters")]
        string password,

        [Title("Repeat password"), IsPassword]
        [Required(ErrorMessage = "Please repeat the password")]
        [IsCompare("Password", ErrorMessage = "Passwords do not match")]
        string passwordRepeat,

        CancellationToken ct);

    [IsLoginPage]
    [IsNotAuthorized]
    [IsPage("/Account/Login", "Login", "Log in", "")]
    Task<BaseResponse> LoginAsync(
        [Title("E-mail")] 
        string email,
        
        [Title("Password")]
        [Required(ErrorMessage = "Password is required")]
        [IsPassword]
        string password, 

        CancellationToken ct);

    [IsLogoffPage]
    [IsAuthorized]
    [IsPage("/Account/Logoff", "Logoff", "Log off", "")]
    Task<BaseResponse> LogoffAsync(CancellationToken ct);
}