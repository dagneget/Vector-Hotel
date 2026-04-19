using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using HRS.Models;
using HRS.Services;

namespace HRS.UI
{
    public class ReservationsView : UserControl
    {
        private Guna2DataGridView grid;
        private Guna2TextBox txtSearch;

        public ReservationsView()
        {
            this.BackColor = Theme.Surface;
            InitializeLayout();
            EventBus.Instance.DataChanged += LoadData;
            LoadData();
        }

        private void InitializeLayout()
        {
            Label lblTitle = new Label { Text = "Reservation Ledger", Font = Theme.HeadlineFont, ForeColor = Theme.OnSurface, AutoSize = true, Location = new Point(20, 20) };
            this.Controls.Add(lblTitle);
            
            // Responsive Header with FlowLayout
            FlowLayoutPanel headerControls = new FlowLayoutPanel
            {
                Location = new Point(350, 15),
                Size = new Size(620, 50),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            txtSearch = new Guna2TextBox { PlaceholderText = "Search by Guest Name...", Font = Theme.BodyFont, Size = new Size(300, 40), FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, BorderColor = Theme.OutlineVariant, BorderRadius = 8, Margin = new Padding(0, 0, 10, 0) };
            txtSearch.TextChanged += (s, e) => LoadData();
            
            Guna2Button btnNew = new Guna2Button { Text = "+ New Reservation", Font = Theme.BodyFont, FillColor = Theme.PrimaryContainer, ForeColor = Color.White, BorderRadius = 8, Size = new Size(180, 40), Cursor = Cursors.Hand };
            btnNew.Click += (s, e) => new NewReservationModal().ShowDialog();

            headerControls.Controls.Add(txtSearch);
            headerControls.Controls.Add(btnNew);
            this.Controls.Add(headerControls);

            // Container for Grid stability
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
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Guest", HeaderText = "Guest Name", MinimumWidth = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RoomId", HeaderText = "Room #", MinimumWidth = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "In", HeaderText = "Arrival", MinimumWidth = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Out", HeaderText = "Departure", MinimumWidth = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Adults", HeaderText = "Adults", MinimumWidth = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Total", HeaderText = "Value ($)", MinimumWidth = 110, DefaultCellStyle = { Format = "C2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", MinimumWidth = 120 });

            grid.CellFormatting += Grid_CellFormatting;
            
            gridContainer.Controls.Add(grid);
            this.Controls.Add(gridContainer);
        }

        private void LoadData()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(LoadData)); return; }
            
            var query = DataStore.Data.Reservations.Join(DataStore.Data.Customers, r => r.CustomerId, c => c.Id, (r, c) => new {
                r.Id, Guest = c.FullName, r.RoomId, In = r.CheckIn.ToShortDateString(), Out = r.CheckOut.ToShortDateString(), Adults = r.AdultsCount, Total = r.TotalPrice, r.Status
            });

            if (!string.IsNullOrWhiteSpace(txtSearch.Text)) query = query.Where(q => q.Guest.ToLower().Contains(txtSearch.Text.ToLower()));

