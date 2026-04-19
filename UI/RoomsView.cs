using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using HRS.Models;
using HRS.Services;

namespace HRS.UI
{
    public class RoomsView : UserControl
    {
        private Guna2DataGridView grid;
        private Guna2TextBox txtSearch;
        private Guna2Panel rightPanel;
        private Guna2TextBox txtRoomNo;
        private Guna2TextBox txtFloor;
        private Guna2ComboBox cbType;
        private Guna2ComboBox cbStatus;
        private string editingId = null;

        public RoomsView()
        {
            this.BackColor = Theme.Surface;
            InitializeLayout();
            EventBus.Instance.DataChanged += LoadData;
            LoadData();
        }

        private void InitializeLayout()
        {
            Label lblTitle = new Label { Text = "Room Management Fleet", Font = Theme.HeadlineFont, ForeColor = Theme.OnSurface, AutoSize = true, Location = new Point(20, 20) };
            this.Controls.Add(lblTitle);

            // Responsive Header
            FlowLayoutPanel headerControls = new FlowLayoutPanel { Location = new Point(300, 15), Size = new Size(660, 50), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent };

            txtSearch = new Guna2TextBox { PlaceholderText = "Search Room #...", Font = Theme.BodyFont, Size = new Size(250, 40), FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, BorderColor = Theme.OutlineVariant, BorderRadius = 8, Margin = new Padding(0, 0, 10, 0) };
            txtSearch.TextChanged += (s, e) => LoadData();
            
            Guna2Button btnAdd = new Guna2Button { Text = "+ Register Room", Font = Theme.BodyFont, FillColor = Theme.PrimaryContainer, ForeColor = Color.White, BorderRadius = 8, Size = new Size(150, 40), Margin = new Padding(0, 0, 10, 0), Cursor = Cursors.Hand };
            btnAdd.Click += (s, e) => OpenForm(null);

            Guna2Button btnDelete = new Guna2Button { Text = "- Delete", Font = Theme.BodyFont, FillColor = Color.DarkRed, ForeColor = Color.White, BorderRadius = 8, Size = new Size(130, 40), Cursor = Cursors.Hand };
            btnDelete.Click += DeleteSelectedRoom;

            headerControls.Controls.Add(txtSearch);
            headerControls.Controls.Add(btnAdd);
            headerControls.Controls.Add(btnDelete);
            this.Controls.Add(headerControls);

            // Container for Grid to protect ScrollBars from clipping
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

            // Explicit Column Setup for Guaranteed Visibility
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "ID", MinimumWidth = 60, FillWeight = 50 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RoomNumber", HeaderText = "Room #", MinimumWidth = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Floor", HeaderText = "Floor", MinimumWidth = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Tier", HeaderText = "Category", MinimumWidth = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "BasePrice", HeaderText = "Rate ($)", MinimumWidth = 100, DefaultCellStyle = { Format = "C2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Housekeeping", HeaderText = "Condition", MinimumWidth = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Current State", MinimumWidth = 120 });

            grid.DoubleClick += (s, e) => {
                if (grid.SelectedRows.Count > 0) OpenForm(grid.SelectedRows[0].Cells["Id"].Value.ToString());
            };
            grid.CellFormatting += Grid_CellFormatting;
            
            gridContainer.Controls.Add(grid);
            this.Controls.Add(gridContainer);
            
            BuildRightPanel();
        }

        private void BuildRightPanel()
        {
            rightPanel = new Guna2Panel { Width = 320, Dock = DockStyle.Right, FillColor = Theme.SurfaceContainerHigh, Visible = false, ShadowDecoration = { Enabled = true, Depth = 30 } };
            Label lblFormTitle = new Label { Text = "Room Specifics", Font = Theme.HeadlineFont, ForeColor = Theme.OnSurface, AutoSize = true, Location = new Point(20, 20), BackColor = Color.Transparent };
            rightPanel.Controls.Add(lblFormTitle);

            txtRoomNo = CreateInput(rightPanel, "Room Code/Number", 80);
            txtFloor = CreateInput(rightPanel, "Floor Level", 160);

            Label lblType = new Label { Text = "Room Tier", Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, AutoSize = true, Location = new Point(20, 240), BackColor = Color.Transparent };
            cbType = new Guna2ComboBox { Location = new Point(20, 265), Size = new Size(280, 40), BorderRadius = 8, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, BorderColor = Theme.OutlineVariant };
            
            Label lblStatus = new Label { Text = "Housekeeping State", Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, AutoSize = true, Location = new Point(20, 320), BackColor = Color.Transparent };
            cbStatus = new Guna2ComboBox { Location = new Point(20, 345), Size = new Size(280, 40), BorderRadius = 8, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, BorderColor = Theme.OutlineVariant, DataSource = new string[] { "Clean", "Dirty", "Maintenance" } };

            rightPanel.Controls.Add(lblType); rightPanel.Controls.Add(cbType);
            rightPanel.Controls.Add(lblStatus); rightPanel.Controls.Add(cbStatus);

            Guna2Button btnSave = new Guna2Button { Text = "Save", Font = Theme.BodyFont, FillColor = Theme.Primary, ForeColor = Theme.OnPrimary, BorderRadius = 8, Size = new Size(130, 40), Location = new Point(20, 410), Cursor = Cursors.Hand };
            btnSave.Click += SaveRoom;
            rightPanel.Controls.Add(btnSave);

            Guna2Button btnCancel = new Guna2Button { Text = "Cancel", Font = Theme.BodyFont, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, BorderRadius = 8, Size = new Size(130, 40), Location = new Point(170, 410), Cursor = Cursors.Hand };
            btnCancel.Click += (s, e) => rightPanel.Visible = false;
            rightPanel.Controls.Add(btnCancel);

            this.Controls.Add(rightPanel);
            rightPanel.BringToFront();
        }

