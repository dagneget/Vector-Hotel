using System.Threading.Tasks;
using HRS.API.Models;

namespace HRS.API.Data.Repositories
{
    public interface IRoomRepository
    {
        Task<RoomModel> GetByIdAsync(string id);
        Task UpdateAsync(RoomModel room);
    }
}
