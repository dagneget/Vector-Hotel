using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using HRS.Services;

namespace HRS.UI
{
    public class ReportsView : UserControl
    {
        private Guna2DataGridView grid;
        private Label lblTotal;

        public ReportsView()
        {
            this.BackColor = Theme.Surface;
            InitializeLayout();
            EventBus.Instance.DataChanged += LoadData;
            LoadData();
        }

        private void InitializeLayout()
        {
            Label lblTitle = new Label { Text = "Financial Reports", Font = Theme.HeadlineFont, ForeColor = Theme.OnSurface, AutoSize = true, Location = new Point(20, 20) };
            this.Controls.Add(lblTitle);

            lblTotal = new Label { Text = "Current Total Revenue: $0.00", Font = Theme.HeadlineFont, ForeColor = Theme.Primary, AutoSize = true, Location = new Point(20, 70), BackColor = Color.Transparent };
            this.Controls.Add(lblTotal);

            // Container for stability
            Guna2Panel gridContainer = new Guna2Panel
            {
                Location = new Point(20, 120),
                Size = new Size(940, 550),
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
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "Payment ID", MinimumWidth = 100, FillWeight = 50 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Guest", HeaderText = "Guest Name", MinimumWidth = 180 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Amount", HeaderText = "Revenue ($)", MinimumWidth = 120, DefaultCellStyle = { Format = "C2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Method", HeaderText = "Method", MinimumWidth = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Date", HeaderText = "Transaction Date", MinimumWidth = 120 });

            gridContainer.Controls.Add(grid);
            this.Controls.Add(gridContainer);
        }

        private void LoadData()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(LoadData)); return; }
            var displayList = DataStore.Data.Payments
                .Join(DataStore.Data.Reservations, p => p.ReservationId, r => r.Id, (p, r) => new { p, r })
                .Join(DataStore.Data.Customers, combined => combined.r.CustomerId, c => c.Id, (combined, c) => new {
                    combined.p.Id,
                    Guest = c.FullName,
                    combined.p.Amount,
                    Method = combined.p.Method,
                    Date = combined.p.Date.ToShortDateString()
                }).OrderByDescending(x => x.Date).ToList();

            grid.DataSource = displayList;
            lblTotal.Text = $"Total Revenue: ${displayList.Sum(p => p.Amount):0.00}";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) EventBus.Instance.DataChanged -= LoadData;
            base.Dispose(disposing);
        }
    }
}
