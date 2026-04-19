using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRS.API.Data;
using HRS.API.Models;

namespace HRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomTypesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public RoomTypesController(AppDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomTypeModel>>> GetRoomTypes() => await _context.RoomTypes.ToListAsync();

        [HttpPost]
        public async Task<ActionResult<RoomTypeModel>> PostRoomType(RoomTypeModel item)
        {
            if (string.IsNullOrEmpty(item.Id)) item.Id = Guid.NewGuid().ToString();
            _context.RoomTypes.Add(item);
            await _context.SaveChangesAsync();
            return Ok(item);
        }
    }
}
