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
                    IsEditing = value != null;
                    if (value != null) PopulateFolio(value);
                }
            }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        // --- Folio Details ---
        private ObservableCollection<LineItemDisplayModel> _folioLineItems;
        public ObservableCollection<LineItemDisplayModel> FolioLineItems { get => _folioLineItems; set => SetProperty(ref _folioLineItems, value); }
        
        private decimal _folioBalanceDue;
        public decimal FolioBalanceDue { get => _folioBalanceDue; set => SetProperty(ref _folioBalanceDue, value); }
        
        private LineItemDisplayModel _selectedLineItem;
        public LineItemDisplayModel SelectedLineItem { get => _selectedLineItem; set => SetProperty(ref _selectedLineItem, value); }
        
        public bool IsAccountant => AuthService.IsAccountant();

        public ICommand PayCashCommand { get; }
        public ICommand PayCardCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand VerifyPaymentCommand { get; }
        public ICommand GenerateInvoiceCommand { get; }
        public ICommand DownloadReceiptCommand { get; }

        public PaymentsViewModel()
        {
            PayCashCommand = new RelayCommand(_ => ProcessPayment("Cash"));
            PayCardCommand = new RelayCommand(_ => ProcessPayment("Credit Card"));
            CancelEditCommand = new RelayCommand(_ => CancelEdit());
            VerifyPaymentCommand = new RelayCommand(_ => VerifyPayment());
            GenerateInvoiceCommand = new RelayCommand(_ => GenerateInvoice());
            DownloadReceiptCommand = new RelayCommand(_ => DownloadReceipt(), _ => SelectedLineItem?.IsPayment == true);
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
            IsEditing = false;
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

        private async void ProcessPayment(string method)
        {
            if (SelectedFolio == null)
            {
                MessageBox.Show("Please select a folio to process payment.");
                return;
            }

            if (FolioBalanceDue == 0)
            {
                MessageBox.Show("This folio is already fully settled.");
                return;
            }

            if (FolioBalanceDue < 0)
            {
                var ask = MessageBox.Show($"Are you sure you want to process a REFUND of {Math.Abs(FolioBalanceDue):C}?", "Confirm Refund", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (ask != MessageBoxResult.Yes) return;
                
                try 
                {
                    var payment = new PaymentModel
                    {
                        Id = DataStore.GenerateId(),
                        ReservationId = SelectedFolio.ReservationId,
                        Amount = FolioBalanceDue, // Negative represents payback
                        Date = DateTime.Now,
                        Method = method,
                        Status = "Refunded",
                        RecordedByUserId = AuthService.CurrentUser?.Id
                    };

                    await ApiService.PostAsync<PaymentModel>("payments", payment);
                    await DataStore.LoadAsync();
                    
                    AuditService.Log("Payment Refunded", $"Refunded {Math.Abs(FolioBalanceDue):C} for {SelectedFolio.GuestName}");
                    MessageBox.Show($"Refund of {Math.Abs(FolioBalanceDue):C} processed via {method} for {SelectedFolio.GuestName}.");
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error processing refund: {ex.Message}");
                }
                return;
            }

            try 
            {
                var payment = new PaymentModel
                {
                    Id = DataStore.GenerateId(),
                    ReservationId = SelectedFolio.ReservationId,
                    Amount = FolioBalanceDue,
                    Date = DateTime.Now,
                    Method = method,
                    Status = "Paid",
                    RecordedByUserId = AuthService.CurrentUser?.Id
                };

                await ApiService.PostAsync<PaymentModel>("payments", payment);
                await DataStore.LoadAsync();
                
                AuditService.Log("Payment Collected", $"Collected {FolioBalanceDue:C} for {SelectedFolio.GuestName}");
                MessageBox.Show($"Payment of {FolioBalanceDue:C} recorded via {method} for {SelectedFolio.GuestName}.");
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing payment: {ex.Message}");
            }
        }

        private async void VerifyPayment()
        {
            if (!IsAccountant) { MessageBox.Show("Only Accountants can verify payments.", "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
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
            if (!IsAccountant) { MessageBox.Show("Only Accountants can generate official invoices.", "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (SelectedFolio == null) return;

            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string invoiceFile = Path.Combine(docPath, $"Invoice_{SelectedFolio.ReservationId}.txt");

            var sb = new StringBuilder();
            sb.AppendLine("==========================================");
            sb.AppendLine("            OFFICIAL INVOICE              ");
            sb.AppendLine("               NOCTURNAL                  ");
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
            sb.AppendLine("               NOCTURNAL                  ");
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
            sb.AppendLine("   Thank you for choosing NOCTURNAL!     ");
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
    }
}
