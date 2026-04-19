using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using HRS.Models;
using HRS.Services;

namespace HRS.UI
{
    public class CustomersView : UserControl
    {
        #region Fields

        // State
        private string _editingId;
        private bool _filterVisible;
        private bool _isLoading;

        // Search / Filter
        private Guna2TextBox txtSearch;
        private Panel filterBar;
        private Guna2ComboBox cbFNationality, cbFGender, cbFType, cbFStatus;

        // Grid
        private Guna2Panel gridContainer;
        private Guna2DataGridView grid;

        // Right Panel shell
        private Guna2Panel rightPanel;
        private Label lblPanelName;
        private Label lblPanelBadge;
        private TabControl tabControl;

        // ── Profile Tab
        private Guna2TextBox txtName, txtPhone, txtEmail, txtNationality;
        private Guna2TextBox txtAddress, txtOccupation, txtCompany;
        private Guna2ComboBox cbGender;
        private CheckBox chkDob;
        private Guna2DateTimePicker dtDob;

        // ── Identity Tab
        private Guna2ComboBox cbIdType;
        private Guna2TextBox txtIdNumber;
        private CheckBox chkIdExpiry;
        private Guna2DateTimePicker dtIdExpiry;
        private Label lblIdWarn;

        // ── Notes Tab
        private Guna2ComboBox cbCustomerType, cbCustStatus;
        private Guna2TextBox txtEmergencyName, txtEmergencyPhone, txtNotes, txtBlacklistReason;
        private CheckBox chkBlacklisted;
        private Panel pnlBlacklist;

        // ── Preferences Tab
        private Guna2ComboBox cbPrefRoomType, cbSmoking, cbFloor, cbBedType;

        // ── History Tab
        private Guna2DataGridView gridResHistory, gridPayHistory;

        // ── Analytics Tab
        private Label lblAStays, lblANights, lblASpent, lblAAvg;
        private Label lblARoomType, lblATier, lblAPoints, lblALastVisit;

        // Action Buttons
        private Guna2Button btnSave, btnCancel, btnDelete, btnNewRes;

        #endregion

        // ═══════════════════════════════════════════════════════════════════════

        public CustomersView()
        {
            this.BackColor = Theme.Surface;
            InitializeLayout();
            EventBus.Instance.DataChanged += OnDataChanged;
            LoadData();
        }

