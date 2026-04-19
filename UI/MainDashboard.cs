using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using HRS.Services;

namespace HRS.UI
{
    public class MainDashboard : Form
    {
        private Guna2Panel sidebarPanel;
        private Guna2Panel contentPanel;
        private Guna2Panel headerPanel;
        private Guna2DragControl dragControl;

        public MainDashboard()
        {
            this.Text = "Hotel Reservation System - Dashboard";
            this.Size = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.Surface;
            this.FormBorderStyle = FormBorderStyle.None;
            
            InitializeLayout();
            LoadView(new DashboardView());
        }

        private void InitializeLayout()
        {
            dragControl = new Guna2DragControl { TargetControl = headerPanel };
            Guna2BorderlessForm borderlessForm = new Guna2BorderlessForm { ContainerControl = this, BorderRadius = 0 };

            sidebarPanel = new Guna2Panel { Dock = DockStyle.Left, Width = 250, FillColor = Theme.SurfaceContainerLowest };
            headerPanel = new Guna2Panel { Dock = DockStyle.Top, Height = 60, FillColor = Theme.SurfaceContainerHigh, ShadowDecoration = { Enabled = true, Depth = 20 } };
            contentPanel = new Guna2Panel { Dock = DockStyle.Fill, FillColor = Theme.Surface };

            Label lblApp = new Label { Text = "NOCTURNAL", Font = Theme.HeadlineFont, ForeColor = Theme.Primary, AutoSize = true, Location = new Point(20, 15), BackColor = Color.Transparent };
            headerPanel.Controls.Add(lblApp);

            Label lblUser = new Label { Text = $"User: {AuthService.CurrentUser.Username} | Role: {AuthService.CurrentUser.Role}", Font = Theme.BodyFont, ForeColor = Theme.OnSurfaceVariant, AutoSize = true, Location = new Point(1000, 20), Anchor = AnchorStyles.Top | AnchorStyles.Right, BackColor = Color.Transparent };
            headerPanel.Controls.Add(lblUser);

            // Window Controls (Minimize, Maximize, Close) attached directly to Form for proper Guna rendering
            Guna2ControlBox btnMinimize = new Guna2ControlBox { Anchor = AnchorStyles.Top | AnchorStyles.Right, ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MinimizeBox, Size = new Size(45, 29), Location = new Point(1145, 15), FillColor = Theme.SurfaceContainerHigh, IconColor = Theme.OnSurface };
            this.Controls.Add(btnMinimize);
            btnMinimize.BringToFront();

            Guna2ControlBox btnMaximize = new Guna2ControlBox { Anchor = AnchorStyles.Top | AnchorStyles.Right, ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MaximizeBox, Size = new Size(45, 29), Location = new Point(1190, 15), FillColor = Theme.SurfaceContainerHigh, IconColor = Theme.OnSurface };
            this.Controls.Add(btnMaximize);
            btnMaximize.BringToFront();

            Guna2ControlBox btnClose = new Guna2ControlBox { Anchor = AnchorStyles.Top | AnchorStyles.Right, ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.CloseBox, Size = new Size(45, 29), Location = new Point(1235, 15), FillColor = Theme.SurfaceContainerHigh, IconColor = Theme.OnSurface };
            this.Controls.Add(btnClose);
            btnClose.BringToFront();

            string[] menuItems = { "Dashboard", "Reservations", "Customers", "Rooms", "Room Types", "Check-In/Out", "Payments", "Reports" };
            int y = 50;
            foreach (var item in menuItems)
            {
                Guna2Button btn = new Guna2Button
                {
                    Text = item,
                    Font = Theme.BodyFont,
                    ForeColor = Theme.OnSurface,
                    FillColor = Color.Transparent,
                    HoverState = { FillColor = Theme.PrimaryContainer, ForeColor = Color.White },
                    Size = new Size(250, 50),
                    Location = new Point(0, y),
                    TextAlign = HorizontalAlignment.Left,
                    TextOffset = new Point(20, 0),
                    Cursor = Cursors.Hand
                };
                btn.Click += SidebarItem_Click;
                sidebarPanel.Controls.Add(btn);
                y += 50;
            }

            Guna2Button btnLogout = new Guna2Button
            {
                Text = "Logout",
                Font = Theme.BodyFont,
                ForeColor = Theme.Tertiary,
                FillColor = Color.Transparent,
                HoverState = { FillColor = Theme.SurfaceContainerHigh },
                Size = new Size(250, 50),
                Location = new Point(0, this.ClientSize.Height - 50),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                TextAlign = HorizontalAlignment.Left,
                TextOffset = new Point(20, 0),
                Cursor = Cursors.Hand
            };
            btnLogout.Click += (s, e) => { AuthService.Logout(); this.Hide(); new LoginForm().ShowDialog(); this.Close(); };
            sidebarPanel.Controls.Add(btnLogout);

            this.Controls.Add(contentPanel);
            this.Controls.Add(headerPanel);
            this.Controls.Add(sidebarPanel);
        }

        private void SidebarItem_Click(object sender, EventArgs e)
        {
            Guna2Button btn = sender as Guna2Button;
            switch(btn.Text)
            {
                case "Dashboard": LoadView(new DashboardView()); break;
                case "Customers": LoadView(new CustomersView()); break;
                case "Rooms": LoadView(new RoomsView()); break;
                case "Room Types": LoadView(new RoomTypesView()); break;
                case "Reservations": LoadView(new ReservationsView()); break;
                case "Check-In/Out": LoadView(new CheckInOutView()); break;
                case "Payments": LoadView(new PaymentsView()); break;
                case "Reports": LoadView(new ReportsView()); break;
            }
        }

        private void LoadView(UserControl view)
        {
            contentPanel.Controls.Clear();
            view.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(view);
        }
    }
}
