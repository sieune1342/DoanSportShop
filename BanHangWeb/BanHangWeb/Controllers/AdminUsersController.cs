using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;

namespace BanHangWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AdminUsersController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // Toggle khóa/mở
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.Now)
                {
                    // Mở khóa
                    user.LockoutEnd = null;
                }
                else
                {
                    // Khóa 100 năm
                    user.LockoutEnd = DateTimeOffset.Now.AddYears(100);
                }

                await _userManager.UpdateAsync(user);
            }

            return RedirectToAction("Index");
        }

        // Xóa tài khoản
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            return RedirectToAction("Index");
        }
    }
}
