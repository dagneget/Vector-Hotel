using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRS.API.Data;
using HRS.API.Models;

namespace HRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UsersController(AppDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserModel>>> GetUsers() => await _context.Users.ToListAsync();

        [HttpPost("login")]
        public async Task<ActionResult<UserModel>> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null || !HRS.API.Helpers.SecurityHelper.VerifyPassword(request.Password, user.Password))
            {
                return Unauthorized();
            }
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserModel>> PostUser(UserModel user)
        {
            if (string.IsNullOrEmpty(user.Id)) user.Id = Guid.NewGuid().ToString();
            
            // Hash the password if provided
            if (!string.IsNullOrEmpty(user.Password))
            {
                user.Password = HRS.API.Helpers.SecurityHelper.HashPassword(user.Password);
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetUsers", new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(string id, UserModel user)
        {
            if (id != user.Id) return BadRequest();

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null) return NotFound();

            existingUser.Username = user.Username;
            existingUser.Role = user.Role;

            // Only update password if a new one is supplied
            if (!string.IsNullOrEmpty(user.Password) && user.Password != existingUser.Password)
            {
                existingUser.Password = HRS.API.Helpers.SecurityHelper.HashPassword(user.Password);
            }

            _context.Entry(existingUser).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}
