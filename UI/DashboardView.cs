using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using HRS.Services;

namespace HRS.UI
{
    public class DashboardView : UserControl
    {
        private Label lblTotalGuests;
        private Label lblOccupancy;
        private Label lblActiveReservations;
        private Label lblTotalRevenue;

        public DashboardView()
        {
            this.BackColor = Theme.Surface;
            InitializeLayout();
            EventBus.Instance.DataChanged += LoadData;
            LoadData();
        }

        private void InitializeLayout()
        {
            Label lblTitle = new Label { Text = "Business Overview", Font = Theme.DisplayFont, ForeColor = Theme.OnSurface, AutoSize = true, Location = new Point(20, 20) };
            this.Controls.Add(lblTitle);

            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 90),
                Size = new Size(1000, 200),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.Transparent
            };

            lblTotalGuests = CreateStatCard(flowPanel, "Total Guests", "#0");
            lblOccupancy = CreateStatCard(flowPanel, "Occupancy Rate", "0%");
            lblActiveReservations = CreateStatCard(flowPanel, "Active Reservations", "#0");
            lblTotalRevenue = CreateStatCard(flowPanel, "Total Revenue", "$0.00");

            this.Controls.Add(flowPanel);
        }

        private Label CreateStatCard(Control parent, string title, string initialValue)
        {
            Guna2Panel card = new Guna2Panel { Size = new Size(220, 140), FillColor = Theme.SurfaceContainerHigh, BorderRadius = 12, Margin = new Padding(10), ShadowDecoration = { Enabled = true, Depth = 20, Color = Color.Black } };
            
            Label lblTitle = new Label { Text = title, Font = Theme.BodyFont, ForeColor = Theme.OnSurfaceVariant, AutoSize = true, Location = new Point(20, 20), BackColor = Color.Transparent };
            Label lblValue = new Label { Text = initialValue, Font = Theme.DisplayFont, ForeColor = Theme.Primary, AutoSize = true, Location = new Point(20, 60), BackColor = Color.Transparent };
            
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblValue);
            parent.Controls.Add(card);
            
            return lblValue;
        }

        private void LoadData()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(LoadData)); return; }
            if (DataStore.Data == null) return;
            
            lblTotalGuests.Text = DataStore.Data.Customers.Count.ToString();
            
            // Occupancy logic calculation: Total CheckedIn rooms / Total Operable Rooms
            int occupied = DataStore.Data.Reservations.Count(r => r.Status == "CheckedIn");
            int totalRooms = DataStore.Data.Rooms.Count(r => r.Status != "OutOfOrder");
            double occRate = totalRooms > 0 ? ((double)occupied / totalRooms) * 100 : 0;
            lblOccupancy.Text = occRate.ToString("0.0") + "%";
            
            lblActiveReservations.Text = DataStore.Data.Reservations.Count(r => r.Status != "Cancelled" && r.Status != "CheckedOut").ToString();
            lblTotalRevenue.Text = "$" + DataStore.Data.Payments.Sum(p => p.Amount).ToString("0.00");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) EventBus.Instance.DataChanged -= LoadData;
            base.Dispose(disposing);
        }
    }
}
