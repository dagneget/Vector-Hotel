using System.Threading.Tasks;

namespace HRS.API.Services
{
    public interface IReservationService
    {
        Task CancelReservationAsync(string reservationId);
        Task ProcessEarlyCheckoutAsync(string reservationId);
    }
}
