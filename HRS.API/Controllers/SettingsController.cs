using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRS.API.Data;
using HRS.API.Models;
using Newtonsoft.Json;
using System.IO;

namespace HRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public SettingsController(AppDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<SystemSettingsModel>> GetSettings()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SystemSettingsModel();
                _context.SystemSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return Ok(settings);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] SystemSettingsModel settings)
        {
            var existing = await _context.SystemSettings.FirstOrDefaultAsync();
            if (existing == null)
            {
                _context.SystemSettings.Add(settings);
            }
            else
            {
                // Update all properties including JSON strings
                existing.HotelName = settings.HotelName ?? existing.HotelName;
                existing.HotelAddress = settings.HotelAddress ?? existing.HotelAddress;
                existing.HotelPhone = settings.HotelPhone ?? existing.HotelPhone;
                existing.HotelEmail = settings.HotelEmail ?? existing.HotelEmail;
                existing.LogoData = settings.LogoData ?? existing.LogoData;
                existing.DefaultCurrency = settings.DefaultCurrency ?? existing.DefaultCurrency;
                existing.TaxRate = settings.TaxRate;
                existing.AllowPriceOverride = settings.AllowPriceOverride;
                existing.RequireFullPaymentBeforeCheckIn = settings.RequireFullPaymentBeforeCheckIn;
                existing.AllowPartialPayments = settings.AllowPartialPayments;
                existing.DefaultReservationStatus = settings.DefaultReservationStatus ?? existing.DefaultReservationStatus;
                existing.AllowReservationCancellation = settings.AllowReservationCancellation;
                existing.DateFormat = settings.DateFormat ?? existing.DateFormat;
                existing.TimeFormat = settings.TimeFormat ?? existing.TimeFormat;
                existing.Theme = settings.Theme ?? existing.Theme;
                
                // Update JSON list properties if provided
                if (!string.IsNullOrEmpty(settings.CurrenciesJson))
                    existing.CurrenciesJson = settings.CurrenciesJson;
                if (!string.IsNullOrEmpty(settings.BedTypesJson))
                    existing.BedTypesJson = settings.BedTypesJson;
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> CreateSettings([FromBody] SystemSettingsModel settings)
        {
            _context.SystemSettings.Add(settings);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("backup")]
        public async Task<IActionResult> Backup()
        {
            var data = new
            {
                Users = await _context.Users.ToListAsync(),
                Customers = await _context.Customers.ToListAsync(),
                RoomTypes = await _context.RoomTypes.ToListAsync(),
                Rooms = await _context.Rooms.ToListAsync(),
                Reservations = await _context.Reservations.ToListAsync(),
                Payments = await _context.Payments.ToListAsync(),
                Charges = await _context.Charges.ToListAsync(),
                AuditLogs = await _context.AuditLogs.ToListAsync(),
                Settings = await _context.SystemSettings.ToListAsync()
            };

            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"HRS_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        }

        [HttpPost("restore")]
        public async Task<IActionResult> Restore([FromBody] dynamic backupData)
        {
            try
            {
                // Simple implementation: Clear everything and reload
                // WARNING: Dangerous operation!
                
                _context.Users.RemoveRange(_context.Users);
                _context.Customers.RemoveRange(_context.Customers);
                _context.RoomTypes.RemoveRange(_context.RoomTypes);
                _context.Rooms.RemoveRange(_context.Rooms);
                _context.Reservations.RemoveRange(_context.Reservations);
                _context.Payments.RemoveRange(_context.Payments);
                _context.Charges.RemoveRange(_context.Charges);
                _context.AuditLogs.RemoveRange(_context.AuditLogs);
                _context.SystemSettings.RemoveRange(_context.SystemSettings);
                await _context.SaveChangesAsync();

                var data = JsonConvert.DeserializeObject<dynamic>(backupData.ToString());

                if (data.Users != null) await _context.Users.AddRangeAsync(JsonConvert.DeserializeObject<List<UserModel>>(data.Users.ToString()));
                if (data.Customers != null) await _context.Customers.AddRangeAsync(JsonConvert.DeserializeObject<List<CustomerModel>>(data.Customers.ToString()));
                if (data.RoomTypes != null) await _context.RoomTypes.AddRangeAsync(JsonConvert.DeserializeObject<List<RoomTypeModel>>(data.RoomTypes.ToString()));
                if (data.Rooms != null) await _context.Rooms.AddRangeAsync(JsonConvert.DeserializeObject<List<RoomModel>>(data.Rooms.ToString()));
                if (data.Reservations != null) await _context.Reservations.AddRangeAsync(JsonConvert.DeserializeObject<List<ReservationModel>>(data.Reservations.ToString()));
                if (data.Payments != null) await _context.Payments.AddRangeAsync(JsonConvert.DeserializeObject<List<PaymentModel>>(data.Payments.ToString()));
                if (data.Charges != null) await _context.Charges.AddRangeAsync(JsonConvert.DeserializeObject<List<ChargeModel>>(data.Charges.ToString()));
                if (data.AuditLogs != null) await _context.AuditLogs.AddRangeAsync(JsonConvert.DeserializeObject<List<AuditLogModel>>(data.AuditLogs.ToString()));
                if (data.Settings != null) await _context.SystemSettings.AddRangeAsync(JsonConvert.DeserializeObject<List<SystemSettingsModel>>(data.Settings.ToString()));

                await _context.SaveChangesAsync();
                return Ok(new { Message = "System restored successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Restore failed: {ex.Message}" });
            }
        }
    }
}