        private void OnDataChanged()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(OnDataChanged)); return; }
            LoadData();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  LAYOUT BUILDERS
        // ═══════════════════════════════════════════════════════════════════════

        private void InitializeLayout()
        {
            BuildHeader();
            BuildFilterBar();
            BuildGrid();
            BuildRightPanel();
        }

        private void BuildHeader()
        {
            this.Controls.Add(new Label
            {
                Text = "Customers Profile Directory",
                Font = Theme.HeadlineFont,
                ForeColor = Theme.OnSurface,
                AutoSize = true,
                Location = new Point(20, 20),
                BackColor = Color.Transparent
            });

            var hdr = new FlowLayoutPanel
            {
                Location = new Point(330, 10),
                Size = new Size(660, 52),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            txtSearch = new Guna2TextBox
            {
                PlaceholderText = "Search by Name, Phone, Email, ID Number...",
                Font = Theme.BodyFont,
                Size = new Size(300, 40),
                FillColor = Theme.SurfaceContainerLowest,
                ForeColor = Theme.OnSurface,
                BorderColor = Theme.OutlineVariant,
                BorderRadius = 8,
                Margin = new Padding(0, 6, 8, 0)
            };
            txtSearch.TextChanged += (s, e) => LoadData();

            var btnFilter = new Guna2Button
            {
                Text = "⚙ Filters",
                Font = Theme.BodyFont,
                FillColor = Theme.SurfaceContainerHigh,
                ForeColor = Theme.OnSurface,
                BorderRadius = 8,
                Size = new Size(105, 40),
                Margin = new Padding(0, 6, 8, 0),
                Cursor = Cursors.Hand
            };
            btnFilter.Click += (s, e) => ToggleFilterBar();

            var btnAdd = new Guna2Button
            {
                Text = "+ Add Customer",
                Font = Theme.BodyFont,
                FillColor = Theme.PrimaryContainer,
                ForeColor = Color.White,
                BorderRadius = 8,
                Size = new Size(155, 40),
                Margin = new Padding(0, 6, 0, 0),
                Cursor = Cursors.Hand
            };
            btnAdd.Click += (s, e) => OpenForm(null);

            hdr.Controls.Add(txtSearch);
            hdr.Controls.Add(btnFilter);
            hdr.Controls.Add(btnAdd);
            this.Controls.Add(hdr);
        }

        private void BuildFilterBar()
        {
            filterBar = new Panel
            {
                Location = new Point(20, 65),
                Size = new Size(960, 52),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Theme.SurfaceContainerHigh,
                Visible = false
            };

            int x = 10;

            filterBar.Controls.Add(FLbl("Nationality", x));
            cbFNationality = FCombo(x, 26, 148); filterBar.Controls.Add(cbFNationality);
            cbFNationality.SelectedIndexChanged += (s, e) => LoadData();
            x += 162;

            filterBar.Controls.Add(FLbl("Gender", x));
            cbFGender = FCombo(x, 26, 118); filterBar.Controls.Add(cbFGender);
            cbFGender.Items.AddRange(new object[] { "All", "Male", "Female", "Other", "Prefer not to say" });
            cbFGender.SelectedIndex = 0;
            cbFGender.SelectedIndexChanged += (s, e) => LoadData();
            x += 132;

            filterBar.Controls.Add(FLbl("Type", x));
            cbFType = FCombo(x, 26, 138); filterBar.Controls.Add(cbFType);
            cbFType.Items.AddRange(new object[] { "All", "Regular", "VIP", "Corporate", "Blacklisted" });
            cbFType.SelectedIndex = 0;
            cbFType.SelectedIndexChanged += (s, e) => LoadData();
            x += 152;

            filterBar.Controls.Add(FLbl("Status", x));
            cbFStatus = FCombo(x, 26, 108); filterBar.Controls.Add(cbFStatus);
            cbFStatus.Items.AddRange(new object[] { "All", "Active", "Inactive" });
            cbFStatus.SelectedIndex = 0;
            cbFStatus.SelectedIndexChanged += (s, e) => LoadData();
            x += 122;

            var btnClear = new Guna2Button
            {
                Text = "Clear",
                Font = Theme.LabelFont,
                FillColor = Theme.SurfaceContainerLowest,
                ForeColor = Theme.OnSurface,
                BorderRadius = 6,
                Size = new Size(70, 26),
                Location = new Point(x, 13),
                Cursor = Cursors.Hand
            };
            btnClear.Click += (s, e) =>
            {
                if (cbFNationality.SelectedIndex > 0) cbFNationality.SelectedIndex = 0;
                cbFGender.SelectedIndex = 0;
                cbFType.SelectedIndex = 0;
                cbFStatus.SelectedIndex = 0;
            };
            filterBar.Controls.Add(btnClear);

            this.Controls.Add(filterBar);
        }

        private Label FLbl(string text, int x) =>
            new Label { Text = text, Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, Location = new Point(x, 7), AutoSize = true, BackColor = Color.Transparent };

        private Guna2ComboBox FCombo(int x, int y, int w) =>
            new Guna2ComboBox { Location = new Point(x, y), Size = new Size(w, 24), BorderRadius = 5, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, Font = Theme.LabelFont, DropDownStyle = ComboBoxStyle.DropDownList };

        private void ToggleFilterBar()
        {
            _filterVisible = !_filterVisible;
            filterBar.Visible = _filterVisible;
            int top = _filterVisible ? 122 : 68;
            gridContainer.Location = new Point(gridContainer.Location.X, top);
            int h = this.ClientSize.Height - top - 10;
            gridContainer.Height = h > 100 ? h : 400;
        }

        private void BuildGrid()
        {
            gridContainer = new Guna2Panel
            {
                Location = new Point(20, 68),
                Size = new Size(520, 600),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            grid = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                ThemeStyle =
                {
                    AlternatingRowsStyle = { BackColor = Theme.SurfaceContainerLowest },
                    RowsStyle = { BackColor = Theme.Surface, ForeColor = Theme.OnSurface, SelectionBackColor = Theme.PrimaryContainer, SelectionForeColor = Color.White },
                    HeaderStyle = { BackColor = Theme.SurfaceContainerHigh, ForeColor = Theme.OnSurface, Font = Theme.BodyFont }
                },
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false,
                BackgroundColor = Theme.SurfaceContainerLowest,
                GridColor = Theme.SurfaceContainerHighest,
                ScrollBars = ScrollBars.Both,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColId",     DataPropertyName = "Id",           HeaderText = "ID",          Visible = false,  FillWeight = 30 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColName",   DataPropertyName = "FullName",     HeaderText = "Full Name",   MinimumWidth = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColType",   DataPropertyName = "DisplayType",  HeaderText = "Type",        MinimumWidth = 100, FillWeight = 60 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPhone",  DataPropertyName = "Phone",        HeaderText = "Phone",       MinimumWidth = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColEmail",  DataPropertyName = "Email",        HeaderText = "Email",       MinimumWidth = 160 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColNat",    DataPropertyName = "Nationality",  HeaderText = "Nationality", MinimumWidth = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColTier",   DataPropertyName = "LoyaltyTier",  HeaderText = "Tier",        MinimumWidth = 80,  FillWeight = 50 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColStatus", DataPropertyName = "Status",       HeaderText = "Status",      MinimumWidth = 90,  FillWeight = 50 });

            grid.CellFormatting += Grid_CellFormatting;
            grid.SelectionChanged += Grid_SelectionChanged;

            gridContainer.Controls.Add(grid);
            this.Controls.Add(gridContainer);
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  RIGHT PANEL
        // ═══════════════════════════════════════════════════════════════════════

        private void BuildRightPanel()
        {
            rightPanel = new Guna2Panel
            {
                Width = 480,
                Dock = DockStyle.Right,
                FillColor = Theme.SurfaceContainerHigh,
                Visible = false
            };

            // ── Top bar
            var topBar = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = Color.Transparent };
            lblPanelName  = new Label { Text = "New Customer",   Font = Theme.HeadlineFont, ForeColor = Theme.OnSurface,        AutoSize = true, Location = new Point(15, 10), BackColor = Color.Transparent };
            lblPanelBadge = new Label { Text = "",               Font = Theme.LabelFont,    ForeColor = Theme.OnSurfaceVariant, AutoSize = true, Location = new Point(15, 44), BackColor = Color.Transparent };
            topBar.Controls.Add(lblPanelName);
            topBar.Controls.Add(lblPanelBadge);
            rightPanel.Controls.Add(topBar);

            // ── Bottom action bar
            var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 112, BackColor = Color.Transparent };

            btnNewRes = new Guna2Button
            {
                Text = "⊞ New Reservation",
                Font = Theme.LabelFont,
                FillColor = Color.Transparent,
                ForeColor = Theme.Tertiary,
                BorderRadius = 7,
                Size = new Size(200, 32),
                Location = new Point(15, 6),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnNewRes.Click += BtnNewRes_Click;

            btnSave = new Guna2Button
            {
                Text = "Save",
                Font = Theme.BodyFont,
                FillColor = Theme.PrimaryContainer,
                ForeColor = Color.White,
                BorderRadius = 8,
                Size = new Size(130, 44),
                Location = new Point(15, 50),
                Cursor = Cursors.Hand
            };
            btnSave.Click += SaveCustomer;

            btnCancel = new Guna2Button
            {
                Text = "Cancel",
                Font = Theme.BodyFont,
                FillColor = Theme.SurfaceContainerLowest,
                ForeColor = Theme.OnSurface,
                BorderRadius = 8,
                Size = new Size(100, 44),
                Location = new Point(155, 50),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => { rightPanel.Visible = false; grid.ClearSelection(); };

            btnDelete = new Guna2Button
            {
                Text = "Delete",
                Font = Theme.BodyFont,
                FillColor = Color.DarkRed,
                ForeColor = Color.White,
                BorderRadius = 8,
                Size = new Size(100, 44),
                Location = new Point(265, 50),
                Cursor = Cursors.Hand,
                Visible = AuthService.IsAdmin()
            };
            btnDelete.Click += DeleteCustomer;

            bottomBar.Controls.Add(btnNewRes);
            bottomBar.Controls.Add(btnSave);
            bottomBar.Controls.Add(btnCancel);
            bottomBar.Controls.Add(btnDelete);
            rightPanel.Controls.Add(bottomBar);

            // ── Tab control (fills middle space)
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = Theme.LabelFont,
                Appearance = TabAppearance.FlatButtons,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(72, 26),
                DrawMode = TabDrawMode.OwnerDrawFixed,
                BackColor = Theme.SurfaceContainerHigh
            };
            tabControl.DrawItem += TabControl_DrawItem;

            tabControl.TabPages.Add(BuildProfileTab());
            tabControl.TabPages.Add(BuildIdentityTab());
            tabControl.TabPages.Add(BuildNotesTab());
            tabControl.TabPages.Add(BuildPreferencesTab());
            tabControl.TabPages.Add(BuildHistoryTab());
            tabControl.TabPages.Add(BuildAnalyticsTab());

            rightPanel.Controls.Add(tabControl);
            this.Controls.Add(rightPanel);
            rightPanel.BringToFront();
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tab = (TabControl)sender;
            var page = tab.TabPages[e.Index];
            bool selected = (e.Index == tab.SelectedIndex);

            using (var bg = new SolidBrush(selected ? Theme.PrimaryContainer : Theme.SurfaceContainerLowest))
                e.Graphics.FillRectangle(bg, e.Bounds);

            Color fg = selected ? Color.White : Theme.OnSurfaceVariant;
            using (var brush = new SolidBrush(fg))
            {
                var sz = e.Graphics.MeasureString(page.Text, Theme.LabelFont);
                e.Graphics.DrawString(page.Text, Theme.LabelFont, brush,
                    e.Bounds.Left + (e.Bounds.Width - sz.Width) / 2,
                    e.Bounds.Top + (e.Bounds.Height - sz.Height) / 2);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  TAB PAGES
        // ═══════════════════════════════════════════════════════════════════════

        private TabPage BuildProfileTab()
        {
            var page = MkPage("Profile");
            var fl   = MkScroll(page);

            txtName   = TBox(fl, "Full Name *",            isFirst: true);
            txtPhone  = TBox(fl, "Phone Number *");
            txtEmail  = TBox(fl, "Email Address");

            FlLbl(fl, "Gender");
            cbGender  = FlCombo(fl, new[] { "", "Male", "Female", "Other", "Prefer not to say" });

            FlLbl(fl, "Date of Birth");
            var dobRow = FlRow(fl, 36);
            chkDob = new CheckBox { Text = "Enable", ForeColor = Theme.OnSurfaceVariant, Font = Theme.LabelFont, Location = new Point(0, 9), BackColor = Color.Transparent, AutoSize = true };
            dtDob  = new Guna2DateTimePicker { Location = new Point(72, 1), Size = new Size(FW - 74, 34), BorderRadius = 7, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddYears(-30), Enabled = false };
            chkDob.CheckedChanged += (s, e) => dtDob.Enabled = chkDob.Checked;
            dobRow.Controls.Add(chkDob); dobRow.Controls.Add(dtDob);

            txtNationality = TBox(fl, "Nationality");
            txtAddress     = TBox(fl, "Address");
            txtOccupation  = TBox(fl, "Occupation");
            txtCompany     = TBox(fl, "Company / Organization");

            return page;
        }

        private TabPage BuildIdentityTab()
        {
            var page = MkPage("Identity");
            var fl   = MkScroll(page);

            FlLbl(fl, "ID Type", isFirst: true);
            cbIdType = FlCombo(fl, new[] { "Passport", "National ID", "Driver's License", "Other" });
            cbIdType.SelectedIndex = 0;

            txtIdNumber = TBox(fl, "ID / Passport Number");

            FlLbl(fl, "ID Expiry Date");
            var expRow = FlRow(fl, 36);
            chkIdExpiry = new CheckBox { Text = "Enable", ForeColor = Theme.OnSurfaceVariant, Font = Theme.LabelFont, Location = new Point(0, 9), BackColor = Color.Transparent, AutoSize = true };
            dtIdExpiry  = new Guna2DateTimePicker { Location = new Point(72, 1), Size = new Size(FW - 74, 34), BorderRadius = 7, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddYears(5), Enabled = false };
            void RefreshWarn() => lblIdWarn.Visible = chkIdExpiry.Checked && dtIdExpiry.Value.Date < DateTime.Today;
            chkIdExpiry.CheckedChanged += (s, e) => { dtIdExpiry.Enabled = chkIdExpiry.Checked; RefreshWarn(); };
            dtIdExpiry.ValueChanged    += (s, e) => RefreshWarn();
            expRow.Controls.Add(chkIdExpiry); expRow.Controls.Add(dtIdExpiry);

            lblIdWarn = new Label { Text = "⚠ This ID has expired!", ForeColor = Color.LightCoral, Font = Theme.LabelFont, Width = FW, Height = 18, Margin = new Padding(0, 4, 0, 0), BackColor = Color.Transparent, Visible = false };
            fl.Controls.Add(lblIdWarn);

            return page;
        }

        private TabPage BuildNotesTab()
        {
            var page = MkPage("Notes");
            var fl   = MkScroll(page);

            FlLbl(fl, "Customer Type", isFirst: true);
            cbCustomerType = FlCombo(fl, new[] { "Regular", "VIP", "Corporate" });
            cbCustomerType.SelectedIndex = 0;

            FlLbl(fl, "Account Status");
            cbCustStatus = FlCombo(fl, new[] { "Active", "Inactive" }, w: 200);
            cbCustStatus.SelectedIndex = 0;

            // Blacklist toggle row
            var blkRow = FlRow(fl, 34);
            blkRow.Margin = new Padding(0, 8, 0, 2);
            blkRow.Controls.Add(new Label { Text = "Blacklisting", Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, Location = new Point(0, 9), AutoSize = true, BackColor = Color.Transparent });
            chkBlacklisted = new CheckBox { Text = "Mark as Blacklisted", ForeColor = Color.LightCoral, Font = Theme.BodyFont, Location = new Point(90, 7), BackColor = Color.Transparent, AutoSize = true };
            blkRow.Controls.Add(chkBlacklisted);

            // Blacklist reason (only when checked)
            pnlBlacklist = new Panel { Width = FW, Height = 58, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 6), Visible = false };
            TLbl(pnlBlacklist, "Blacklist Reason (required) *", 0);
            txtBlacklistReason = new Guna2TextBox { Location = new Point(0, 18), Size = new Size(FW, 34), FillColor = Color.FromArgb(80, 30, 30), ForeColor = Color.LightCoral, BorderColor = Color.LightCoral, BorderRadius = 7, Font = Theme.BodyFont };
            pnlBlacklist.Controls.Add(txtBlacklistReason);
            fl.Controls.Add(pnlBlacklist);
            chkBlacklisted.CheckedChanged += (s, e) => pnlBlacklist.Visible = chkBlacklisted.Checked;

            txtEmergencyName  = TBox(fl, "Emergency Contact Name");
            txtEmergencyPhone = TBox(fl, "Emergency Contact Phone");

            FlLbl(fl, "Notes / Remarks");
            txtNotes = new Guna2TextBox { Size = new Size(FW, 82), Margin = new Padding(0, 0, 0, 0), FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, BorderColor = Theme.OutlineVariant, BorderRadius = 7, Font = Theme.BodyFont, Multiline = true };
            fl.Controls.Add(txtNotes);

            return page;
        }

        private TabPage BuildPreferencesTab()
        {
            var page = MkPage("Prefs");
            var fl   = MkScroll(page);

            FlLbl(fl, "Preferred Room Type", isFirst: true);
            cbPrefRoomType = FlCombo(fl);
            cbPrefRoomType.Items.Add("No Preference");
            foreach (var rt in DataStore.Data.RoomTypes) cbPrefRoomType.Items.Add(rt.Name);
            cbPrefRoomType.SelectedIndex = 0;

            FlLbl(fl, "Smoking Preference");
            cbSmoking = FlCombo(fl, new[] { "No Preference", "Non-Smoking", "Smoking" });
            cbSmoking.SelectedIndex = 0;

            FlLbl(fl, "Floor Preference");
            cbFloor = FlCombo(fl, new[] { "No Preference", "Low Floor (1–3)", "Mid Floor (4–7)", "High Floor (8+)" });
            cbFloor.SelectedIndex = 0;

            FlLbl(fl, "Bed Type Preference");
            cbBedType = FlCombo(fl, new[] { "No Preference", "Single", "Double", "Twin", "King", "Queen" });
            cbBedType.SelectedIndex = 0;

            return page;
        }

        private TabPage BuildHistoryTab()
        {
            var page = MkPage("History");

            // Payments panel (fill, added first)
            var payPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.SurfaceContainerHigh };
            var lblPay = new Label { Text = "Payment History", Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, Dock = DockStyle.Top, Height = 22, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0), BackColor = Color.Transparent };
            gridPayHistory = MkMiniGrid();
            gridPayHistory.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Date",   HeaderText = "Date",       MinimumWidth = 90 });
            gridPayHistory.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Amount", HeaderText = "Amount ($)", MinimumWidth = 90, DefaultCellStyle = { Format = "C2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            gridPayHistory.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Method", HeaderText = "Method",     MinimumWidth = 100 });
            payPanel.Controls.Add(gridPayHistory);
            payPanel.Controls.Add(lblPay);

            // Separator
            var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.SurfaceContainerHighest };

            // Reservations panel (top, added last so it floats to top)
            var resPanel = new Panel { Dock = DockStyle.Top, Height = 220, BackColor = Theme.SurfaceContainerHigh };
            var lblRes = new Label { Text = "Reservation History", Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, Dock = DockStyle.Top, Height = 22, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0), BackColor = Color.Transparent };
            gridResHistory = MkMiniGrid();
            gridResHistory.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CheckIn",    HeaderText = "Check-In",  MinimumWidth = 85 });
            gridResHistory.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CheckOut",   HeaderText = "Check-Out", MinimumWidth = 85 });
            gridResHistory.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Room",       HeaderText = "Room #",    MinimumWidth = 70 });
            gridResHistory.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalPrice", HeaderText = "Total ($)", MinimumWidth = 80, DefaultCellStyle = { Format = "C2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            gridResHistory.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status",     HeaderText = "Status",    MinimumWidth = 90 });
            resPanel.Controls.Add(gridResHistory);
            resPanel.Controls.Add(lblRes);

            page.Controls.Add(payPanel);
            page.Controls.Add(sep);
            page.Controls.Add(resPanel);
            return page;
        }

        private TabPage BuildAnalyticsTab()
        {
            var page = MkPage("Stats");

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                Padding = new Padding(6),
                BackColor = Theme.SurfaceContainerHigh
            };

            lblAStays    = StatCard(flow, "Total Stays");
            lblANights   = StatCard(flow, "Total Nights");
            lblASpent    = StatCard(flow, "Total Spent");
            lblAAvg      = StatCard(flow, "Avg Stay (days)");
            lblARoomType = StatCard(flow, "Fav Room Type");
            lblATier     = StatCard(flow, "Loyalty Tier");
            lblAPoints   = StatCard(flow, "Loyalty Points");
            lblALastVisit= StatCard(flow, "Last Visit");

            page.Controls.Add(flow);
            return page;
        }

        private Label StatCard(FlowLayoutPanel parent, string title)
        {
            var card  = new Guna2Panel { Size = new Size(210, 95), FillColor = Theme.SurfaceContainerLowest, BorderRadius = 10, Margin = new Padding(5) };
            var lblT  = new Label { Text = title, Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, AutoSize = true, Location = new Point(12, 10), BackColor = Color.Transparent };
            var lblV  = new Label { Text = "—", Font = new Font("Segoe UI", 17F, FontStyle.Bold), ForeColor = Theme.Primary, AutoSize = true, Location = new Point(12, 34), BackColor = Color.Transparent };
            card.Controls.Add(lblT);
            card.Controls.Add(lblV);
            parent.Controls.Add(card);
            return lblV;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  DATA LOADING & OPEN FORM
        // ═══════════════════════════════════════════════════════════════════════

        private void LoadData()
        {
            if (_isLoading) return;
            if (this.InvokeRequired) { this.Invoke(new Action(LoadData)); return; }
            _isLoading = true;
            try
            {
            // Rebuild nationality dropdown preserving selection
            if (cbFNationality != null)
            {
                string saved = cbFNationality.SelectedItem?.ToString() ?? "All";
                cbFNationality.Items.Clear();
                cbFNationality.Items.Add("All");
                foreach (var n in DataStore.Data.Customers
                    .Select(c => c.Nationality)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct().OrderBy(n => n))
                    cbFNationality.Items.Add(n);
                cbFNationality.SelectedItem = saved;
                if (cbFNationality.SelectedIndex < 0) cbFNationality.SelectedIndex = 0;
            }

            var query = DataStore.Data.Customers.AsEnumerable();

            // Text search
            string s = txtSearch?.Text.Trim().ToLower() ?? "";
            if (!string.IsNullOrWhiteSpace(s))
                query = query.Where(c =>
                    (c.FullName        != null && c.FullName.ToLower().Contains(s))        ||
                    (c.Phone           != null && c.Phone.ToLower().Contains(s))           ||
                    (c.Email           != null && c.Email.ToLower().Contains(s))           ||
                    (c.IdNumber        != null && c.IdNumber.ToLower().Contains(s))        ||
                    (c.PassportNumber  != null && c.PassportNumber.ToLower().Contains(s)));

            // Filters
            string nat = cbFNationality?.SelectedItem?.ToString() ?? "All";
            if (nat != "All") query = query.Where(c => c.Nationality == nat);

            string gen = cbFGender?.SelectedItem?.ToString() ?? "All";
            if (gen != "All") query = query.Where(c => c.Gender == gen);

            string typ = cbFType?.SelectedItem?.ToString() ?? "All";
            if (typ == "Blacklisted")
                query = query.Where(c => c.IsBlacklisted);
            else if (typ != "All")
                query = query.Where(c => c.CustomerType == typ && !c.IsBlacklisted);

            string stat = cbFStatus?.SelectedItem?.ToString() ?? "All";
            if (stat != "All") query = query.Where(c => (c.Status ?? "Active") == stat);

            // Project display row (DisplayType resolves Blacklisted override)
            grid.DataSource = query.OrderBy(c => c.FullName).Select(c => new
            {
                c.Id,
                c.FullName,
                DisplayType  = c.IsBlacklisted ? "⚠ Blacklisted" : (string.IsNullOrEmpty(c.CustomerType) ? "Regular" : c.CustomerType),
                c.Phone,
                c.Email,
                c.Nationality,
                LoyaltyTier  = string.IsNullOrEmpty(c.LoyaltyTier) ? "None" : c.LoyaltyTier,
                Status       = string.IsNullOrEmpty(c.Status) ? "Active" : c.Status
            }).ToList();
            } // end try
            finally { _isLoading = false; }
        }

        private void OpenForm(string id)
        {
            _editingId = id;
            rightPanel.Visible = true;
            ClearAnalytics();

            if (id == null)
            {
                lblPanelName.Text        = "New Customer";
                lblPanelBadge.Text       = "";
                lblPanelBadge.ForeColor  = Theme.OnSurfaceVariant;
                btnNewRes.Visible        = false;
                btnDelete.Visible        = false;
                ClearForm();
            }
            else
            {
                var c = DataStore.Data.Customers.FirstOrDefault(x => x.Id == id);
                if (c == null) return;

                lblPanelName.Text = c.FullName ?? "—";
                ApplyBadge(c);
                btnNewRes.Visible  = true;
                btnDelete.Visible  = AuthService.IsAdmin();

                PopulateForm(c);
                LoadHistory(id);

                CustomerService.UpdateLoyalty(c);
                ApplyAnalytics(CustomerService.GetStats(id), c);
            }
        }

        private void ClearForm()
        {
            txtName.Text = ""; txtPhone.Text = ""; txtEmail.Text = "";
            cbGender.SelectedIndex = 0;
            chkDob.Checked = false; dtDob.Value = DateTime.Today.AddYears(-30);
            txtNationality.Text = ""; txtAddress.Text = ""; txtOccupation.Text = ""; txtCompany.Text = "";

            cbIdType.SelectedIndex = 0; txtIdNumber.Text = "";
            chkIdExpiry.Checked = false; dtIdExpiry.Value = DateTime.Today.AddYears(5);

            cbCustomerType.SelectedIndex = 0; cbCustStatus.SelectedIndex = 0;
            chkBlacklisted.Checked = false; txtBlacklistReason.Text = "";
            txtEmergencyName.Text = ""; txtEmergencyPhone.Text = ""; txtNotes.Text = "";

            cbPrefRoomType.SelectedIndex = 0; cbSmoking.SelectedIndex = 0;
            cbFloor.SelectedIndex = 0; cbBedType.SelectedIndex = 0;

            gridResHistory.DataSource = null;
            gridPayHistory.DataSource = null;
        }

        private void PopulateForm(CustomerModel c)
        {
            txtName.Text  = c.FullName ?? "";
            txtPhone.Text = c.Phone ?? "";
            txtEmail.Text = c.Email ?? "";
            SafeSet(cbGender, c.Gender ?? "");

            if (c.DateOfBirth.HasValue) { chkDob.Checked = true; dtDob.Value = c.DateOfBirth.Value; }
            else                        { chkDob.Checked = false; }

            txtNationality.Text = c.Nationality ?? "";
            txtAddress.Text     = c.Address ?? "";
            txtOccupation.Text  = c.Occupation ?? "";
            txtCompany.Text     = c.Company ?? "";

            // Prefer IdNumber, fall back to legacy PassportNumber
            string idNum = !string.IsNullOrWhiteSpace(c.IdNumber) ? c.IdNumber : (c.PassportNumber ?? "");
            SafeSet(cbIdType, c.IdType ?? "Passport");
            txtIdNumber.Text = idNum;

            if (c.IdExpiryDate.HasValue) { chkIdExpiry.Checked = true; dtIdExpiry.Value = c.IdExpiryDate.Value; }
            else                         { chkIdExpiry.Checked = false; }

            SafeSet(cbCustomerType, c.CustomerType ?? "Regular");
            SafeSet(cbCustStatus,   c.Status ?? "Active");
            chkBlacklisted.Checked     = c.IsBlacklisted;
            txtBlacklistReason.Text    = c.BlacklistReason ?? "";
            txtEmergencyName.Text      = c.EmergencyContactName ?? "";
            txtEmergencyPhone.Text     = c.EmergencyContactPhone ?? "";
            txtNotes.Text              = c.Notes ?? "";

            SafeSet(cbPrefRoomType, c.PreferredRoomType ?? "No Preference");
            SafeSet(cbSmoking,      c.SmokingPreference ?? "No Preference");
            SafeSet(cbFloor,        c.FloorPreference   ?? "No Preference");
            SafeSet(cbBedType,      c.BedTypePreference ?? "No Preference");
        }

        private void LoadHistory(string customerId)
        {
            var reservations = CustomerService.GetReservations(customerId)
                .Join(DataStore.Data.Rooms, r => r.RoomId, rm => rm.Id, (r, rm) => new
                {
                    CheckIn    = r.CheckIn.ToShortDateString(),
                    CheckOut   = r.CheckOut.ToShortDateString(),
                    Room       = rm.RoomNumber,
                    TotalPrice = r.TotalPrice,
                    r.Status
                }).ToList();
            gridResHistory.DataSource = reservations;

            var payments = CustomerService.GetPayments(customerId)
                .Select(p => new { Date = p.Date.ToShortDateString(), p.Amount, p.Method })
                .ToList();
            gridPayHistory.DataSource = payments;
        }

        private void ApplyAnalytics(CustomerStats stats, CustomerModel c)
        {
            lblAStays.Text     = stats.TotalStays.ToString();
            lblANights.Text    = stats.TotalNightsStayed.ToString();
            lblASpent.Text     = "$" + stats.TotalSpent.ToString("0.00");
            lblAAvg.Text       = stats.AverageStayDuration.ToString("0.1");
            lblARoomType.Text  = stats.MostUsedRoomType;
            lblATier.Text      = string.IsNullOrEmpty(c.LoyaltyTier) ? "None" : c.LoyaltyTier;
            lblAPoints.Text    = c.LoyaltyPoints.ToString();
            lblALastVisit.Text = stats.LastVisitDate.HasValue
                ? stats.LastVisitDate.Value.ToShortDateString()
                : "No visits yet";
        }

        private void ClearAnalytics()
        {
            foreach (var lbl in new[] { lblAStays, lblANights, lblASpent, lblAAvg, lblARoomType, lblATier, lblAPoints, lblALastVisit })
                if (lbl != null) lbl.Text = "—";
            if (gridResHistory != null) gridResHistory.DataSource = null;
            if (gridPayHistory != null) gridPayHistory.DataSource = null;
        }

        private void ApplyBadge(CustomerModel c)
        {
            if (c.IsBlacklisted)
            { lblPanelBadge.Text = "⚠ BLACKLISTED";        lblPanelBadge.ForeColor = Color.LightCoral; }
            else if (c.LoyaltyTier == "Platinum")
            { lblPanelBadge.Text = "💎 Platinum Member";    lblPanelBadge.ForeColor = Theme.Tertiary; }
            else if (c.LoyaltyTier == "Gold")
            { lblPanelBadge.Text = "⭐ Gold Member";         lblPanelBadge.ForeColor = Color.Gold; }
            else if (c.CustomerType == "VIP")
            { lblPanelBadge.Text = "⭐ VIP Customer";        lblPanelBadge.ForeColor = Color.Gold; }
            else if (c.CustomerType == "Corporate")
            { lblPanelBadge.Text = "🏢 Corporate Account";  lblPanelBadge.ForeColor = Theme.Secondary; }
            else
            { lblPanelBadge.Text = $"Status: {c.Status ?? "Active"}"; lblPanelBadge.ForeColor = Theme.OnSurfaceVariant; }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  SAVE / DELETE / NEW RESERVATION
        // ═══════════════════════════════════════════════════════════════════════

        private void SaveCustomer(object sender, EventArgs e)
        {
            var existing  = _editingId != null ? DataStore.Data.Customers.FirstOrDefault(x => x.Id == _editingId) : null;
            bool isNew    = (_editingId == null);

            var c = new CustomerModel
            {
                Id                   = _editingId ?? DataStore.GenerateId(),
                FullName             = txtName.Text.Trim(),
                Phone                = txtPhone.Text.Trim(),
                Email                = txtEmail.Text.Trim(),
                Gender               = cbGender.SelectedItem?.ToString(),
                DateOfBirth          = chkDob.Checked ? (DateTime?)dtDob.Value.Date : null,
                Nationality          = txtNationality.Text.Trim(),
                Address              = txtAddress.Text.Trim(),
                Occupation           = txtOccupation.Text.Trim(),
                Company              = txtCompany.Text.Trim(),
                IdType               = cbIdType.SelectedItem?.ToString(),
                IdNumber             = txtIdNumber.Text.Trim(),
                PassportNumber       = txtIdNumber.Text.Trim(),   // keep legacy field in sync
                IdExpiryDate         = chkIdExpiry.Checked ? (DateTime?)dtIdExpiry.Value.Date : null,
                CustomerType         = chkBlacklisted.Checked ? "Regular" : cbCustomerType.SelectedItem?.ToString(),
                Status               = cbCustStatus.SelectedItem?.ToString() ?? "Active",
                IsBlacklisted        = chkBlacklisted.Checked,
                BlacklistReason      = chkBlacklisted.Checked ? txtBlacklistReason.Text.Trim() : "",
                EmergencyContactName = txtEmergencyName.Text.Trim(),
                EmergencyContactPhone= txtEmergencyPhone.Text.Trim(),
                Notes                = txtNotes.Text.Trim(),
                PreferredRoomType    = cbPrefRoomType.SelectedItem?.ToString(),
                SmokingPreference    = cbSmoking.SelectedItem?.ToString(),
                FloorPreference      = cbFloor.SelectedItem?.ToString(),
                BedTypePreference    = cbBedType.SelectedItem?.ToString(),
                // Preserve loyalty + tracking from existing record
                LoyaltyPoints        = existing?.LoyaltyPoints ?? 0,
                LoyaltyTier          = existing?.LoyaltyTier ?? "None",
                CreatedDate          = existing?.CreatedDate ?? DateTime.Now,
                LastVisitDate        = existing?.LastVisitDate
            };

            // Validation
            var errors = CustomerService.ValidateCustomer(c, _editingId);
            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join("\n\n", errors), "Validation Errors", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Soft duplicate warning (new customers only)
            if (isNew)
            {
                var dupes = CustomerService.CheckDuplicates(c);
                if (dupes.Count > 0)
                {
                    string names = string.Join(", ", dupes.Select(d => d.FullName));
                    var answer = MessageBox.Show(
                        $"Possible duplicate(s) detected:\n{names}\n\nCreate this customer anyway?",
                        "Duplicate Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (answer != DialogResult.Yes) return;
                }
            }

            // Persist
            if (isNew)
                DataStore.Data.Customers.Add(c);
            else
            {
                int idx = DataStore.Data.Customers.FindIndex(x => x.Id == _editingId);
                if (idx >= 0) DataStore.Data.Customers[idx] = c;
            }

            CustomerService.UpdateLoyalty(c);
            DataStore.Save();
            rightPanel.Visible = false;
        }

        private void DeleteCustomer(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_editingId)) return;

            var (ok, reason) = CustomerService.CanDelete(_editingId);
            if (!ok) { MessageBox.Show(reason, "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var c = DataStore.Data.Customers.FirstOrDefault(x => x.Id == _editingId);
            if (c == null) return;

            if (MessageBox.Show($"Permanently delete '{c.FullName}'?\nThis cannot be undone.",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                DataStore.Data.Customers.Remove(c);
                DataStore.Save();
                rightPanel.Visible = false;
            }
        }

        private void BtnNewRes_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_editingId)) return;
            new NewReservationModal(_editingId).ShowDialog();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  GRID EVENTS
        // ═══════════════════════════════════════════════════════════════════════

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            // Ignore programmatic selection changes that happen during data binding
            if (_isLoading) return;
            if (grid.SelectedRows.Count > 0)
            {
                string id = grid.SelectedRows[0].Cells["ColId"].Value?.ToString();
                if (!string.IsNullOrEmpty(id)) OpenForm(id);
            }
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count) return;

            string type = grid.Rows[e.RowIndex].Cells["ColType"].Value?.ToString() ?? "";

            if (type.Contains("Blacklisted"))
            {
                e.CellStyle.BackColor = Color.FromArgb(70, 25, 25);
                e.CellStyle.ForeColor = Color.LightCoral;
            }
            else if (type == "VIP")
            {
                e.CellStyle.BackColor = Color.FromArgb(58, 48, 8);
                e.CellStyle.ForeColor = Color.Gold;
            }
            else if (type == "Corporate")
            {
                e.CellStyle.BackColor = Color.FromArgb(18, 28, 55);
                e.CellStyle.ForeColor = Theme.Secondary;
            }

            if (grid.Columns[e.ColumnIndex].Name == "ColTier")
            {
                string tier = e.Value?.ToString() ?? "";
                if      (tier == "Platinum") e.CellStyle.ForeColor = Theme.Tertiary;
                else if (tier == "Gold")     e.CellStyle.ForeColor = Color.Gold;
                else if (tier == "Silver")   e.CellStyle.ForeColor = Theme.Secondary;
            }

            if (grid.Columns[e.ColumnIndex].Name == "ColStatus" && e.Value?.ToString() == "Inactive")
                e.CellStyle.ForeColor = Theme.OnSurfaceVariant;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════════════════

        // ── Layout constants
        private const int FW = 430; // field width (480 panel - 12 left - 4 right padding - 17 scrollbar - 17 spare)

        /// <summary>Adds a field Label directly to the FlowLayout. isFirst removes extra top margin.</summary>
        private void FlLbl(FlowLayoutPanel fl, string text, bool isFirst = false)
        {
            fl.Controls.Add(new Label
            {
                Text = text, Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant,
                Width = FW, Height = 18, AutoSize = false,
                Margin = new Padding(0, isFirst ? 2 : 10, 0, 3),
                BackColor = Color.Transparent
            });
        }

        /// <summary>Adds a Label then a TextBox directly to the FlowLayout and returns the TextBox.</summary>
        private Guna2TextBox TBox(FlowLayoutPanel fl, string label, int inputH = 34, bool isFirst = false)
        {
            FlLbl(fl, label, isFirst);
            var txt = new Guna2TextBox
            {
                Size = new Size(FW, inputH),
                Margin = new Padding(0, 0, 0, 0),
                FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface,
                BorderColor = Theme.OutlineVariant, BorderRadius = 7, Font = Theme.BodyFont,
                Multiline = (inputH > 34)
            };
            fl.Controls.Add(txt);
            return txt;
        }

        /// <summary>Adds a ComboBox directly to the FlowLayout and returns it.</summary>
        private Guna2ComboBox FlCombo(FlowLayoutPanel fl, string[] items = null, int w = 0)
        {
            var cb = new Guna2ComboBox
            {
                Size = new Size(w > 0 ? w : FW, 34),
                Margin = new Padding(0, 0, 0, 0),
                BorderRadius = 7, FillColor = Theme.SurfaceContainerLowest,
                ForeColor = Theme.OnSurface, DropDownStyle = ComboBoxStyle.DropDownList
            };
            if (items != null) cb.Items.AddRange(items);
            fl.Controls.Add(cb);
            return cb;
        }

        /// <summary>Creates a fixed-height horizontal container panel inside the FlowLayout (for side-by-side controls).</summary>
        private Panel FlRow(FlowLayoutPanel fl, int h)
        {
            var p = new Panel { Width = FW, Height = h, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 0) };
            fl.Controls.Add(p);
            return p;
        }

        /// <summary>Retained for use inside fixed-size sub-panels (e.g. pnlBlacklist).</summary>
        private void TLbl(Panel parent, string text, int y) =>
            parent.Controls.Add(new Label
            {
                Text = text, Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant,
                Location = new Point(0, y), AutoSize = true, BackColor = Color.Transparent
            });

        /// <summary>Creates a TopDown scrollable FlowLayoutPanel docked to fill the TabPage.</summary>
        private FlowLayoutPanel MkScroll(TabPage page)
        {
            var fl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(12, 4, 4, 12),
                BackColor = Theme.SurfaceContainerHigh
            };
            page.Controls.Add(fl);
            return fl;
        }





        private static void SafeSet(ComboBox cb, string value)
        {
            cb.SelectedItem = value;
            if (cb.SelectedIndex < 0) cb.SelectedIndex = 0;
        }

        private TabPage MkPage(string title) =>
            new TabPage(title) { BackColor = Theme.SurfaceContainerHigh, BorderStyle = BorderStyle.None };

        private Guna2DataGridView MkMiniGrid() =>
            new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                ThemeStyle =
                {
                    RowsStyle    = { BackColor = Theme.Surface, ForeColor = Theme.OnSurface, SelectionBackColor = Theme.PrimaryContainer, SelectionForeColor = Color.White },
                    HeaderStyle  = { BackColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, Font = Theme.LabelFont }
                },
                ReadOnly = true,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                BackgroundColor = Theme.Surface,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

        protected override void Dispose(bool disposing)
        {
            if (disposing) EventBus.Instance.DataChanged -= OnDataChanged;
            base.Dispose(disposing);
        }
    }
}