            grid.DataSource = query.ToList();
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (grid.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                string status = e.Value.ToString();
                if (status == "Confirmed" || status == "CheckedIn") e.CellStyle.BackColor = Theme.Tertiary;
                else if (status == "Pending") e.CellStyle.BackColor = Theme.OrangeAccent;
                else if (status == "Cancelled") e.CellStyle.BackColor = Color.LightCoral;
                e.CellStyle.ForeColor = Color.Black;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) EventBus.Instance.DataChanged -= LoadData;
            base.Dispose(disposing);
        }
    }
    
    public class NewReservationModal : Form
    {
        private Guna2ComboBox cbGuest;
        private Guna2ComboBox cbRoom;
        private Guna2DateTimePicker dtIn;
        private Guna2DateTimePicker dtOut;
        private Guna2NumericUpDown numAdults;
        private Guna2TextBox txtRequests;

        private readonly string _preSelectedCustomerId;

        public NewReservationModal(string preSelectedCustomerId = null)
        {
            _preSelectedCustomerId = preSelectedCustomerId;
            this.Text = "New Reservation";
            this.Size = new Size(420, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Theme.SurfaceContainerHigh;
            Guna2DragControl dc = new Guna2DragControl { TargetControl = this };

            Label lblTitle = new Label { Text = "Book Reservation", Font = Theme.HeadlineFont, ForeColor = Theme.OnSurface, AutoSize = true, Location = new Point(20, 20) };
            this.Controls.Add(lblTitle);
            
            Label lblGuest = new Label { Text = "Guest Account", Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, Location = new Point(20, 70), AutoSize = true };
            cbGuest = new Guna2ComboBox { Location = new Point(20, 95), Size = new Size(380, 36), BorderRadius = 8, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface };
            cbGuest.DataSource = DataStore.Data.Customers.Select(c => new {
                c.Id,
                Label = c.FullName + " (" + (!string.IsNullOrWhiteSpace(c.IdNumber) ? c.IdNumber : c.PassportNumber) + ")"
            }).ToList();
            cbGuest.DisplayMember = "Label"; cbGuest.ValueMember = "Id";
            // Pre-select customer when opened from the Customers section
            if (!string.IsNullOrEmpty(_preSelectedCustomerId))
                cbGuest.SelectedValue = _preSelectedCustomerId;

            Label lblIn = new Label { Text = "Check-In Date", Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, Location = new Point(20, 145), AutoSize = true };
            dtIn = new Guna2DateTimePicker { Location = new Point(20, 170), Size = new Size(180, 36), BorderRadius = 8, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface };
            dtIn.ValueChanged += Dts_ValueChanged;

            Label lblOut = new Label { Text = "Check-Out Date", Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, Location = new Point(220, 145), AutoSize = true };
            dtOut = new Guna2DateTimePicker { Location = new Point(220, 170), Size = new Size(180, 36), BorderRadius = 8, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, Value = DateTime.Now.AddDays(1) };
            dtOut.ValueChanged += Dts_ValueChanged;

            Label lblRoom = new Label { Text = "Available Rooms (Calculated)", Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, Location = new Point(20, 220), AutoSize = true };
            cbRoom = new Guna2ComboBox { Location = new Point(20, 245), Size = new Size(380, 36), BorderRadius = 8, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface };

            Label lblAdults = new Label { Text = "Adults Count", Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, Location = new Point(20, 295), AutoSize = true };
            numAdults = new Guna2NumericUpDown { Location = new Point(20, 320), Size = new Size(120, 36), BorderRadius = 8, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, Minimum = 1, Value = 1 };

            Label lblReq = new Label { Text = "Special Requests / Notes", Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, Location = new Point(20, 370), AutoSize = true };
            txtRequests = new Guna2TextBox { Location = new Point(20, 395), Size = new Size(380, 80), BorderRadius = 8, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, Multiline = true };

            Guna2GradientButton btnSave = new Guna2GradientButton { Text = "CREATE HOLD", Font = Theme.BodyFont, Location = new Point(20, 520), Size = new Size(240, 45), BorderRadius = 8, FillColor = Theme.Primary, FillColor2 = Theme.PrimaryContainer, ForeColor = Theme.OnPrimary, Cursor = Cursors.Hand };
            btnSave.Click += SaveReservation;

            Guna2Button btnCancel = new Guna2Button { Text = "CANCEL", Font = Theme.BodyFont, Location = new Point(280, 520), Size = new Size(120, 45), BorderRadius = 8, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, Cursor = Cursors.Hand };
            btnCancel.Click += (s,e) => this.Close();

            this.Controls.Add(lblGuest); this.Controls.Add(cbGuest);
            this.Controls.Add(lblIn); this.Controls.Add(dtIn);
            this.Controls.Add(lblOut); this.Controls.Add(dtOut);
            this.Controls.Add(lblRoom); this.Controls.Add(cbRoom);
            this.Controls.Add(lblAdults); this.Controls.Add(numAdults);
            this.Controls.Add(lblReq); this.Controls.Add(txtRequests);
            this.Controls.Add(btnSave); this.Controls.Add(btnCancel);

            UpdateAvailableRooms();
        }

        private void Dts_ValueChanged(object sender, EventArgs e) => UpdateAvailableRooms();

        private void UpdateAvailableRooms()
        {
            DateTime checkIn = dtIn.Value.Date;
            DateTime checkOut = dtOut.Value.Date;

            // Proper advanced conflict detection
            var conflictingReservations = DataStore.Data.Reservations.Where(r => 
                r.Status != "Cancelled" && r.Status != "CheckedOut" &&
                (checkIn < r.CheckOut && checkOut > r.CheckIn) // Overlap logic
            ).Select(r => r.RoomId).Distinct().ToList();

            var availableRooms = DataStore.Data.Rooms
                .Where(r => r.Status != "OutOfOrder" && !conflictingReservations.Contains(r.Id))
                .Join(DataStore.Data.RoomTypes, r => r.TypeId, t => t.Id, (r,t) => new { r.Id, Label = $"Rm {r.RoomNumber} ({t.Name} - ${t.BasePrice})" })
                .ToList();

            cbRoom.DataSource = availableRooms;
            cbRoom.DisplayMember = "Label";
            cbRoom.ValueMember = "Id";
        }

        private void SaveReservation(object sender, EventArgs e)
        {
            if (cbGuest.SelectedValue == null || cbRoom.SelectedValue == null) { MessageBox.Show("Please select a guest and a room."); return; }
            if (dtIn.Value.Date >= dtOut.Value.Date) { MessageBox.Show("CheckOut must be gracefully scheduled after CheckIn."); return; }

            string roomId = cbRoom.SelectedValue.ToString();
            var room = DataStore.Data.Rooms.FirstOrDefault(r => r.Id == roomId);
            var roomType = DataStore.Data.RoomTypes.FirstOrDefault(t => t.Id == room?.TypeId);
            decimal price = (roomType?.BasePrice ?? 0) * (decimal)(dtOut.Value.Date - dtIn.Value.Date).TotalDays;

            DataStore.Data.Reservations.Add(new ReservationModel
            {
                Id = DataStore.GenerateId(), CustomerId = cbGuest.SelectedValue.ToString(), RoomId = roomId,
                CheckIn = dtIn.Value.Date, CheckOut = dtOut.Value.Date, AdultsCount = (int)numAdults.Value,
                SpecialRequests = txtRequests.Text, TotalPrice = price, Status = "Pending"
            });
            DataStore.Save();
            this.Close();
        }
    }
}
