using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using HRS.Services;

namespace HRS.UI
{
    public class CheckInOutView : UserControl
    {
        private Guna2DataGridView grid;
        private Guna2TextBox txtSearch;

        public CheckInOutView()
        {
            this.BackColor = Theme.Surface;
            InitializeLayout();
            EventBus.Instance.DataChanged += LoadData;
            LoadData();
        }

        private void InitializeLayout()
        {
            Label lblTitle = new Label { Text = "Daily Check-In/Out Desk", Font = Theme.HeadlineFont, ForeColor = Theme.OnSurface, AutoSize = true, Location = new Point(20, 20) };
            this.Controls.Add(lblTitle);

            // Responsive Header
            FlowLayoutPanel headerControls = new FlowLayoutPanel { Location = new Point(320, 15), Size = new Size(640, 50), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent };

            txtSearch = new Guna2TextBox { PlaceholderText = "Search Guest/Room...", Font = Theme.BodyFont, Size = new Size(250, 40), FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, BorderColor = Theme.OutlineVariant, BorderRadius = 8, Margin = new Padding(0, 0, 10, 0) };
            txtSearch.TextChanged += (s, e) => LoadData();
            
            Guna2Button btnCheckIn = new Guna2Button { Text = "Check-In Selected", Font = Theme.BodyFont, FillColor = Theme.Tertiary, ForeColor = Theme.OnTertiaryContainer, BorderRadius = 8, Size = new Size(160, 40), Margin = new Padding(0, 0, 10, 0), Cursor = Cursors.Hand };
            btnCheckIn.Click += (s, e) => ChangeStatus("Confirmed", "CheckedIn");

            Guna2Button btnCheckOut = new Guna2Button { Text = "Check-Out Selected", Font = Theme.BodyFont, FillColor = Theme.OrangeAccent, ForeColor = Color.White, BorderRadius = 8, Size = new Size(160, 40), Cursor = Cursors.Hand };
            btnCheckOut.Click += (s, e) => ChangeStatus("CheckedIn", "CheckedOut");

            headerControls.Controls.Add(txtSearch);
            headerControls.Controls.Add(btnCheckIn);
            headerControls.Controls.Add(btnCheckOut);
            this.Controls.Add(headerControls);

            // Container for stability
            Guna2Panel gridContainer = new Guna2Panel
            {
                Location = new Point(20, 75),
                Size = new Size(950, 600),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            grid = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                ThemeStyle = { AlternatingRowsStyle = { BackColor = Theme.SurfaceContainerLowest }, RowsStyle = { BackColor = Theme.Surface, ForeColor = Theme.OnSurface, SelectionBackColor = Theme.PrimaryContainer, SelectionForeColor = Color.White }, HeaderStyle = { BackColor = Theme.SurfaceContainerHigh, ForeColor = Theme.OnSurface, Font = Theme.BodyFont } },
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false,
                BackgroundColor = Theme.SurfaceContainerLowest,
                GridColor = Theme.SurfaceContainerHighest,
                ScrollBars = ScrollBars.Both,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Explicit Columns
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "ID", MinimumWidth = 60, FillWeight = 50 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Guest", HeaderText = "Guest Name", MinimumWidth = 180 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Room", HeaderText = "Room #", MinimumWidth = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "In", HeaderText = "Arrival", MinimumWidth = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Out", HeaderText = "Departure", MinimumWidth = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", MinimumWidth = 120 });

            gridContainer.Controls.Add(grid);
            this.Controls.Add(gridContainer);
        }

        private void LoadData()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(LoadData)); return; }
            
            var query = DataStore.Data.Reservations.Where(r => r.Status != "Cancelled" && r.Status != "Pending")
                .Join(DataStore.Data.Customers, r => r.CustomerId, c => c.Id, (r, c) => new { r, Guest = c.FullName })
                .Join(DataStore.Data.Rooms, combo => combo.r.RoomId, rm => rm.Id, (combo, rm) => new {
                    combo.r.Id, combo.Guest, Room = rm.RoomNumber, In = combo.r.CheckIn.ToShortDateString(), Out = combo.r.CheckOut.ToShortDateString(), combo.r.Status
                });

            if (!string.IsNullOrWhiteSpace(txtSearch.Text)) query = query.Where(q => q.Guest.ToLower().Contains(txtSearch.Text.ToLower()) || q.Room.Contains(txtSearch.Text));

            grid.DataSource = query.ToList();
        }

        private void ChangeStatus(string fromStatus, string toStatus)
        {
            if (grid.SelectedRows.Count > 0)
            {
                string id = grid.SelectedRows[0].Cells["Id"].Value.ToString();
                var res = DataStore.Data.Reservations.FirstOrDefault(r => r.Id == id);
                if (res != null)
                {
                    if (res.Status == fromStatus)
                    {
                        res.Status = toStatus;
                        
                        // Professional trigger: checking out dirties the room
                        if (toStatus == "CheckedOut")
                        {
                            var room = DataStore.Data.Rooms.FirstOrDefault(rm => rm.Id == res.RoomId);
                            if (room != null) room.CleanStatus = "Dirty";
                        }
                        
                        DataStore.Save();
                    }
                    else MessageBox.Show($"Reservation must specifically be '{fromStatus}' to change to '{toStatus}'.", "Invalid Action", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) EventBus.Instance.DataChanged -= LoadData;
            base.Dispose(disposing);
        }
    }
}
