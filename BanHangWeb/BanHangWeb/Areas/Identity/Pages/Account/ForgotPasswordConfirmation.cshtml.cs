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
    public class ForgotPasswordConfirmationModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly EmailService _emailService;
        private readonly ILogger<ForgotPasswordConfirmationModel> _logger;

        // Nhận email qua query (?email=...)
        [BindProperty(SupportsGet = true)]
        public string? Email { get; set; }

        public ForgotPasswordConfirmationModel(
            UserManager<IdentityUser> userManager,
            EmailService emailService,
            ILogger<ForgotPasswordConfirmationModel> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        public void OnGet()
        {
        }

        // POST: nút "Gửi lại liên kết"
        public async Task<IActionResult> OnPostResendAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                // Không có email -> quay lại trang nhập
                return RedirectToPage("./ForgotPassword");
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                // Không tiết lộ user có tồn tại hay không
                return RedirectToPage("./ForgotPassword");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var codeBytes = Encoding.UTF8.GetBytes(code);
            var codeEncoded = WebEncoders.Base64UrlEncode(codeBytes);

            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code = codeEncoded, email = user.Email },
                protocol: Request.Scheme);

            var htmlBody = $@"
                <p>Xin chào {user.Email},</p>
                <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản tại <strong>ĐỒ THỂ THAO</strong>.</p>
                <p>Nhấn vào link dưới đây để đặt lại mật khẩu:</p>
                <p><a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>
                    Đặt lại mật khẩu
                </a></p>
                <p>Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>";

            var ok = await _emailService.SendEmailAsync(
                recipientEmail: user.Email!,
                subject: "Đặt lại mật khẩu - ĐỒ THỂ THAO",
                bodyHtml: htmlBody);

            if (!ok)
            {
                _logger.LogWarning("Resend reset password email FAILED for {Email}", user.Email);
            }

            TempData["ResendOk"] = true;
            return Page(); // vẫn ở lại trang xác nhận
        }
    }
}
