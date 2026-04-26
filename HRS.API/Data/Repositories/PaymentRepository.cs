using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using HRS.API.Models;

namespace HRS.API.Data.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PaymentModel>> GetByReservationIdAsync(string reservationId)
        {
            return await _context.Payments.Where(p => p.ReservationId == reservationId).ToListAsync();
        }

        public async Task AddAsync(PaymentModel payment)
        {
            await _context.Payments.AddAsync(payment);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
