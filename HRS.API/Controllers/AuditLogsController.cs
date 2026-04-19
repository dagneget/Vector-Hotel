using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRS.API.Data;
using HRS.API.Models;

namespace HRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AuditLogsController(AppDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLogModel>>> GetLogs() => await _context.AuditLogs.OrderByDescending(l => l.Timestamp).ToListAsync();

        [HttpPost]
        public async Task<ActionResult<AuditLogModel>> PostLog(AuditLogModel log)
        {
            if (string.IsNullOrEmpty(log.Id)) log.Id = Guid.NewGuid().ToString();
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
            return Ok(log);
        }
    }
}