        private Guna2TextBox CreateInput(Guna2Panel parent, string label, int y)
        {
            Label lbl = new Label { Text = label, Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, AutoSize = true, Location = new Point(20, y), BackColor = Color.Transparent };
            Guna2TextBox txt = new Guna2TextBox { Font = Theme.BodyFont, Size = new Size(280, 40), Location = new Point(20, y + 25), FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, BorderColor = Theme.OutlineVariant, BorderRadius = 8 };
            parent.Controls.Add(lbl); parent.Controls.Add(txt);
            return txt;
        }

        private void LoadData()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(LoadData)); return; }
            var query = DataStore.Data.Rooms.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(txtSearch.Text)) query = query.Where(r => r.RoomNumber.Contains(txtSearch.Text));
            
            var displayList = query.Join(DataStore.Data.RoomTypes, r => r.TypeId, t => t.Id, (r, t) => new {
                r.Id, r.RoomNumber, Floor = r.FloorNumber, Tier = t.Name, BasePrice = t.BasePrice, Housekeeping = r.CleanStatus, r.Status
            }).ToList();
            
            grid.DataSource = displayList;
            
            cbType.DataSource = DataStore.Data.RoomTypes.ToList();
            cbType.DisplayMember = "Name"; cbType.ValueMember = "Id";
        }

        private void OpenForm(string id)
        {
            editingId = id;
            if (id == null) { txtRoomNo.Text = ""; txtFloor.Text = "1"; cbStatus.SelectedIndex = 0; }
            else
            {
                var room = DataStore.Data.Rooms.FirstOrDefault(r => r.Id == id);
                if (room != null) { txtRoomNo.Text = room.RoomNumber; txtFloor.Text = room.FloorNumber.ToString(); cbType.SelectedValue = room.TypeId; cbStatus.SelectedItem = room.CleanStatus; }
            }
            rightPanel.Visible = true;
        }

        private void SaveRoom(object sender, EventArgs e)
        {
            int.TryParse(txtFloor.Text, out int floor);
            string tid = cbType.SelectedValue?.ToString();

            if (editingId == null) DataStore.Data.Rooms.Add(new RoomModel { Id = DataStore.GenerateId(), RoomNumber = txtRoomNo.Text, FloorNumber = floor, TypeId = tid, CleanStatus = cbStatus.SelectedItem.ToString(), Status = "Available" });
            else
            {
                var room = DataStore.Data.Rooms.FirstOrDefault(r => r.Id == editingId);
                if (room != null) { room.RoomNumber = txtRoomNo.Text; room.FloorNumber = floor; room.TypeId = tid; room.CleanStatus = cbStatus.SelectedItem.ToString(); }
            }
            DataStore.Save();
            rightPanel.Visible = false;
        }

        private void DeleteSelectedRoom(object sender, EventArgs e)
        {
            if (!AuthService.CanDeleteRoom()) { MessageBox.Show("Access Denied: Receptionist cannot delete rooms."); return; }
            if (grid.SelectedRows.Count > 0)
            {
                string id = grid.SelectedRows[0].Cells["Id"].Value.ToString();
                var room = DataStore.Data.Rooms.FirstOrDefault(r => r.Id == id);
                if (room != null) { DataStore.Data.Rooms.Remove(room); DataStore.Save(); }
            }
        }
        
        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (grid.Columns[e.ColumnIndex].Name == "Housekeeping" && e.Value != null)
            {
                string hs = e.Value.ToString();
                if (hs == "Dirty") e.CellStyle.ForeColor = Color.LightCoral;
                if (hs == "Maintenance") e.CellStyle.ForeColor = Theme.OrangeAccent;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) EventBus.Instance.DataChanged -= LoadData;
            base.Dispose(disposing);
        }
    }
}
