using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRS.API.Data;
using HRS.API.Models;

namespace HRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservationModel>>> GetReservations()
        {
            return await _context.Reservations.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationModel>> GetReservation(string id)
        {
            var res = await _context.Reservations.FindAsync(id);
            if (res == null) return NotFound();
            return res;
        }

        [HttpPost]
        public async Task<ActionResult<ReservationModel>> PostReservation(ReservationModel res)
        {
            if (string.IsNullOrEmpty(res.Id)) res.Id = Guid.NewGuid().ToString();
            res.LastModified = DateTime.Now;
            _context.Reservations.Add(res);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetReservation", new { id = res.Id }, res);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutReservation(string id, ReservationModel res)
        {
            if (id != res.Id) return BadRequest();
            res.LastModified = DateTime.Now;
            _context.Entry(res).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { if (!ReservationExists(id)) return NotFound(); else throw; }
            return NoContent();
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> PutReservationStatus(string id, [FromBody] string newStatus)
        {
            var res = await _context.Reservations.FindAsync(id);
            if (res == null) return NotFound();

            var room = await _context.Rooms.FindAsync(res.RoomId);
            
            // Basic Status Sync Logic
            if (newStatus == "CheckedIn")
            {
                if (room != null) room.Status = "Occupied";
                res.CheckInTime = DateTime.Now;
            }
            else if (newStatus == "CheckedOut")
            {
                if (room != null)
                {
                    room.Status = "Available";
                    room.CleanStatus = "Dirty";
                }
                res.CheckOutTime = DateTime.Now;
            }
            else if (newStatus == "Cancelled")
            {
                if (room != null && room.Status == "Occupied") room.Status = "Available";
            }

            res.Status = newStatus;
            res.LastModified = DateTime.Now;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool ReservationExists(string id) => _context.Reservations.Any(e => e.Id == id);
    }
}
