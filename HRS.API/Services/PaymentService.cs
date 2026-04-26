using System;
using System.Threading.Tasks;
using HRS.API.Data.Repositories;
using HRS.API.Models;

namespace HRS.API.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;

        public PaymentService(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task ProcessRefundAsync(string reservationId, decimal amount)
        {
            if (amount <= 0) return;

            var refundPayment = new PaymentModel
            {
                Id = Guid.NewGuid().ToString(),
                ReservationId = reservationId,
                Amount = -amount, // Negative indicates refund
                Date = DateTime.Now,
                Method = "System Refund",
                Status = "Refunded",
                RecordedByUserId = "System"
            };

            await _paymentRepository.AddAsync(refundPayment);
            await _paymentRepository.SaveChangesAsync();
        }
    }
}
