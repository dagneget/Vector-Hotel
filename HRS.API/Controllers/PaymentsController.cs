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
            return Ok(payment);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutPayment(string id, PaymentModel payment)
        {
            if (id != payment.Id) return BadRequest();
            _context.Entry(payment).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
