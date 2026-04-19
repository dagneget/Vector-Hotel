using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRS.API.Data;
using HRS.API.Models;

namespace HRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChargesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ChargesController(AppDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChargeModel>>> GetCharges() => await _context.Charges.ToListAsync();

        [HttpPost]
        public async Task<ActionResult<ChargeModel>> PostCharge(ChargeModel charge)
        {
            if (string.IsNullOrEmpty(charge.Id)) charge.Id = Guid.NewGuid().ToString();
            _context.Charges.Add(charge);
            await _context.SaveChangesAsync();
            return Ok(charge);
        }
    }
}
