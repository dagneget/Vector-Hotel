using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HRS.ViewModels;

namespace HRS.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void OnOccupancyClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.ViewOccupancyDetailsCommand.Execute(null);
            }
        }

        private void OnAdrClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.ViewAdrDetailsCommand.Execute(null);
            }
        }

        private void OnRevparClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.ViewRevparDetailsCommand.Execute(null);
            }
        }

        private void OnArrivalsClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.ViewArrivalsDetailsCommand.Execute(null);
            }
        }

        private void OnOccupiedRoomsClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.ViewOccupiedRoomsCommand.Execute(null);
            }
        }

        private void OnAvailableRoomsClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.ViewAvailableRoomsCommand.Execute(null);
            }
        }

        private void OnMaintenanceRoomsClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.ViewMaintenanceRoomsCommand.Execute(null);
            }
        }

        private void OnDetailItemClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel vm && sender is FrameworkElement element)
            {
                vm.ViewCustomerDetailsCommand.Execute(element.DataContext);
            }
        }

        private void OnFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.ApplyFilterCommand.Execute(null);
            }
        }

        private void OnRoomDetailItemClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel vm && sender is FrameworkElement element)
            {
                vm.ViewRoomDetailsCommand.Execute(element.DataContext);
            }
        }
    }
}
