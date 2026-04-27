using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using HRS.Models;
using HRS.Services;

namespace HRS.ViewModels
{
    public class CustomersViewModel : ViewModelBase
    {
        private ObservableCollection<CustomerModel> _customers;
        public ObservableCollection<CustomerModel> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        private CustomerModel _selectedCustomer;
        public CustomerModel SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (SetProperty(ref _selectedCustomer, value))
                {
                    if (value == null)
                    {
                        IsViewingDetails = false;
                        IsEditing = false;
                    }
                    
                    if (value != null)
                    {
                        // Copy values to the editing context to avoid modifying the grid directly before save
                        EditingContext = new CustomerModel
                        {
                            Id = value.Id,
                            FullName = value.FullName,
                            Phone = value.Phone,
                            Email = value.Email,
                            Gender = value.Gender,
                            Nationality = value.Nationality,
                            Address = value.Address,
                            IdType = value.IdType,
                            IdNumber = value.IdNumber,
                            IdExpiryDate = value.IdExpiryDate,
                            PassportNumber = value.PassportNumber,
                            DateOfBirth = value.DateOfBirth,
                            Occupation = value.Occupation,
                            Company = value.Company,
                            EmergencyContactName = value.EmergencyContactName,
                            EmergencyContactPhone = value.EmergencyContactPhone,
                            Notes = value.Notes,
                            CustomerType = value.CustomerType,
                            Status = value.Status,
                            IsBlacklisted = value.IsBlacklisted,
                            BlacklistReason = value.BlacklistReason,
                            PreferredRoomType = value.PreferredRoomType,
                            SmokingPreference = value.SmokingPreference,
                            FloorPreference = value.FloorPreference,
                            BedTypePreference = value.BedTypePreference,
                            LoyaltyPoints = value.LoyaltyPoints,
                            LoyaltyTier = value.LoyaltyTier,
                            CreatedDate = value.CreatedDate,
                            LastVisitDate = value.LastVisitDate
                        };
                    }
                }
            }
        }

        private CustomerModel _editingContext;
        public CustomerModel EditingContext
        {
            get => _editingContext;
            set => SetProperty(ref _editingContext, value);
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
        
        public bool CanManageCustomers => AuthService.IsAdmin() || AuthService.IsReceptionist();

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterData();
                }
            }
        }

        public ICommand AddCustomerCommand { get; }
        public ICommand SaveCustomerCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteCustomerCommand { get; }
        public ICommand ViewCustomerCommand { get; }
        public ICommand EditCustomerCommand { get; }

        public CustomersViewModel()
        {
            AddCustomerCommand = new RelayCommand(_ => { if (CanManageCustomers) AddCustomer(); });
            SaveCustomerCommand = new RelayCommand(_ => { if (CanManageCustomers) SaveCustomer(); });
            CancelEditCommand = new RelayCommand(_ => { IsEditing = false; IsViewingDetails = false; });
            DeleteCustomerCommand = new RelayCommand(_ => { if (CanManageCustomers) DeleteCustomer(); });
            ViewCustomerCommand = new RelayCommand(c => { SelectedCustomer = c as CustomerModel; IsViewingDetails = true; IsEditing = false; });
            EditCustomerCommand = new RelayCommand(c => { if (CanManageCustomers) { SelectedCustomer = c as CustomerModel; IsEditing = true; IsViewingDetails = false; } });

            LoadData();
        }

        private void LoadData()
        {
            FilterData();
        }

        private void FilterData()
        {
            var query = DataStore.Data.Customers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.ToLower();
                query = query.Where(c => 
                    (c.FullName?.ToLower().Contains(s) == true) ||
                    (c.Phone?.ToLower().Contains(s) == true) ||
                    (c.Email?.ToLower().Contains(s) == true) ||
                    (c.IdNumber?.ToLower().Contains(s) == true));
            }

            Customers = new ObservableCollection<CustomerModel>(query.OrderBy(c => c.FullName));
        }

        private void AddCustomer()
        {
            SelectedCustomer = null;
            EditingContext = new CustomerModel { Status = "Active", CustomerType = "Regular" };
            IsViewingDetails = false;
            IsEditing = true;
        }

        private async void SaveCustomer()
        {
            if (string.IsNullOrWhiteSpace(EditingContext.FullName) || string.IsNullOrWhiteSpace(EditingContext.Phone))
            {
                MessageBox.Show("Full Name and Phone Number are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try 
            {
                if (string.IsNullOrEmpty(EditingContext.Id))
                {
                    // New Customer
                    EditingContext.Id = DataStore.GenerateId();
                    await ApiService.PostAsync<CustomerModel>("customers", EditingContext);
                    AuditService.Log("Guest Registered", $"New guest profile created for {EditingContext.FullName}.", "Modification", "Info");
                }
                else
                {
                    // Update Existing
                    await ApiService.PutAsync($"customers/{EditingContext.Id}", EditingContext);
                    AuditService.Log("Guest Profile Updated", $"Updated profile details for {EditingContext.FullName}.", "Modification", "Info");
                }

                await DataStore.LoadAsync();
                LoadData();
                IsEditing = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving customer: {ex.Message}");
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
            SelectedCustomer = null;
            EditingContext = null;
        }

        private async void DeleteCustomer()
        {
            if (SelectedCustomer == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete {SelectedCustomer.FullName}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try 
                {
                    string guestName = SelectedCustomer.FullName;
                    await ApiService.DeleteAsync($"customers/{SelectedCustomer.Id}");
                    AuditService.Log("Guest Deleted", $"Removed guest profile for {guestName}.", "Modification", "Warning");
                    await DataStore.LoadAsync();
                    LoadData();
                    IsEditing = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting customer: {ex.Message}");
                }
            }
        }

        public void SelectCustomerById(string customerId)
        {
            var customer = DataStore.Data.Customers.FirstOrDefault(c => c.Id == customerId);
            if (customer != null)
            {
                SelectedCustomer = customer;
                EditingContext = new CustomerModel
                {
                    Id = customer.Id,
                    FullName = customer.FullName,
                    Phone = customer.Phone,
                    Email = customer.Email,
                    Gender = customer.Gender,
                    Nationality = customer.Nationality,
                    Address = customer.Address,
                    IdType = customer.IdType,
                    IdNumber = customer.IdNumber,
                    IdExpiryDate = customer.IdExpiryDate,
                    PassportNumber = customer.PassportNumber,
                    DateOfBirth = customer.DateOfBirth,
                    Occupation = customer.Occupation,
                    Company = customer.Company,
                    EmergencyContactName = customer.EmergencyContactName,
                    EmergencyContactPhone = customer.EmergencyContactPhone,
                    Notes = customer.Notes,
                    CustomerType = customer.CustomerType,
                    Status = customer.Status,
                    IsBlacklisted = customer.IsBlacklisted,
                    BlacklistReason = customer.BlacklistReason,
                    PreferredRoomType = customer.PreferredRoomType,
                    SmokingPreference = customer.SmokingPreference,
                    FloorPreference = customer.FloorPreference,
                    BedTypePreference = customer.BedTypePreference,
                    LoyaltyPoints = customer.LoyaltyPoints,
                    LoyaltyTier = customer.LoyaltyTier,
                    CreatedDate = customer.CreatedDate,
                    LastVisitDate = customer.LastVisitDate
                };
                IsEditing = true;
            }
        }
    }
}
