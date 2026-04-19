using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using HRS.Services;

namespace HRS.UI
{
    public class LoginForm : Form
    {
        private Guna2TextBox txtUsername;
        private Guna2TextBox txtPassword;
        private Label lblError;

        public LoginForm()
        {
            this.Text = "Login - Nocturnal Concierge";
            this.Size = new Size(450, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Theme.Surface;
            
            // Drag Control
            Guna2DragControl dragControl = new Guna2DragControl { TargetControl = this };

            // Panel
            Guna2Panel panel = new Guna2Panel { Size = new Size(350, 450), Location = new Point(50, 50), BackColor = Color.Transparent, FillColor = Theme.SurfaceContainerHigh, BorderRadius = 12, ShadowDecoration = { Enabled = true, Shadow = new Padding(10), Depth = 50, Color = Color.Black } };
            this.Controls.Add(panel);

            // Title
            Label lblTitle = new Label { Text = "Nocturnal", Font = Theme.HeadlineFont, ForeColor = Theme.Primary, Size = new Size(300, 30), Location = new Point(25, 30), TextAlign = ContentAlignment.MiddleCenter };
            Label lblSub = new Label { Text = "Concierge Access", Font = Theme.LabelFont, ForeColor = Theme.OnSurfaceVariant, Size = new Size(300, 20), Location = new Point(25, 60), TextAlign = ContentAlignment.MiddleCenter };
            
            // Username
            txtUsername = new Guna2TextBox { Size = new Size(300, 40), Location = new Point(25, 120), FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, BorderColor = Theme.OutlineVariant, BorderRadius = 8, PlaceholderText = "Username", PlaceholderForeColor = Theme.OnSurfaceVariant };
            
            // Password
            txtPassword = new Guna2TextBox { Size = new Size(300, 40), Location = new Point(25, 180), FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, BorderColor = Theme.OutlineVariant, BorderRadius = 8, PlaceholderText = "Password", PlaceholderForeColor = Theme.OnSurfaceVariant, UseSystemPasswordChar = true };
            
            // Error Label
            lblError = new Label { Text = "Invalid credentials", ForeColor = Color.Red, Font = Theme.BodyFont, Size = new Size(300, 20), Location = new Point(25, 230), Visible = false };

            // Login Button
            Guna2GradientButton btnLogin = new Guna2GradientButton { Text = "SIGN IN", Font = Theme.BodyFont, Size = new Size(300, 45), Location = new Point(25, 270), BorderRadius = 8, FillColor = Theme.Primary, FillColor2 = Theme.PrimaryContainer, ForeColor = Theme.OnPrimary, Cursor = Cursors.Hand };
            btnLogin.Click += BtnLogin_Click;

            // Exit Button
            Guna2Button btnExit = new Guna2Button { Text = "EXIT", Font = Theme.BodyFont, Size = new Size(300, 45), Location = new Point(25, 330), BorderRadius = 8, FillColor = Theme.SurfaceContainerLowest, ForeColor = Theme.OnSurface, Cursor = Cursors.Hand };
            btnExit.Click += (s, e) => Application.Exit();

            panel.Controls.Add(lblTitle);
            panel.Controls.Add(lblSub);
            panel.Controls.Add(txtUsername);
            panel.Controls.Add(txtPassword);
            panel.Controls.Add(lblError);
            panel.Controls.Add(btnLogin);
            panel.Controls.Add(btnExit);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (AuthService.Login(txtUsername.Text, txtPassword.Text))
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                lblError.Visible = true;
            }
        }
    }
}
