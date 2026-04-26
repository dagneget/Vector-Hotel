using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRS.API.Data;
using HRS.API.Models;

namespace HRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PaymentsController(AppDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentModel>>> GetPayments() => await _context.Payments.ToListAsync();

        [HttpPost]
        public async Task<ActionResult<PaymentModel>> PostPayment(PaymentModel payment)
        {
            if (string.IsNullOrEmpty(payment.Id)) payment.Id = Guid.NewGuid().ToString();
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var res = await _context.Reservations.FindAsync(payment.ReservationId);
            if (res != null)
            {
                var room = await _context.Rooms.FindAsync(res.RoomId);
                if (room != null && res.RoomStatus == "CheckedIn")
                {
                    bool isPaymentVerified = res.PaymentStatus == "Confirmed" || 
                                           await _context.Payments.AnyAsync(p => p.ReservationId == res.Id && p.VerifiedByUserId != null);
                    room.Status = isPaymentVerified ? "Occupied" : "Reserved";
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(payment);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutPayment(string id, PaymentModel payment)
        {
            if (id != payment.Id) return BadRequest();
            _context.Entry(payment).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            var res = await _context.Reservations.FindAsync(payment.ReservationId);
            if (res != null)
            {
                var room = await _context.Rooms.FindAsync(res.RoomId);
                if (room != null && res.RoomStatus == "CheckedIn")
                {
                    bool isPaymentVerified = res.PaymentStatus == "Confirmed" || 
                                           await _context.Payments.AnyAsync(p => p.ReservationId == res.Id && p.VerifiedByUserId != null);
                    room.Status = isPaymentVerified ? "Occupied" : "Reserved";
                    await _context.SaveChangesAsync();
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(string id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) return NotFound();
            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
