using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace CipherTool
{
    public partial class Form1 : Form
    {
        // ------------------------------------------------------------------ //
        //  UI KONTROLLERI (CS8618 Çözümü için = null!; eklendi)
        // ------------------------------------------------------------------ //
        private Panel pnlHeader = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;

        private Panel pnlDropZone = null!;
        private Label lblDropHint = null!;
        private Label lblFileName = null!;
        private Button btnBrowse = null!;

        private Panel pnlPassword = null!;
        private Label lblPassword = null!;
        private TextBox txtPassword = null!;
        private CheckBox chkShowPassword = null!;
        private Label lblPasswordStrength = null!;
        private Label lblPasswordRules = null!;

        private Button btnEncrypt = null!;
        private Button btnDecrypt = null!;
        private Button btnClear = null!;
        private Button btnHint = null!;

        private Panel pnlStatus = null!;
        private Label lblStatusIcon = null!;
        private Label lblStatusText = null!;

        private Panel pnlFooter = null!;
        private Label lblFooter = null!;

        // Renk paleti
        private static readonly Color ColorBg = Color.FromArgb(18, 18, 30);
        private static readonly Color ColorPanel = Color.FromArgb(28, 28, 45);
        private static readonly Color ColorAccent = Color.FromArgb(99, 102, 241);
        private static readonly Color ColorSuccess = Color.FromArgb(34, 197, 94);
        private static readonly Color ColorDanger = Color.FromArgb(239, 68, 68);
        private static readonly Color ColorWarning = Color.FromArgb(234, 179, 8);
        private static readonly Color ColorText = Color.FromArgb(226, 232, 240);
        private static readonly Color ColorMuted = Color.FromArgb(100, 116, 139);

        private string _selectedFilePath = string.Empty;

        // ------------------------------------------------------------------ //
        //  CONSTRUCTOR
        // ------------------------------------------------------------------ //
        public Form1()
        {
            InitializeComponent();
            BuildUI();
            WireEvents();
        }

        // ------------------------------------------------------------------ //
        //  UI İNŞASI
        // ------------------------------------------------------------------ //
        private void BuildUI()
        {
            // FORM
            Text = "CipherTool — Dosya Şifreleme";
            MinimumSize = new Size(500, 650);
            Size = new Size(620, 750);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ColorBg;
            ForeColor = ColorText;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            Font = new Font("Segoe UI", 9.5f);

            // Ana TableLayoutPanel
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 1,
                BackColor = ColorBg,
                Padding = new Padding(0)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));   // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));    // DropZone
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));  // Password
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // Buttons
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));    // Status
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));   // Footer

            // ── HEADER ──────────────────────────────────────────────────
            pnlHeader = new Panel { Dock = DockStyle.Fill, BackColor = ColorPanel };
            lblTitle = new Label
            {
                Text = "🔐  CipherTool",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = ColorAccent,
                AutoSize = true,
                Location = new Point(24, 10)
            };
            lblSubtitle = new Label
            {
                Text = "AES-256 · PBKDF2  |  Güvenli Dosya Şifreleme",
                Font = new Font("Segoe UI", 9f),
                ForeColor = ColorMuted,
                AutoSize = true,
                Location = new Point(27, 46)
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSubtitle });

            // ── DROP ZONE ───────────────────────────────────────────────
            pnlDropZone = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorPanel,
                Margin = new Padding(16, 8, 16, 8),
                AllowDrop = true
            };
            StyleRoundedBorder(pnlDropZone);

            var dropLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            dropLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            dropLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
            dropLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

            lblDropHint = new Label
            {
                Text = "📂   Dosyayı buraya sürükleyip bırakın",
                Font = new Font("Segoe UI", 12f),
                ForeColor = ColorMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            btnBrowse = new Button
            {
                Text = "Dosya Seç (Browse)",
                BackColor = ColorAccent,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Size = new Size(180, 36),
                Anchor = AnchorStyles.None
            };

            var btnPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnPanel.Controls.Add(btnBrowse);
            btnPanel.Resize += (s, e) =>
                btnBrowse.Location = new Point(
                    (btnPanel.Width - btnBrowse.Width) / 2,
                    (btnPanel.Height - btnBrowse.Height) / 2);

            lblFileName = new Label
            {
                Text = string.Empty,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = ColorSuccess,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            dropLayout.Controls.Add(lblDropHint, 0, 0);
            dropLayout.Controls.Add(btnPanel, 0, 1);
            dropLayout.Controls.Add(lblFileName, 0, 2);
            pnlDropZone.Controls.Add(dropLayout);

            // ── PAROLA ──────────────────────────────────────────────────
            pnlPassword = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorPanel,
                Margin = new Padding(16, 0, 16, 8)
            };
            
            lblPassword = new Label
            {
                Text = "Parola",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = ColorMuted,
                AutoSize = true,
                Location = new Point(16, 10)
            };

            txtPassword = new TextBox
            {
                UseSystemPasswordChar = true,
                Font = new Font("Segoe UI", 11f),
                BackColor = ColorBg,
                ForeColor = ColorText,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(16, 32),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                PlaceholderText = "En az 8 karakter girin..."
            };

            // Parola güç göstergesi
            lblPasswordStrength = new Label
            {
                Text = "Güç: Parola girilmedi",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = ColorMuted,
                AutoSize = true,
                Location = new Point(16, 68)
            };

            // Parola kuralları
            lblPasswordRules = new Label
            {
                Text = "Büyük harf · Küçük harf · Rakam · Özel karakter (!@#$%^&* vb.) içermelidir",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = ColorMuted,
                AutoSize = false,
                Location = new Point(16, 90),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            chkShowPassword = new CheckBox
            {
                Text = "Parolayı göster",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = ColorMuted,
                AutoSize = true,
                Location = new Point(16, 130)
            };

            pnlPassword.Controls.AddRange(new Control[]
            {
                lblPassword, txtPassword, lblPasswordStrength, lblPasswordRules, chkShowPassword
            });

            pnlPassword.Resize += (s, e) =>
            {
                txtPassword.Width = pnlPassword.Width - 32;
                lblPasswordRules.Width = pnlPassword.Width - 32;
            };

            // ── BUTONLAR ────────────────────────────────────────────────
            var pnlButtons = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorBg,
                Margin = new Padding(16, 0, 16, 8)
            };

            btnEncrypt = new Button
            {
                Text = "🔒  Şifrele",
                BackColor = ColorAccent,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            btnDecrypt = new Button
            {
                Text = "🔓  Çöz",
                BackColor = Color.FromArgb(30, 100, 80),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            btnHint = new Button
            {
                Text = "💡  İpucu",
                BackColor = Color.FromArgb(70, 70, 110),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            btnClear = new Button
            {
                Text = "✕  Temizle",
                BackColor = Color.FromArgb(60, 40, 40),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            pnlButtons.Controls.AddRange(new Control[] { btnEncrypt, btnDecrypt, btnHint, btnClear });
            pnlButtons.Resize += (s, e) =>
            {
                int gap = 10;
                int btnH = 42;
                int btnW = (pnlButtons.Width - (gap * 3)) / 4;

                btnEncrypt.SetBounds(0, 8, btnW, btnH);
                btnDecrypt.SetBounds(btnW + gap, 8, btnW, btnH);
                btnHint.SetBounds((btnW + gap) * 2, 8, btnW, btnH);
                btnClear.SetBounds((btnW + gap) * 3, 8, btnW, btnH);
            };

            // ── DURUM PANELİ ────────────────────────────────────────────
            pnlStatus = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(25, 25, 40),
                Margin = new Padding(16, 0, 16, 8)
            };

            lblStatusIcon = new Label
            {
                Text = "ℹ",
                Font = new Font("Segoe UI", 22f),
                ForeColor = ColorMuted,
                AutoSize = true,
                Location = new Point(12, 12)
            };
            lblStatusText = new Label
            {
                Text = "Bir dosya seçin ve parolanızı girin, ardından işlem butonuna basın.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = ColorMuted,
                Location = new Point(54, 12),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            pnlStatus.Controls.AddRange(new Control[] { lblStatusIcon, lblStatusText });
            pnlStatus.Resize += (s, e) =>
                lblStatusText.Width = pnlStatus.Width - 70;

            // ── FOOTER ──────────────────────────────────────────────────
            pnlFooter = new Panel { Dock = DockStyle.Fill, BackColor = ColorPanel };
            lblFooter = new Label
            {
                Text = "AES-256-CBC · PBKDF2-SHA256 · 200.000 İterasyon · Random Salt + IV",
                Font = new Font("Segoe UI", 8f),
                ForeColor = ColorMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            pnlFooter.Controls.Add(lblFooter);

            // ── ANA LAYOUT'A EKLE ───────────────────────────────────────
            mainLayout.Controls.Add(pnlHeader, 0, 0);
            mainLayout.Controls.Add(pnlDropZone, 0, 1);
            mainLayout.Controls.Add(pnlPassword, 0, 2);
            mainLayout.Controls.Add(pnlButtons, 0, 3);
            mainLayout.Controls.Add(pnlStatus, 0, 4);
            mainLayout.Controls.Add(pnlFooter, 0, 5);

            Controls.Add(mainLayout);
        }

        // ------------------------------------------------------------------ //
        //  EVENT WIRING
        // ------------------------------------------------------------------ //
        private void WireEvents()
        {
            btnBrowse.Click += BtnBrowse_Click;
            btnEncrypt.Click += BtnEncrypt_Click;
            btnDecrypt.Click += BtnDecrypt_Click;
            btnClear.Click += BtnClear_Click;
            btnHint.Click += BtnHint_Click;

            chkShowPassword.CheckedChanged += (s, e) =>
                txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;

            // Canlı parola gücü göstergesi
            txtPassword.TextChanged += (s, e) =>
            {
                int score = CryptoManager.GetPasswordStrength(txtPassword.Text);

                (string text, Color color) = score switch
                {
                    0 => ("Güç: Parola girilmedi  ○○○○○", ColorMuted),
                    1 => ("Güç: Çok Zayıf          ●○○○○", ColorDanger),
                    2 => ("Güç: Zayıf              ●●○○○", ColorDanger),
                    3 => ("Güç: Orta               ●●●○○", ColorWarning),
                    4 => ("Güç: İyi                ●●●●○", ColorSuccess),
                    5 => ("Güç: Güçlü 🔒           ●●●●●", ColorSuccess),
                    _ => ("", ColorMuted)
                };

                lblPasswordStrength.Text = text;
                lblPasswordStrength.ForeColor = color;
            };

            // Drag & Drop
            pnlDropZone.DragEnter += PnlDropZone_DragEnter;
            pnlDropZone.DragDrop += PnlDropZone_DragDrop;
            pnlDropZone.DragLeave += (s, e) => pnlDropZone.BackColor = ColorPanel;
            lblDropHint.DragEnter += PnlDropZone_DragEnter;
            lblDropHint.DragDrop += PnlDropZone_DragDrop;

            // Hover efekti
            AddHoverEffect(btnEncrypt, ColorAccent, Color.FromArgb(79, 82, 221));
            AddHoverEffect(btnDecrypt, Color.FromArgb(30, 100, 80), Color.FromArgb(22, 163, 74));
            AddHoverEffect(btnClear, Color.FromArgb(60, 40, 40), Color.FromArgb(127, 29, 29));
            AddHoverEffect(btnBrowse, ColorAccent, Color.FromArgb(79, 82, 221));
        }

        // ------------------------------------------------------------------ //
        //  DRAG & DROP
        // ------------------------------------------------------------------ //
        private void PnlDropZone_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
                pnlDropZone.BackColor = Color.FromArgb(35, 40, 65);
            }
            else
                e.Effect = DragDropEffects.None;
        }

        private void PnlDropZone_DragDrop(object? sender, DragEventArgs e)
        {
            pnlDropZone.BackColor = ColorPanel;
            string[]? files = (string[]?)e.Data?.GetData(DataFormats.FileDrop);
            if (files?.Length > 0)
                LoadFile(files[0]);
        }

        // ------------------------------------------------------------------ //
        //  BUTTON HANDLERS
        // ------------------------------------------------------------------ //
        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dlg = new()
            {
                Title = "Şifrelenecek veya Çözülecek Dosyayı Seçin",
                Filter = "Tüm Dosyalar (*.*)|*.*|Cipher Dosyaları (*.cipher)|*.cipher"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                LoadFile(dlg.FileName);
        }

        private void BtnEncrypt_Click(object? sender, EventArgs e)
        {
            if (!ValidateForOperation()) return;

            SetStatus("⏳ Şifreleniyor...", ColorMuted, "⏳");
            Application.DoEvents();

            try
            {
                string output = CryptoManager.EncryptFile(_selectedFilePath, txtPassword.Text);
                SetStatus($"✅ Başarıyla şifrelendi!\n📄 Çıktı: {Path.GetFileName(output)}", ColorSuccess, "✅");
            }
            catch (Exception ex)
            {
                SetStatus($"❌ Hata: {ex.Message}", ColorDanger, "❌");
            }
        }

        private void BtnDecrypt_Click(object? sender, EventArgs e)
        {
            if (!ValidateForOperation()) return;

            SetStatus("⏳ Şifre çözülüyor...", ColorMuted, "⏳");
            Application.DoEvents();

            try
            {
                string output = CryptoManager.DecryptFile(_selectedFilePath, txtPassword.Text);
                SetStatus($"✅ Başarıyla çözüldü!\n📄 Çıktı: {Path.GetFileName(output)}", ColorSuccess, "✅");
            }
            catch (CryptographicException ex)
            {
                SetStatus($"🔑 {ex.Message}", ColorDanger, "🔑");
            }
            catch (Exception ex)
            {
                SetStatus($"❌ Hata: {ex.Message}", ColorDanger, "❌");
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            _selectedFilePath = string.Empty;
            lblFileName.Text = string.Empty;
            txtPassword.Text = string.Empty;
            chkShowPassword.Checked = false;
            lblPasswordStrength.Text = "Güç: Parola girilmedi  ○○○○○";
            lblPasswordStrength.ForeColor = ColorMuted;
            SetStatus("Bir dosya seçin ve parolanızı girin, ardından işlem butonuna basın.", ColorMuted, "ℹ");
        }

        private void BtnHint_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                SetStatus("⚠️ Önce bir dosya seçmelisin.", ColorWarning, "⚠️");
                return;
            }

            if (!_selectedFilePath.EndsWith(".cipher", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("İpucu sadece şifrelenmiş (.cipher) dosyada olur.");
                return;
            }

            try
            {
                string hint = CryptoManager.ReadHint(_selectedFilePath);
                MessageBox.Show("💡 İpucu:\n" + hint);
            }
            catch (Exception ex)
            {
                MessageBox.Show("İpucu okunamadı: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------ //
        //  YARDIMCI METODLAR
        // ------------------------------------------------------------------ //
        private void LoadFile(string path)
        {
            _selectedFilePath = path;
            lblFileName.Text = $"📄 {Path.GetFileName(path)}";
            lblFileName.ForeColor = ColorSuccess;

            if (path.EndsWith(".cipher", StringComparison.OrdinalIgnoreCase))
                SetStatus($"Cipher dosyası seçildi: {Path.GetFileName(path)}\nŞifre çözmek için 'Çöz' butonuna basın.", ColorWarning, "⚠️");
            else
                SetStatus($"Dosya seçildi: {Path.GetFileName(path)}\nParolanızı girin ve 'Şifrele' butonuna basın.", ColorAccent, "📂");
        }

        private bool ValidateForOperation()
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                SetStatus("⚠️ Lütfen önce bir dosya seçin.", ColorWarning, "⚠️");
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                SetStatus("⚠️ Lütfen bir parola girin.", ColorWarning, "⚠️");
                txtPassword.Focus();
                return false;
            }
            return true;
        }

        private void SetStatus(string message, Color color, string icon)
        {
            lblStatusText.Text = message;
            lblStatusText.ForeColor = color;
            lblStatusIcon.Text = icon;
            lblStatusIcon.ForeColor = color;
        }

        // ------------------------------------------------------------------ //
        //  UI FACTORY METODLARI
        // ------------------------------------------------------------------ //
        private static void StyleRoundedBorder(Panel panel)
        {
            panel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(99, 102, 241), 2)
                {
                    DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
                };
                e.Graphics.DrawRectangle(pen, new Rectangle(1, 1, panel.Width - 3, panel.Height - 3));
            };
        }

        private static void AddHoverEffect(Button btn, Color normalColor, Color hoverColor)
        {
            btn.MouseEnter += (s, e) => btn.BackColor = hoverColor;
            btn.MouseLeave += (s, e) => btn.BackColor = normalColor;
        }

        // ------------------------------------------------------------------ //
        //  DESIGNER STUB
        // ------------------------------------------------------------------ //
        private void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(620, 750);
            ResumeLayout(false);
        }
    }
}