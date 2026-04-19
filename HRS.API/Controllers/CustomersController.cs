using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRS.API.Data;
using HRS.API.Models;

namespace HRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CustomersController(AppDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerModel>>> GetCustomers() => await _context.Customers.ToListAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerModel>> GetCustomer(string id)
        {
            var item = await _context.Customers.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<CustomerModel>> PostCustomer(CustomerModel item)
        {
            if (string.IsNullOrEmpty(item.Id)) item.Id = Guid.NewGuid().ToString();
            _context.Customers.Add(item);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetCustomer", new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomer(string id, CustomerModel item)
        {
            if (id != item.Id) return BadRequest();
            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
