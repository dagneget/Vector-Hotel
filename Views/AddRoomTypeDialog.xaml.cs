using System;
using System.Windows;
using HRS.Models;
using HRS.Services;

namespace HRS.Views
{
    public partial class AddRoomTypeDialog : Window
    {
        public RoomTypeModel NewRoomType { get; private set; }

        public AddRoomTypeDialog()
        {
            InitializeComponent();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnCreateClick(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(TypeNameTextBox.Text))
            {
                MessageBox.Show("Please enter a room type name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(BasePriceTextBox.Text) || !decimal.TryParse(BasePriceTextBox.Text, out decimal basePrice))
            {
                MessageBox.Show("Please enter a valid base price.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create new room type
            NewRoomType = new RoomTypeModel
            {
                Id = DataStore.GenerateId(),
                Name = TypeNameTextBox.Text.Trim(),
                BasePrice = basePrice
            };

            DialogResult = true;
            Close();
        }
    }
}
