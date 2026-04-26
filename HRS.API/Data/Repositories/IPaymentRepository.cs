using System.Threading.Tasks;
using System.Collections.Generic;
using HRS.API.Models;

namespace HRS.API.Data.Repositories
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<PaymentModel>> GetByReservationIdAsync(string reservationId);
        Task AddAsync(PaymentModel payment);
        Task SaveChangesAsync();
    }
}
