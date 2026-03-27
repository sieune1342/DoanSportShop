using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using BanHangWeb.Services;

namespace BanHangWeb.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly EmailService _emailService;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<IdentityUser> userManager,
            EmailService emailService,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email.")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);

            // ✅ Không tiết lộ user có tồn tại hay không,
            // nhưng vẫn truyền email sang trang xác nhận để dùng nút “Gửi lại liên kết”
            if (user == null)
            {
                return RedirectToPage("./ForgotPasswordConfirmation", new { email = Input.Email });
            }

            // Tạo token reset password
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var codeBytes = Encoding.UTF8.GetBytes(code);
            var codeEncoded = WebEncoders.Base64UrlEncode(codeBytes);

            // Tạo link reset password
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code = codeEncoded, email = user.Email },
                protocol: Request.Scheme);

            if (string.IsNullOrEmpty(callbackUrl))
            {
                _logger.LogError("Không tạo được callbackUrl khi reset password cho {Email}", user.Email);
                return RedirectToPage("./ForgotPasswordConfirmation", new { email = Input.Email });
            }

            var htmlBody = $@"
                <p>Xin chào {user.Email},</p>
                <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản tại <strong>ĐỒ THỂ THAO</strong>.</p>
                <p>Nhấn vào link dưới đây để đặt lại mật khẩu:</p>
                <p><a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>
                    Đặt lại mật khẩu
                </a></p>
                <p>Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>";

            // Gửi mail (chú ý đúng thứ tự tham số: name, email, subject, body)
            var ok = await _emailService.SendEmailAsync(
                user.Email ?? user.UserName ?? "Khách", user.Email!, "Đặt lại mật khẩu - ĐỒ THỂ THAO", htmlBody);

            if (!ok)
            {
                _logger.LogWarning("Gửi email reset password thất bại cho {Email}", user.Email);
            }

            return RedirectToPage("./ForgotPasswordConfirmation", new { email = Input.Email });
        }
    }
}
