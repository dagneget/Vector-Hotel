using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HRS.Models;
using HRS.Services;
using System.IO;
using System.Text;

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

    public class TopGuestModel
    {
        public string Name { get; set; }
        public int VisitCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public double Value { get; set; }
        public double Percentage { get; set; } // For visual bar heights
    }

    public class ReportsViewModel : ViewModelBase
    {
        private ObservableCollection<ReportDisplayModel> _reportData;
        public ObservableCollection<ReportDisplayModel> ReportData
        {
            get => _reportData;
            set => SetProperty(ref _reportData, value);
        }

        // --- Core KPIs ---
        private string _totalRevenue;
        public string TotalRevenue { get => _totalRevenue; set => SetProperty(ref _totalRevenue, value); }

        private string _adr;
        public string Adr { get => _adr; set => SetProperty(ref _adr, value); }

        private string _revPar;
        public string RevPar { get => _revPar; set => SetProperty(ref _revPar, value); }

        private double _occupancyRate;
        public double OccupancyRate { get => _occupancyRate; set => SetProperty(ref _occupancyRate, value); }

        // --- Taxation ---
        private string _taxAmount;
        public string TaxAmount { get => _taxAmount; set => SetProperty(ref _taxAmount, value); }

        private string _netIncome;
        public string NetIncome { get => _netIncome; set => SetProperty(ref _netIncome, value); }

        // --- Analytics Data ---
        public ObservableCollection<ChartDataPoint> RevenueChartData { get; set; } = new ObservableCollection<ChartDataPoint>();
        public ObservableCollection<ChartDataPoint> RoomPopularityData { get; set; } = new ObservableCollection<ChartDataPoint>();
        public ObservableCollection<TopGuestModel> TopGuests { get; set; } = new ObservableCollection<TopGuestModel>();

        private int _futureBookingsCount;
        public int FutureBookingsCount { get => _futureBookingsCount; set => SetProperty(ref _futureBookingsCount, value); }

        private int _transactionCount;
        public int TransactionCount { get => _transactionCount; set => SetProperty(ref _transactionCount, value); }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) FilterData(); }
        }

        // --- Commands ---
        public ICommand FilterCommand { get; }
        public ICommand GenerateReportCommand { get; }

        private string _activeFilter = "All";
        public string ActiveFilter { get => _activeFilter; set => SetProperty(ref _activeFilter, value); }

        public ReportsViewModel()
        {
            FilterCommand = new RelayCommand(p => { ActiveFilter = p.ToString(); FilterData(); });
            GenerateReportCommand = new RelayCommand(_ => GenerateProfessionalReport());
            LoadData();
        }

        private void LoadData()
        {
            CalculateAnalytics();
            FilterData();
        }

        private void CalculateAnalytics()
        {
            var payments = DataStore.Data.Payments;
            var reservations = DataStore.Data.Reservations;
            var rooms = DataStore.Data.Rooms;
            var customers = DataStore.Data.Customers;

            // 1. Revenue Growth (Last 7 intervals for the chart)
            RevenueChartData.Clear();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                var dayRevenue = (double)payments.Where(p => p.Date.Date == date.Date).Sum(p => p.Amount);
                RevenueChartData.Add(new ChartDataPoint { Label = date.ToString("ddd"), Value = dayRevenue });
            }
            // Normalize for bar heights (0-100)
            double maxVal = RevenueChartData.Max(d => d.Value);
            if (maxVal > 0) foreach (var d in RevenueChartData) d.Percentage = (d.Value / maxVal) * 100;

            // 2. Room Popularity
            RoomPopularityData.Clear();
            var roomTypeRevenue = reservations
                .Join(rooms, r => r.RoomId, rm => rm.Id, (r, rm) => new { rm.BedType, r.TotalPrice })
                .GroupBy(x => x.BedType)
                .Select(g => new ChartDataPoint { Label = g.Key, Value = (double)g.Sum(x => x.TotalPrice) })
                .OrderByDescending(x => x.Value);
            
            foreach (var r in roomTypeRevenue) RoomPopularityData.Add(r);

            // 3. KPIs
            int totalRooms = rooms.Count;
            int occupiedToday = reservations.Count(r => r.CheckIn.Date <= DateTime.Today && r.CheckOut.Date >= DateTime.Today && r.RoomStatus != "Cancelled");
            
            OccupancyRate = totalRooms > 0 ? (double)occupiedToday / totalRooms : 0;
            
            decimal totalRevenueVal = payments.Sum(p => p.Amount);
            Adr = occupiedToday > 0 ? (totalRevenueVal / occupiedToday).ToString("C0") : "$0";
            RevPar = totalRooms > 0 ? (totalRevenueVal / totalRooms).ToString("C0") : "$0";

            // 4. Taxation (Assuming 10% tax rate from settings or default)
            decimal taxRate = 0.10m; // Fallback
            TaxAmount = (totalRevenueVal * taxRate).ToString("C2");
            NetIncome = (totalRevenueVal * (1 - taxRate)).ToString("C2");

            // 5. Top Guests
            TopGuests.Clear();
            var topList = payments
                .Join(reservations, p => p.ReservationId, r => r.Id, (p, r) => new { p, r })
                .GroupBy(x => x.r.CustomerId)
                .Select(g => new { 
                    CustomerId = g.Key, 
                    TotalSpent = g.Sum(x => x.p.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(5);

            foreach (var item in topList)
            {
                var cust = customers.FirstOrDefault(c => c.Id == item.CustomerId);
                if (cust != null)
                {
                    TopGuests.Add(new TopGuestModel { Name = cust.FullName, TotalSpent = item.TotalSpent, VisitCount = item.Count });
                }
            }

            // 6. Future Bookings
            FutureBookingsCount = reservations.Count(r => r.CheckIn > DateTime.Today && r.RoomStatus != "Cancelled");
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

            // Apply Date Filter
            switch (ActiveFilter)
            {
                case "Today":
                    payments = payments.Where(p => p.Date.Date == DateTime.Today);
                    break;
                case "Week":
                    var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    payments = payments.Where(p => p.Date.Date >= startOfWeek);
                    break;
                case "Month":
                    payments = payments.Where(p => p.Date.Month == DateTime.Now.Month && p.Date.Year == DateTime.Now.Year);
                    break;
                case "YTD":
                    payments = payments.Where(p => p.Date.Year == DateTime.Now.Year);
                    break;
            }

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

        private void GenerateProfessionalReport()
        {
            try
            {
                string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string reportsFolder = Path.Combine(docPath, "HRS_Reports");
                Directory.CreateDirectory(reportsFolder);
                
                string fileName = $"Financial_Statement_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string fullPath = Path.Combine(reportsFolder, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("==================================================");
                sb.AppendLine("           FINANCIAL INTELLIGENCE STATEMENT       ");
                sb.AppendLine("                    NOCTURNAL                     ");
                sb.AppendLine("==================================================");
                sb.AppendLine($"Generated On: {DateTime.Now:f}");
                sb.AppendLine($"Filter Range: {ActiveFilter}");
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine("SUMMARY PERFORMANCE:");
                sb.AppendLine($"  Gross Revenue:    {TotalRevenue}");
                sb.AppendLine($"  Tax Liability:    {TaxAmount}");
                sb.AppendLine($"  Net Income:       {NetIncome}");
                sb.AppendLine($"  Occupancy Rate:   {OccupancyRate:P1}");
                sb.AppendLine($"  ADR:              {Adr}");
                sb.AppendLine($"  RevPAR:           {RevPar}");
                sb.AppendLine($"  Transactions:     {TransactionCount}");
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine();
                sb.AppendLine("TRANSACTION DETAILS:");
                sb.AppendLine(string.Format("{0,-12} | {1,-20} | {2,-10} | {3,10}", "DATE", "GUEST", "METHOD", "AMOUNT"));
                sb.AppendLine(new string('-', 60));

                foreach (var item in ReportData)
                {
                    sb.AppendLine(string.Format("{0,-12} | {1,-20} | {2,-10} | {3,10:C2}", 
                        item.Date.ToString("yyyy-MM-dd"), 
                        item.GuestName.Length > 20 ? item.GuestName.Substring(0, 17) + "..." : item.GuestName,
                        item.Method, 
                        item.Amount));
                }

                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine($"Generated By: {AuthService.CurrentUser?.Username?.ToUpper()}");
                sb.AppendLine("==================================================");
                sb.AppendLine("          END OF FINANCIAL STATEMENT              ");
                sb.AppendLine("==================================================");

                File.WriteAllText(fullPath, sb.ToString());
                AuditService.Log("Generated Report", $"Exported financial statement to {fileName}.");

                var result = System.Windows.MessageBox.Show(
                    $"Financial Statement successfully generated!\n\nSaved at:\n{fullPath}\n\nWould you like to open the folder?",
                    "Report Generated",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Information);
                
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error generating report: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
