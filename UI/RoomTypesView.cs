using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using HRS.Models;
using HRS.Services;

namespace HRS.UI
{
    public class RoomTypesView : UserControl
    {
        private Guna2DataGridView grid;
        private Guna2Panel rightPanel;
        private Guna2TextBox txtName;
        private Guna2TextBox txtPrice;
        private string editingId = null;

        public RoomTypesView()
        {
            this.BackColor = Theme.Surface;
            InitializeLayout();
            EventBus.Instance.DataChanged += LoadData;
            LoadData();
        }

        private void InitializeLayout()
        {
            Label lblTitle = new Label { Text = "Manage Room Types", Font = Theme.HeadlineFont, ForeColor = Theme.OnSurface, AutoSize = true, Location = new Point(20, 20) };
            this.Controls.Add(lblTitle);

            // Responsive Header
            FlowLayoutPanel headerControls = new FlowLayoutPanel { Location = new Point(400, 15), Size = new Size(560, 50), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent };

            Guna2Button btnAdd = new Guna2Button { Text = "+ Add Room Type", Font = Theme.BodyFont, FillColor = Theme.PrimaryContainer, ForeColor = Color.White, BorderRadius = 8, Size = new Size(180, 40), Cursor = Cursors.Hand };
            btnAdd.Click += (s, e) => OpenForm(null);

            headerControls.Controls.Add(btnAdd);
            this.Controls.Add(headerControls);

            grid = new Guna2DataGridView
            {
                Location = new Point(20, 75),
                Size = new Size(940, 600),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ThemeStyle = { AlternatingRowsStyle = { BackColor = Theme.SurfaceContainerLowest }, RowsStyle = { BackColor = Theme.Surface, ForeColor = Theme.OnSurface, SelectionBackColor = Theme.PrimaryContainer, SelectionForeColor = Color.White }, HeaderStyle = { BackColor = Theme.SurfaceContainerHigh, ForeColor = Theme.OnSurface, Font = Theme.BodyFont } },
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                BackgroundColor = Theme.SurfaceContainerLowest,
                GridColor = Theme.SurfaceContainerHighest,
                ScrollBars = ScrollBars.Both,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            grid.DataBindingComplete += (s, e) => {
                foreach (DataGridViewColumn col in grid.Columns) col.MinimumWidth = 150;
            };
            grid.DoubleClick += (s, e) => {
                if (grid.SelectedRows.Count > 0) OpenForm(grid.SelectedRows[0].Cells["Id"].Value.ToString());
            };
            this.Controls.Add(grid);

            // Right Panel Form
            rightPanel = new Guna2Panel { Width = 300, Dock = DockStyle.Right, FillColor = Theme.SurfaceContainerHigh, Visible = false, ShadowDecoration = { Enabled = true, Depth = 30 } };
            Label lblFormTitle = new Label { Text = "Type Details", Font = Theme.HeadlineFont, ForeColor = Theme.OnSurface, AutoSize = true, Location = new Point(20, 20), BackColor = Color.Transparent };
            rightPanel.Controls.Add(lblFormTitle);

            Label lblName = new Label { Text = "Designation Rank", Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, AutoSize = true, Location = new Point(20, 80), BackColor = Color.Transparent };
            txtName = new Guna2TextBox { Font = Theme.BodyFont, Size = new Size(260, 40), Location = new Point(20, 105), FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, BorderColor = Theme.OutlineVariant, BorderRadius = 8 };
            
            Label lblPrice = new Label { Text = "Base Price Per Night ($)", Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, AutoSize = true, Location = new Point(20, 160), BackColor = Color.Transparent };
            txtPrice = new Guna2TextBox { Font = Theme.BodyFont, Size = new Size(260, 40), Location = new Point(20, 185), FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, BorderColor = Theme.OutlineVariant, BorderRadius = 8 };

            Guna2Button btnSave = new Guna2Button { Text = "Save", Font = Theme.BodyFont, FillColor = Theme.Primary, ForeColor = Theme.OnPrimary, BorderRadius = 8, Size = new Size(120, 40), Location = new Point(20, 250), Cursor = Cursors.Hand };
            btnSave.Click += SaveType;
            
            Guna2Button btnCancel = new Guna2Button { Text = "Cancel", Font = Theme.BodyFont, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, BorderRadius = 8, Size = new Size(120, 40), Location = new Point(160, 250), Cursor = Cursors.Hand };
            btnCancel.Click += (s, e) => rightPanel.Visible = false;

            rightPanel.Controls.Add(lblName); rightPanel.Controls.Add(txtName);
            rightPanel.Controls.Add(lblPrice); rightPanel.Controls.Add(txtPrice);
            rightPanel.Controls.Add(btnSave); rightPanel.Controls.Add(btnCancel);
            this.Controls.Add(rightPanel);
            rightPanel.BringToFront();
        }

        private void LoadData()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(LoadData)); return; }
            grid.DataSource = DataStore.Data.RoomTypes.ToList();
        }

        private void OpenForm(string id)
        {
            if (!AuthService.CanDeleteRoom()) { MessageBox.Show("Administrators Only."); return; }

            editingId = id;
            if (id == null) { txtName.Text = ""; txtPrice.Text = "0.00"; }
            else
            {
                var rt = DataStore.Data.RoomTypes.FirstOrDefault(r => r.Id == id);
                if (rt != null) { txtName.Text = rt.Name; txtPrice.Text = rt.BasePrice.ToString(); }
            }
            rightPanel.Visible = true;
        }

        private void SaveType(object sender, EventArgs e)
        {
            if (decimal.TryParse(txtPrice.Text, out decimal price))
            {
                if (editingId == null) DataStore.Data.RoomTypes.Add(new RoomTypeModel { Id = DataStore.GenerateId(), Name = txtName.Text, BasePrice = price });
                else
                {
                    var rt = DataStore.Data.RoomTypes.FirstOrDefault(r => r.Id == editingId);
                    if (rt != null) { rt.Name = txtName.Text; rt.BasePrice = price; }
                }
                DataStore.Save();
                rightPanel.Visible = false;
            }
            else MessageBox.Show("Invalid price format.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) EventBus.Instance.DataChanged -= LoadData;
            base.Dispose(disposing);
        }
    }
}
