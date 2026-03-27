using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace BanHangWeb.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminUsersApiController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        public AdminUsersApiController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: api/AdminUsers
        [HttpGet]
        public IActionResult GetUsers()
        {
            var users = _userManager.Users.ToList();
            return Ok(users);
        }

        // POST: api/AdminUsers/togglelock
        [HttpPost("togglelock")]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.Now)
                {
                    user.LockoutEnd = null;
                }
                else
                {
                    user.LockoutEnd = DateTimeOffset.Now.AddYears(100);
                }
                await _userManager.UpdateAsync(user);
                return Ok(user);
            }
            return NotFound();
        }

        // DELETE: api/AdminUsers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                return Ok();
            }
            return NotFound();
        }
    }
} 