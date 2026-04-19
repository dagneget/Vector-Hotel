using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HRS.Models;
using HRS.Services;

namespace HRS.ViewModels
{
    public class ReportDisplayModel : ViewModelBase
    {
        public string PaymentId { get; set; }
        public string GuestName { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; }
        public DateTime Date { get; set; }
    }

    public class ReportsViewModel : ViewModelBase
    {
        private ObservableCollection<ReportDisplayModel> _reportData;
        public ObservableCollection<ReportDisplayModel> ReportData
        {
            get => _reportData;
            set => SetProperty(ref _reportData, value);
        }

        private string _totalRevenue;
        public string TotalRevenue
        {
            get => _totalRevenue;
            set => SetProperty(ref _totalRevenue, value);
        }

        private int _transactionCount;
        public int TransactionCount
        {
            get => _transactionCount;
            set => SetProperty(ref _transactionCount, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) FilterData(); }
        }

        public ReportsViewModel()
        {
            LoadData();
        }

        private void LoadData()
        {
            FilterData();
        }

        private void FilterData()
        {
            var payments = DataStore.Data.Payments
                .Join(DataStore.Data.Reservations, p => p.ReservationId, r => r.Id, (p, r) => new { p, r })
                .Join(DataStore.Data.Customers, combined => combined.r.CustomerId, c => c.Id, (combined, c) => new ReportDisplayModel
                {
                    PaymentId = combined.p.Id,
                    GuestName = c.FullName,
                    Amount = combined.p.Amount,
                    Method = combined.p.Method,
                    Date = combined.p.Date
                });

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.ToLower();
                payments = payments.Where(p => p.GuestName.ToLower().Contains(s) || p.PaymentId.ToLower().Contains(s));
            }

            var list = payments.OrderByDescending(p => p.Date).ToList();
            ReportData = new ObservableCollection<ReportDisplayModel>(list);
            
            TotalRevenue = list.Sum(p => p.Amount).ToString("C2");
            TransactionCount = list.Count;
        }
    }
}
