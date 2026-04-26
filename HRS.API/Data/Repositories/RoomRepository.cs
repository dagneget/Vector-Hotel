using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HRS.API.Models;

namespace HRS.API.Data.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly AppDbContext _context;

        public RoomRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RoomModel> GetByIdAsync(string id)
        {
            return await _context.Rooms.FindAsync(id);
        }

        public async Task UpdateAsync(RoomModel room)
        {
            _context.Entry(room).State = EntityState.Modified;
            await Task.CompletedTask;
        }
    }
}
