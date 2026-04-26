using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRS.API.Data;
using HRS.API.Models;
using HRS.API.Services;

namespace HRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IReservationService _reservationService;

        public ReservationsController(AppDbContext context, IReservationService reservationService)
        {
            _context = context;
            _reservationService = reservationService;
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
            await SyncRoomStatusAsync(res);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("GetReservation", new { id = res.Id }, res);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutReservation(string id, ReservationModel res)
        {
            if (id != res.Id) return BadRequest();
            res.LastModified = DateTime.Now;
            _context.Entry(res).State = EntityState.Modified;
            
            try 
            { 
                await _context.SaveChangesAsync(); 
                await SyncRoomStatusAsync(res);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) { if (!ReservationExists(id)) return NotFound(); else throw; }
            return NoContent();
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> PutReservationStatus(string id, [FromBody] string newStatus)
        {
            var res = await _context.Reservations.FindAsync(id);
            if (res == null) return NotFound();

            if (newStatus == "Pending" || newStatus == "Confirmed")
            {
                res.PaymentStatus = newStatus;
            }
            else if (newStatus == "CheckedIn" || newStatus == "CheckedOut" || newStatus == "Cancelled")
            {
                res.RoomStatus = newStatus;
                
                if (newStatus == "CheckedIn")
                {
                    res.CheckInTime = DateTime.Now;
                }
                else if (newStatus == "CheckedOut")
                {
                    res.CheckOutTime = DateTime.Now;
                }
            }
            res.LastModified = DateTime.Now;

            await _context.SaveChangesAsync();
            await SyncRoomStatusAsync(res);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelReservation(string id)
        {
            try
            {
                await _reservationService.CancelReservationAsync(id);
                return Ok(new { message = "Reservation cancelled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/early-checkout")]
        public async Task<IActionResult> ProcessEarlyCheckout(string id)
        {
            try
            {
                await _reservationService.ProcessEarlyCheckoutAsync(id);
                return Ok(new { message = "Early checkout processed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task SyncRoomStatusAsync(ReservationModel res)
        {
            var room = await _context.Rooms.FindAsync(res.RoomId);
            if (room != null)
            {
                if (res.RoomStatus == "CheckedIn")
                {
                    bool isPaymentVerified = res.PaymentStatus == "Confirmed" || 
                                           await _context.Payments.AnyAsync(p => p.ReservationId == res.Id && p.VerifiedByUserId != null);
                    
                    if (isPaymentVerified)
                    {
                        room.Status = "Occupied";
                    }
                    else
                    {
                        room.Status = "Reserved";
                    }
                }
                else if (res.RoomStatus == "CheckedOut" || res.RoomStatus == "Cancelled")
                {
                    room.Status = "Available";
                    if (res.RoomStatus == "CheckedOut")
                    {
                        room.CleanStatus = "Dirty";
                    }
                }
            }
        }

        private bool ReservationExists(string id) => _context.Reservations.Any(e => e.Id == id);
    }
}
