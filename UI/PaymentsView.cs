using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using HRS.Models;
using HRS.Services;

namespace HRS.UI
{
    public class PaymentsView : UserControl
    {
        private Guna2DataGridView grid;
        private Guna2TextBox txtSearch;

        public PaymentsView()
        {
            this.BackColor = Theme.Surface;
            InitializeLayout();
            EventBus.Instance.DataChanged += LoadData;
            LoadData();
        }

        private void InitializeLayout()
        {
            Label lblTitle = new Label { Text = "Payment Processing", Font = Theme.HeadlineFont, ForeColor = Theme.OnSurface, AutoSize = true, Location = new Point(20, 20) };
            this.Controls.Add(lblTitle);

            // Responsive Header
            FlowLayoutPanel headerControls = new FlowLayoutPanel { Location = new Point(250, 15), Size = new Size(710, 50), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent };

            txtSearch = new Guna2TextBox { PlaceholderText = "Search by Guest Name...", Font = Theme.BodyFont, Size = new Size(250, 40), FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, BorderColor = Theme.OutlineVariant, BorderRadius = 8, Margin = new Padding(0, 0, 10, 0) };
            txtSearch.TextChanged += (s, e) => LoadData();
            
            Guna2Button btnPayCash = new Guna2Button { Text = "Pay by Cash", Font = Theme.BodyFont, FillColor = Theme.PrimaryContainer, ForeColor = Color.White, BorderRadius = 8, Size = new Size(130, 40), Margin = new Padding(0, 0, 10, 0), Cursor = Cursors.Hand };
            btnPayCash.Click += (s, e) => ProcessPayment("Cash");

            Guna2Button btnPayCard = new Guna2Button { Text = "Pay by Card", Font = Theme.BodyFont, FillColor = Theme.SecondaryContainer, ForeColor = Theme.OnSecondaryContainer, BorderRadius = 8, Size = new Size(130, 40), Cursor = Cursors.Hand };
            btnPayCard.Click += (s, e) => ProcessPayment("Credit Card");

            headerControls.Controls.Add(txtSearch);
            headerControls.Controls.Add(btnPayCash);
            headerControls.Controls.Add(btnPayCard);
            this.Controls.Add(headerControls);

            // Container for stability
            Guna2Panel gridContainer = new Guna2Panel
            {
                Location = new Point(20, 75),
                Size = new Size(940, 600),
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
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Amount", HeaderText = "Amount ($)", MinimumWidth = 100, DefaultCellStyle = { Format = "C2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Method", HeaderText = "Method", MinimumWidth = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Date", HeaderText = "Date", MinimumWidth = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", MinimumWidth = 120 });

            gridContainer.Controls.Add(grid);
            this.Controls.Add(gridContainer);
        }

        private void LoadData()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(LoadData)); return; }
            
            var query = DataStore.Data.Reservations.Where(r => r.Status == "Pending")
                .Join(DataStore.Data.Customers, r => r.CustomerId, c => c.Id, (r, c) => new { 
                    r.Id, Guest = c.FullName, r.RoomId, Date = r.CheckIn.ToShortDateString(), Amount = r.TotalPrice, r.Status 
                });

            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                query = query.Where(q => q.Guest.ToLower().Contains(txtSearch.Text.ToLower()));
            }

            grid.DataSource = query.ToList();
        }

        private void ProcessPayment(string method)
        {
            if (grid.SelectedRows.Count > 0)
            {
                string id = grid.SelectedRows[0].Cells["Id"].Value.ToString();
                var res = DataStore.Data.Reservations.FirstOrDefault(r => r.Id == id);
                if (res != null)
                {
                    DataStore.Data.Payments.Add(new PaymentModel
                    {
                        Id = DataStore.GenerateId(),
                        ReservationId = res.Id,
                        Amount = res.TotalPrice,
                        Date = DateTime.Now,
                        Method = method
                    });
                    
                    res.Status = "Confirmed";
                    DataStore.Save();
                    MessageBox.Show($"Payment of ${res.TotalPrice:0.00} recorded via {method}!");
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
