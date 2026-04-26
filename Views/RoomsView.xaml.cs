using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HRS.ViewModels;

namespace HRS.Views
{
    public partial class RoomsView : UserControl
    {
        public RoomsView()
        {
            InitializeComponent();
        }

        private void OnRoomRowDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is RoomDisplayModel room)
            {
                if (DataContext is RoomsViewModel viewModel)
                {
                    viewModel.ViewRoomDetailsCommand.Execute(room);
                }
            }
        }
    }
}
