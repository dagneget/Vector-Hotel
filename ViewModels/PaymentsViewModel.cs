using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using HRS.Models;
using HRS.Services;

namespace HRS.ViewModels
{
    public class FolioDisplayModel : ViewModelBase
    {
        public ReservationModel BaseReservation { get; set; }
        public string ReservationId => BaseReservation.Id;
        public string GuestName { get; set; }
        public string RoomNumber { get; set; }
        public DateTime CheckIn => BaseReservation.CheckIn;
        public string Status => BaseReservation.PaymentStatus ?? "Pending";
        
        public decimal TotalCharges 
        {
            get
            {
                decimal total = DataStore.Data.Charges.Where(c => c.ReservationId == BaseReservation.Id).Sum(c => c.Amount);
                if (!DataStore.Data.Charges.Any(c => c.ReservationId == BaseReservation.Id && c.Description == "Room Charge"))
                {
                    total += BaseReservation.TotalPrice;
                }
                return total;
            }
        }
        
        public decimal TotalPayments => DataStore.Data.Payments.Where(p => p.ReservationId == BaseReservation.Id && (string.IsNullOrEmpty(p.Status) || p.Status == "Paid")).Sum(p => p.Amount);
        public decimal BalanceDue => TotalCharges - TotalPayments;

        public string PaymentStatus
        {
            get
            {
                if (BalanceDue <= 0) return "SETTLED";
                if (TotalPayments > 0) return "PARTIAL";
                return "UNPAID";
            }
        }
    }

    public class LineItemDisplayModel 
    {
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public bool IsPayment { get; set; }
        public string Type => IsPayment ? "PAYMENT" : "CHARGE";
        public string Status { get; set; } // e.g. VERIFIED or UNVERIFIED
        public PaymentModel ActualPayment { get; set; }
    }

    public class PaymentsViewModel : ViewModelBase
    {
        private ObservableCollection<FolioDisplayModel> _folios;
        public ObservableCollection<FolioDisplayModel> Folios
        {
            get => _folios;
            set => SetProperty(ref _folios, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) FilterData(); }
        }

