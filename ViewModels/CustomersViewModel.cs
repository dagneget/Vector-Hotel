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
                    IsEditing = value != null;
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
                            CustomerType = value.CustomerType,
                            Status = value.Status,
                            Occupation = value.Occupation,
                            Company = value.Company,
                            Notes = value.Notes
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
            set => SetProperty(ref _isEditing, value);
        }

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

        public CustomersViewModel()
        {
            AddCustomerCommand = new RelayCommand(_ => AddCustomer());
            SaveCustomerCommand = new RelayCommand(_ => SaveCustomer());
            CancelEditCommand = new RelayCommand(_ => CancelEdit());
            DeleteCustomerCommand = new RelayCommand(_ => DeleteCustomer());

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
                }
                else
                {
                    // Update Existing
                    await ApiService.PutAsync($"customers/{EditingContext.Id}", EditingContext);
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
                    await ApiService.DeleteAsync($"customers/{SelectedCustomer.Id}");
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
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Address = customer.Address,
                    PassportNumber = customer.PassportNumber,
                    Status = customer.Status,
                    CustomerType = customer.CustomerType,
                    CreatedDate = customer.CreatedDate
                };
                IsEditing = true;
            }
        }
    }
}
