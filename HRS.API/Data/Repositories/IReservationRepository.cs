using System.Threading.Tasks;
using System.Collections.Generic;
using HRS.API.Models;

namespace HRS.API.Data.Repositories
{
    public interface IReservationRepository
    {
        Task<ReservationModel> GetByIdAsync(string id);
        Task UpdateAsync(ReservationModel reservation);
        Task SaveChangesAsync();
    }
}
