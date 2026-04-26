using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HRS.API.Models;

namespace HRS.API.Data.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly AppDbContext _context;

        public ReservationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ReservationModel> GetByIdAsync(string id)
        {
            return await _context.Reservations.FindAsync(id);
        }

        public async Task UpdateAsync(ReservationModel reservation)
        {
            _context.Entry(reservation).State = EntityState.Modified;
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
