using System.Threading.Tasks;

namespace HRS.API.Services
{
    public interface IPaymentService
    {
        Task ProcessRefundAsync(string reservationId, decimal amount);
    }
}
