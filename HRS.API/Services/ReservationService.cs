using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HRS.API.Data;
using HRS.API.Data.Repositories;
using HRS.API.Models;

namespace HRS.API.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IRoomRepository _roomRepo;
        private readonly IPaymentService _paymentService;
        private readonly AppDbContext _context;

        public ReservationService(
            IReservationRepository reservationRepo,
            IPaymentRepository paymentRepo,
            IRoomRepository roomRepo,
            IPaymentService paymentService,
            AppDbContext context)
        {
            _reservationRepo = reservationRepo;
            _paymentRepo = paymentRepo;
            _roomRepo = roomRepo;
            _paymentService = paymentService;
            _context = context;
        }

        public async Task CancelReservationAsync(string reservationId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var res = await _reservationRepo.GetByIdAsync(reservationId);
                if (res == null) throw new Exception("Reservation not found.");

                if (res.RoomStatus == "Cancelled" || res.RoomStatus == "CheckedOut")
                    throw new Exception("Cannot cancel a completed or already cancelled reservation.");

                double hoursUntilCheckIn = (res.CheckIn.Date - DateTime.Today).TotalHours;
                decimal penalty = 0;

                var room = await _roomRepo.GetByIdAsync(res.RoomId);
                decimal pricePerNight = room != null ? room.BasePricePerNight : (res.TotalPrice / Math.Max(1, (int)(res.CheckOut.Date - res.CheckIn.Date).TotalDays));
                if (room != null && res.PricingPlan == "Weekend") pricePerNight = room.WeekendPrice;
                else if (room != null && res.PricingPlan == "Holiday") pricePerNight = room.HolidayPrice;

                if (hoursUntilCheckIn < 48)
                {
                    penalty = pricePerNight;
                }

                var payments = await _paymentRepo.GetByReservationIdAsync(reservationId);
                decimal totalPaid = payments.Sum(p => p.Amount);

                decimal refundAmount = totalPaid - penalty;
                if (refundAmount < 0) refundAmount = 0;

                if (refundAmount > 0)
                {
                    await _paymentService.ProcessRefundAsync(reservationId, refundAmount);
                    res.PaymentStatus = "Refunded";
                }

                res.RoomStatus = "Cancelled";
                res.TotalPrice = penalty;
                res.LastModified = DateTime.Now;

                if (room != null)
                {
                    room.Status = "Available";
                    await _roomRepo.UpdateAsync(room);
                }

                await _reservationRepo.UpdateAsync(res);
                await _reservationRepo.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ProcessEarlyCheckoutAsync(string reservationId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var res = await _reservationRepo.GetByIdAsync(reservationId);
                if (res == null) throw new Exception("Reservation not found.");

                if (res.RoomStatus != "CheckedIn")
                    throw new Exception("Reservation must be CheckedIn to process early checkout.");

                var room = await _roomRepo.GetByIdAsync(res.RoomId);
                decimal pricePerNight = room != null ? room.BasePricePerNight : (res.TotalPrice / Math.Max(1, (int)(res.CheckOut.Date - res.CheckIn.Date).TotalDays));
                if (room != null && res.PricingPlan == "Weekend") pricePerNight = room.WeekendPrice;
                else if (room != null && res.PricingPlan == "Holiday") pricePerNight = room.HolidayPrice;

                int daysStayed = (int)(DateTime.Today - res.CheckIn.Date).TotalDays;
                if (daysStayed < 1) daysStayed = 1;

                int originalDays = (int)(res.CheckOut.Date - res.CheckIn.Date).TotalDays;
                decimal actualCost = 0;
                int finalChargedDays = daysStayed;

                if (originalDays <= 1 || daysStayed < 1 || (DateTime.Today == res.CheckIn.Date))
                {
                    actualCost = res.TotalPrice * 0.30m;
                    finalChargedDays = 1;
                }
                else
                {
                    finalChargedDays = daysStayed + 1;
                    if (finalChargedDays > originalDays) finalChargedDays = originalDays;

                    actualCost = pricePerNight * finalChargedDays;
                }

                var payments = await _paymentRepo.GetByReservationIdAsync(reservationId);
                decimal totalPaid = payments.Sum(p => p.Amount);

                decimal refundAmount = totalPaid - actualCost;
                if (refundAmount < 0) refundAmount = 0;

                if (refundAmount > 0)
                {
                    await _paymentService.ProcessRefundAsync(reservationId, refundAmount);
                    res.PaymentStatus = "Refunded";
                }

                res.RoomStatus = "CheckedOut";
                res.CheckOutTime = DateTime.Now;
                res.CheckOut = res.CheckIn.AddDays(finalChargedDays);
                res.TotalPrice = actualCost;
                res.LastModified = DateTime.Now;

                if (room != null)
                {
                    room.Status = "Available";
                    room.CleanStatus = "Dirty";
                    await _roomRepo.UpdateAsync(room);
                }

                await _reservationRepo.UpdateAsync(res);
                await _reservationRepo.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