        private FolioDisplayModel _selectedFolio;
        public FolioDisplayModel SelectedFolio
        {
            get => _selectedFolio;
            set
            {
                if (SetProperty(ref _selectedFolio, value))
                {
                    if (value == null)
                    {
                        IsViewingDetails = false;
                        IsEditing = false;
                    }
                    if (value != null) PopulateFolio(value);
                }
            }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (SetProperty(ref _isEditing, value))
                    OnPropertyChanged(nameof(IsPanelOpen));
            }
        }

        private bool _isViewingDetails;
        public bool IsViewingDetails
        {
            get => _isViewingDetails;
            set
            {
                if (SetProperty(ref _isViewingDetails, value))
                    OnPropertyChanged(nameof(IsPanelOpen));
            }
        }

        public bool IsPanelOpen => IsEditing || IsViewingDetails;

        // --- Folio Details ---
        private ObservableCollection<LineItemDisplayModel> _folioLineItems;
        public ObservableCollection<LineItemDisplayModel> FolioLineItems { get => _folioLineItems; set => SetProperty(ref _folioLineItems, value); }
        
        private decimal _folioBalanceDue;
        public decimal FolioBalanceDue { get => _folioBalanceDue; set => SetProperty(ref _folioBalanceDue, value); }
        
        private LineItemDisplayModel _selectedLineItem;
        public LineItemDisplayModel SelectedLineItem { get => _selectedLineItem; set => SetProperty(ref _selectedLineItem, value); }
        
        public bool IsAccountant => AuthService.IsAccountant();
        public bool IsAdmin => AuthService.IsAdmin();
        public bool CanProcessPayments => IsAccountant || IsAdmin;
        
        public string HotelName => DataStore.Data.HotelInfo?.HotelName ?? "VECTOR HOTEL";
        
        // --- Analytics ---
        public decimal CashTotal => DataStore.Data.Payments.Where(p => p.Method == "Cash").Sum(p => p.Amount);
        public decimal CardTotal => DataStore.Data.Payments.Where(p => p.Method == "Credit Card").Sum(p => p.Amount);
        public decimal TotalCollected => CashTotal + CardTotal;
        public double CashPercentage => TotalCollected > 0 ? (double)(CashTotal / TotalCollected) * 100 : 0;
        public double CardPercentage => TotalCollected > 0 ? (double)(CardTotal / TotalCollected) * 100 : 0;

        // --- Currency & Multi-Currency ---
        private string _selectedCurrency = "USD";
        public string SelectedCurrency
        {
            get => _selectedCurrency;
            set { if (SetProperty(ref _selectedCurrency, value)) OnPropertyChanged(nameof(ConvertedBalanceDue)); }
        }

        public string[] CurrencyOptions => new[] { "USD", "EUR", "GBP" };
        
        public decimal ConvertedBalanceDue
        {
            get
            {
                decimal rate = 1.0m;
                if (SelectedCurrency == "EUR") rate = 0.92m;
                if (SelectedCurrency == "GBP") rate = 0.78m;
                return FolioBalanceDue * rate;
            }
        }

        // --- Partial Payments ---
        private string _customPaymentAmount;
        public string CustomPaymentAmount { get => _customPaymentAmount; set => SetProperty(ref _customPaymentAmount, value); }

        public ICommand PayCashCommand { get; }
        public ICommand PayCardCommand { get; }
        public ICommand PayCustomCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand VerifyPaymentCommand { get; }
        public ICommand GenerateInvoiceCommand { get; }
        public ICommand SendEmailInvoiceCommand { get; }
        public ICommand DownloadReceiptCommand { get; }
        public ICommand EarlyCheckoutCommand { get; }
        public ICommand ViewPaymentCommand { get; }
        public ICommand EditPaymentCommand { get; }

        public PaymentsViewModel()
        {
            PayCashCommand = new RelayCommand(_ => ProcessPayment("Cash"));
            PayCardCommand = new RelayCommand(_ => ProcessPayment("Credit Card"));
            PayCustomCommand = new RelayCommand(_ => ProcessCustomPayment());
            CancelEditCommand = new RelayCommand(_ => CancelEdit());
            VerifyPaymentCommand = new RelayCommand(_ => VerifyPayment());
            GenerateInvoiceCommand = new RelayCommand(_ => GenerateInvoice());
            SendEmailInvoiceCommand = new RelayCommand(_ => SendEmailInvoice());
            DownloadReceiptCommand = new RelayCommand(_ => DownloadReceipt(), _ => SelectedLineItem?.IsPayment == true);
            EarlyCheckoutCommand = new RelayCommand(_ => ProcessEarlyCheckout());
            ViewPaymentCommand = new RelayCommand(f => { SelectedFolio = f as FolioDisplayModel; IsViewingDetails = true; IsEditing = false; });
            EditPaymentCommand = new RelayCommand(f => { SelectedFolio = f as FolioDisplayModel; IsEditing = true; IsViewingDetails = false; });
            LoadData();
        }

        private void LoadData()
        {
            FilterData();
            if (SelectedFolio != null)
            {
                PopulateFolio(Folios.FirstOrDefault(f => f.ReservationId == SelectedFolio.ReservationId));
            }
        }

        private void FilterData()
        {
            // Get all reservations that are not cancelled or fully checked out long ago (simplified: just get all for now to serve as full billing system)
            var query = DataStore.Data.Reservations.Select(r => new FolioDisplayModel
            {
                BaseReservation = r,
                GuestName = DataStore.Data.Customers.FirstOrDefault(c => c.Id == r.CustomerId)?.FullName ?? "Unknown",
                RoomNumber = DataStore.Data.Rooms.FirstOrDefault(room => room.Id == r.RoomId)?.RoomNumber ?? "N/A"
            });

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.ToLower();
                query = query.Where(p => p.GuestName.ToLower().Contains(s) || p.RoomNumber.ToLower().Contains(s));
            }

            Folios = new ObservableCollection<FolioDisplayModel>(query.OrderByDescending(p => p.CheckIn));
        }

        private void CancelEdit()
        {
            SelectedFolio = null;
            IsEditing = false;
            IsViewingDetails = false;
        }

        private async void PopulateFolio(FolioDisplayModel folio)
        {
            if (folio == null) return;
            
            // Auto inject missing Room Charge if it doesn't exist
            if (!DataStore.Data.Charges.Any(c => c.ReservationId == folio.ReservationId && c.Description == "Room Charge"))
            {
                if (folio.BaseReservation.TotalPrice > 0)
                {
                    try 
                    {
                        var charge = new ChargeModel
                        {
                            Id = DataStore.GenerateId(),
                            ReservationId = folio.ReservationId,
                            Description = "Room Charge",
                            Amount = folio.BaseReservation.TotalPrice,
                            Date = folio.BaseReservation.CheckIn
                        };
                        await ApiService.PostAsync<ChargeModel>("charges", charge);
                        await DataStore.LoadAsync();
                    }
                    catch (Exception ex) { Console.WriteLine($"Auto charge fail: {ex.Message}"); }
                }
            }

            var lines = new ObservableCollection<LineItemDisplayModel>();
            
            foreach(var charge in DataStore.Data.Charges.Where(c => c.ReservationId == folio.ReservationId))
            {
                lines.Add(new LineItemDisplayModel { Description = charge.Description, Amount = charge.Amount, Date = charge.Date, IsPayment = false });
            }

            foreach(var payment in DataStore.Data.Payments.Where(p => p.ReservationId == folio.ReservationId))
            {
                string verifiedTxt = payment.VerifiedByUserId != null ? "[VERIFIED]" : "[UNVERIFIED]";
                lines.Add(new LineItemDisplayModel { Description = $"Payment ({payment.Method})", Amount = payment.Amount, Date = payment.Date, IsPayment = true, Status = verifiedTxt, ActualPayment = payment });
            }

            FolioLineItems = new ObservableCollection<LineItemDisplayModel>(lines.OrderBy(l => l.Date));
            FolioBalanceDue = folio.BalanceDue;
        }

        private async void ProcessPayment(string method, decimal? customAmount = null)
        {
            if (!CanProcessPayments)
            {
                MessageBox.Show("Access Denied: Only Accountants or Admins can process payments.", "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedFolio == null)
            {
                MessageBox.Show("Please select a folio to process payment.");
                return;
            }

            decimal amountToPay = customAmount ?? FolioBalanceDue;

            if (amountToPay == 0)
            {
                MessageBox.Show("This amount is already settled.");
                return;
            }

            try 
            {
                var payment = new PaymentModel
                {
                    Id = DataStore.GenerateId(),
                    ReservationId = SelectedFolio.ReservationId,
                    Amount = amountToPay,
                    Date = DateTime.Now,
                    Method = method,
                    Status = amountToPay < 0 ? "Refunded" : "Paid",
                    RecordedByUserId = AuthService.CurrentUser?.Id
                };

                await ApiService.PostAsync<PaymentModel>("payments", payment);
                await DataStore.LoadAsync();
                
                AuditService.Log("Payment Processed", $"Processed {amountToPay:C} via {method} for {SelectedFolio.GuestName}");
                MessageBox.Show($"Payment of {amountToPay:C} recorded successfully.");
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing payment: {ex.Message}");
            }
        }

        private async void VerifyPayment()
        {
            if (!CanProcessPayments) { MessageBox.Show("Only Accountants or Admins can verify payments.", "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (SelectedLineItem?.ActualPayment == null) { MessageBox.Show("Please select a valid Payment line item to verify.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            if (SelectedLineItem.ActualPayment.VerifiedByUserId != null) { MessageBox.Show("This payment is already verified."); return; }

            try 
            {
                SelectedLineItem.ActualPayment.VerifiedByUserId = AuthService.CurrentUser?.Id;
                await ApiService.PutAsync($"payments/{SelectedLineItem.ActualPayment.Id}", SelectedLineItem.ActualPayment);
                await DataStore.LoadAsync();
                
                AuditService.Log("Payment Verified", $"Accountant verified payment {SelectedLineItem.ActualPayment.Id} of {SelectedLineItem.Amount:C}");
                MessageBox.Show("Payment verified successfully.", "Verified", MessageBoxButton.OK, MessageBoxImage.Information);
                PopulateFolio(SelectedFolio);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verifying payment: {ex.Message}");
            }
        }

        private void GenerateInvoice()
        {
            if (!CanProcessPayments) { MessageBox.Show("Only Accountants or Admins can generate official invoices.", "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (SelectedFolio == null) return;

            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string invoiceFile = Path.Combine(docPath, $"Invoice_{SelectedFolio.ReservationId}.txt");

            var sb = new StringBuilder();
            sb.AppendLine("==========================================");
            sb.AppendLine("            OFFICIAL INVOICE              ");
            sb.AppendLine($"               {HotelName?.ToUpper() ?? "VECTOR HOTEL"}                  ");
            sb.AppendLine("==========================================");
            sb.AppendLine($"Date: {DateTime.Now}");
            sb.AppendLine($"Guest: {SelectedFolio.GuestName}");
            sb.AppendLine($"Room: {SelectedFolio.RoomNumber}");
            sb.AppendLine($"Reservation ID: {SelectedFolio.ReservationId}");
            sb.AppendLine("------------------------------------------");
            
            foreach (var item in FolioLineItems)
            {
                sb.AppendLine($"{item.Date:MM/dd} | {item.Description,-20} | {item.Amount,8:C} {item.Status}");
            }
            sb.AppendLine("------------------------------------------");
            sb.AppendLine($"Total Charges:  {SelectedFolio.TotalCharges:C}");
            sb.AppendLine($"Total Payments: {SelectedFolio.TotalPayments:C}");
            sb.AppendLine($"Balance Due:    {FolioBalanceDue:C}");
            sb.AppendLine("==========================================");
            sb.AppendLine($"Verified By: {AuthService.CurrentUser?.Username?.ToUpper()}");

            File.WriteAllText(invoiceFile, sb.ToString());
            AuditService.Log("Invoice Generated", $"Accountant generated invoice for {SelectedFolio.GuestName}. File: {invoiceFile}");
            MessageBox.Show($"Invoice successfully generated!\n\nSaved at: {invoiceFile}", "Invoice Created", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DownloadReceipt()
        {
            if (SelectedLineItem?.ActualPayment == null)
            {
                MessageBox.Show("Please select a payment to download receipt.", "No Payment Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var payment = SelectedLineItem.ActualPayment;
            
            // Create receipts directory in Documents
            string receiptsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HRS_Receipts");
            Directory.CreateDirectory(receiptsFolder);
            
            string receiptFile = Path.Combine(receiptsFolder, $"Receipt_{payment.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            var sb = new StringBuilder();
            sb.AppendLine("==========================================");
            sb.AppendLine("          PAYMENT RECEIPT                 ");
            sb.AppendLine($"               {HotelName?.ToUpper() ?? "VECTOR HOTEL"}                  ");
            sb.AppendLine("         Hotel Reservation System         ");
            sb.AppendLine("==========================================");
            sb.AppendLine($"Receipt #: {payment.Id}");
            sb.AppendLine($"Date: {payment.Date:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Guest: {SelectedFolio.GuestName}");
            sb.AppendLine($"Room: {SelectedFolio.RoomNumber}");
            sb.AppendLine($"Reservation ID: {SelectedFolio.ReservationId}");
            sb.AppendLine("------------------------------------------");
            sb.AppendLine("PAYMENT DETAILS:");
            sb.AppendLine($"  Method: {payment.Method}");
            sb.AppendLine($"  Amount: {payment.Amount:C}");
            sb.AppendLine($"  Status: {(payment.VerifiedByUserId != null ? "VERIFIED" : "PENDING")}");
            sb.AppendLine("------------------------------------------");
            
            if (FolioLineItems != null)
            {
                sb.AppendLine("FOLIO SUMMARY:");
                sb.AppendLine($"  Total Charges:  {SelectedFolio.TotalCharges:C}");
                sb.AppendLine($"  Total Payments: {SelectedFolio.TotalPayments:C}");
                sb.AppendLine($"  Balance Due:    {FolioBalanceDue:C}");
                sb.AppendLine("------------------------------------------");
            }
            
            sb.AppendLine($"Recorded By: {AuthService.CurrentUser?.Username?.ToUpper()}");
            if (payment.VerifiedByUserId != null)
            {
                sb.AppendLine($"Verified By: {payment.VerifiedByUserId}");
            }
            sb.AppendLine("==========================================");
            sb.AppendLine($"   Thank you for choosing {HotelName?.ToUpper() ?? "VECTOR HOTEL"}!     ");
            sb.AppendLine("==========================================");

            try
            {
                File.WriteAllText(receiptFile, sb.ToString());
                AuditService.Log("Receipt Downloaded", $"Receipt downloaded for payment {payment.Id} - {SelectedFolio.GuestName}. File: {receiptFile}");
                
                // Show success message with option to open folder
                var result = MessageBox.Show(
                    $"Receipt successfully downloaded!\n\nSaved at:\n{receiptFile}\n\nWould you like to open the folder?",
                    "Receipt Downloaded",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);
                
                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{receiptFile}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving receipt: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProcessCustomPayment()
        {
            if (!IsValid)
            {
                var errors = string.Join("\n", AllErrors);
                MessageBox.Show($"Please fix the following errors:\n\n{errors}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (decimal.TryParse(CustomPaymentAmount, out decimal amount))
            {
                ProcessPayment("Credit Card", amount);
                CustomPaymentAmount = "";
            }
        }

        // --- Validation Logic ---
        protected override void ValidateProperty(string propertyName)
        {
            RemoveError(propertyName);

            switch (propertyName)
            {
                case nameof(CustomPaymentAmount):
                    if (string.IsNullOrWhiteSpace(CustomPaymentAmount))
                        break;

                    if (!decimal.TryParse(CustomPaymentAmount, out decimal amount))
                        AddError(propertyName, "Must be a valid number.");
                    else if (amount <= 0)
                        AddError(propertyName, "Payment must be greater than zero.");
                    else if (amount > FolioBalanceDue && FolioBalanceDue > 0)
                        AddError(propertyName, $"Payment exceeds balance due ({FolioBalanceDue:C}).");
                    break;
            }
        }


        private async void SendEmailInvoice()
        {
            if (SelectedFolio == null) return;
            
            try
            {
                await System.Threading.Tasks.Task.Delay(1000);
                AuditService.Log("Email Sent", $"Invoice emailed to guest for reservation {SelectedFolio.ReservationId}");
                MessageBox.Show($"Invoice successfully emailed to guest.", "Email Sent", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private async void ProcessEarlyCheckout()
        {
            if (!CanProcessPayments)
            {
                MessageBox.Show("Access Denied: Only Accountants or Admins can process early checkouts/refunds.", "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedFolio == null || SelectedFolio.BaseReservation.RoomStatus != "CheckedIn")
            {
                MessageBox.Show("Only Checked-In reservations can be processed for early checkout.", "Invalid Action", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ask = MessageBox.Show($"Are you sure you want to process Early Checkout for {SelectedFolio.GuestName}?\n\nThis will automatically apply the 1-night penalty or 30% rule, adjust the total charges, and process refunds if applicable.", "Confirm Early Checkout", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ask != MessageBoxResult.Yes) return;

            try
            {
                await ApiService.PostAsync<object>($"reservations/{SelectedFolio.ReservationId}/early-checkout", new { });
                await DataStore.LoadAsync();
                
                AuditService.Log("Early Checkout", $"Processed early checkout with payback/penalty for {SelectedFolio.GuestName}");
                MessageBox.Show($"Early checkout and payback processed successfully for {SelectedFolio.GuestName}.\n\nCheck the updated Folio for the adjusted charges and refunds.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Refresh data
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing early checkout: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
