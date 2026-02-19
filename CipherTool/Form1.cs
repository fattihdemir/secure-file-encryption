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
        //  UI KONTROLLERI
        // ------------------------------------------------------------------ //
        private Panel pnlHeader;
        private Label lblTitle;
        private Label lblSubtitle;

        private Panel pnlDropZone;
        private Label lblDropHint;
        private Label lblFileName;
        private Button btnBrowse;

        private Panel pnlPassword;
        private Label lblPassword;
        private TextBox txtPassword;
        private CheckBox chkShowPassword;

        private Button btnEncrypt;
        private Button btnDecrypt;
        private Button btnClear;

        private Panel pnlStatus;
        private Label lblStatusIcon;
        private Label lblStatusText;

        private Panel pnlFooter;
        private Label lblFooter;

        // Renk paleti
        private static readonly Color ColorBg = Color.FromArgb(18, 18, 30);
        private static readonly Color ColorPanel = Color.FromArgb(28, 28, 45);
        private static readonly Color ColorAccent = Color.FromArgb(99, 102, 241); // indigo
        private static readonly Color ColorSuccess = Color.FromArgb(34, 197, 94);
        private static readonly Color ColorDanger = Color.FromArgb(239, 68, 68);
        private static readonly Color ColorWarning = Color.FromArgb(234, 179, 8);
        private static readonly Color ColorText = Color.FromArgb(226, 232, 240);
        private static readonly Color ColorMuted = Color.FromArgb(100, 116, 139);
        private static readonly Color ColorBorder = Color.FromArgb(51, 65, 85);

        // Seçili dosya yolu
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
            Size = new Size(620, 700);
            MinimumSize = new Size(620, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ColorBg;
            ForeColor = ColorText;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9.5f);

            // ── HEADER ────────────────────────────────────────────────────
            pnlHeader = CreatePanel(ColorPanel, new Rectangle(0, 0, 620, 80));
            pnlHeader.BorderStyle = BorderStyle.None;

            lblTitle = new Label
            {
                Text = "🔐  CipherTool",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = ColorAccent,
                AutoSize = true,
                Location = new Point(24, 12)
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

            // ── DROP ZONE ─────────────────────────────────────────────────
            pnlDropZone = CreatePanel(ColorPanel, new Rectangle(24, 96, 572, 160));
            pnlDropZone.BorderStyle = BorderStyle.FixedSingle;
            pnlDropZone.AllowDrop = true;
            StyleRoundedBorder(pnlDropZone);

            lblDropHint = new Label
            {
                Text = "📂   Dosyayı buraya sürükleyip bırakın",
                Font = new Font("Segoe UI", 12f),
                ForeColor = ColorMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
                AutoSize = false,
                Size = new Size(572, 70),
                Location = new Point(0, 30)
            };

            lblFileName = new Label
            {
                Text = string.Empty,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = ColorSuccess,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(572, 24),
                Location = new Point(0, 100)
            };

            btnBrowse = CreateButton("Dosya Seç (Browse)", ColorAccent, new Rectangle(196, 108, 180, 36));
            btnBrowse.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            pnlDropZone.Controls.AddRange(new Control[] { lblDropHint, lblFileName, btnBrowse });

            // ── PAROLA ────────────────────────────────────────────────────
            pnlPassword = CreatePanel(ColorPanel, new Rectangle(24, 272, 572, 100));

            lblPassword = new Label
            {
                Text = "Parola",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = ColorMuted,
                AutoSize = true,
                Location = new Point(16, 14)
            };

            txtPassword = new TextBox
            {
                UseSystemPasswordChar = true,
                Font = new Font("Segoe UI", 11f),
                BackColor = ColorBg,
                ForeColor = ColorText,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(16, 36),
                Size = new Size(534, 30),
                PlaceholderText = "En az 8 karakter girin..."
            };

            chkShowPassword = new CheckBox
            {
                Text = "Parolayı göster",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = ColorMuted,
                AutoSize = true,
                Location = new Point(16, 68)
            };

            pnlPassword.Controls.AddRange(new Control[] { lblPassword, txtPassword, chkShowPassword });

            // ── BUTONLAR ──────────────────────────────────────────────────
            btnEncrypt = CreateButton("🔒  Şifrele", ColorAccent, new Rectangle(24, 388, 174, 46));
            btnDecrypt = CreateButton("🔓  Çöz", Color.FromArgb(30, 100, 80), new Rectangle(210, 388, 174, 46));
            btnClear = CreateButton("✕  Temizle", Color.FromArgb(60, 40, 40), new Rectangle(396, 388, 200, 46));

            btnEncrypt.ForeColor = Color.White;
            btnDecrypt.ForeColor = Color.White;
            btnClear.ForeColor = Color.White;

            // ── DURUM PANELİ ──────────────────────────────────────────────
            pnlStatus = CreatePanel(Color.FromArgb(25, 25, 40), new Rectangle(24, 450, 572, 130));
            pnlStatus.BorderStyle = BorderStyle.FixedSingle;

            lblStatusIcon = new Label
            {
                Text = "ℹ",
                Font = new Font("Segoe UI", 22f),
                ForeColor = ColorMuted,
                AutoSize = true,
                Location = new Point(16, 14)
            };

            lblStatusText = new Label
            {
                Text = "Bir dosya seçin ve parolanızı girin, ardından işlem butonuna basın.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = ColorMuted,
                AutoSize = false,
                Size = new Size(510, 100),
                Location = new Point(58, 14),
                MaximumSize = new Size(510, 0)
            };

            pnlStatus.Controls.AddRange(new Control[] { lblStatusIcon, lblStatusText });

            // ── FOOTER ────────────────────────────────────────────────────
            pnlFooter = CreatePanel(ColorPanel, new Rectangle(0, 620, 620, 40));
            lblFooter = new Label
            {
                Text = "AES-256-CBC · PBKDF2-SHA256 · 200.000 İterasyon · Random Salt + IV",
                Font = new Font("Segoe UI", 8f),
                ForeColor = ColorMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Dock = DockStyle.Fill
            };
            pnlFooter.Controls.Add(lblFooter);

            // ── FORMA EKLE ────────────────────────────────────────────────
            Controls.AddRange(new Control[]
            {
                pnlHeader, pnlDropZone, pnlPassword,
                btnEncrypt, btnDecrypt, btnClear,
                pnlStatus, pnlFooter
            });
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

            chkShowPassword.CheckedChanged += (s, e) =>
                txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;

            // Drag & Drop
            pnlDropZone.DragEnter += PnlDropZone_DragEnter;
            pnlDropZone.DragDrop += PnlDropZone_DragDrop;
            pnlDropZone.DragLeave += (s, e) => ResetDropZoneBorder();
            lblDropHint.DragEnter += PnlDropZone_DragEnter;
            lblDropHint.DragDrop += PnlDropZone_DragDrop;

            // Hover efekti
            AddHoverEffect(btnEncrypt, ColorAccent, Color.FromArgb(79, 82, 221));
            AddHoverEffect(btnDecrypt, Color.FromArgb(30, 100, 80), Color.FromArgb(22, 163, 74));
            AddHoverEffect(btnClear, Color.FromArgb(60, 40, 40), Color.FromArgb(127, 29, 29));
            AddHoverEffect(btnBrowse, ColorAccent, Color.FromArgb(79, 82, 221));
        }

        // ------------------------------------------------------------------ //
        //  DRAG & DROP HANDLERS
        // ------------------------------------------------------------------ //
        private void PnlDropZone_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
                pnlDropZone.BackColor = Color.FromArgb(35, 40, 65); // vurgu
            }
            else
                e.Effect = DragDropEffects.None;
        }

        private void PnlDropZone_DragDrop(object? sender, DragEventArgs e)
        {
            ResetDropZoneBorder();
            string[]? files = (string[]?)e.Data?.GetData(DataFormats.FileDrop);
            if (files?.Length > 0)
                LoadFile(files[0]);
        }

        private void ResetDropZoneBorder() =>
            pnlDropZone.BackColor = ColorPanel;

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
                SetStatus(
                    $"✅ Başarıyla şifrelendi!\n📄 Çıktı: {Path.GetFileName(output)}",
                    ColorSuccess, "✅");
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
                SetStatus(
                    $"✅ Başarıyla çözüldü!\n📄 Çıktı: {Path.GetFileName(output)}",
                    ColorSuccess, "✅");
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
            SetStatus("Bir dosya seçin ve parolanızı girin, ardından işlem butonuna basın.",
                      ColorMuted, "ℹ");
        }

        // ------------------------------------------------------------------ //
        //  YARDIMCI METODLAR
        // ------------------------------------------------------------------ //
        private void LoadFile(string path)
        {
            _selectedFilePath = path;
            string name = Path.GetFileName(path);
            lblFileName.Text = $"📄 {name}";
            lblFileName.ForeColor = ColorSuccess;

            // .cipher ise uyarı ver
            if (path.EndsWith(".cipher", StringComparison.OrdinalIgnoreCase))
                SetStatus($"Cipher dosyası seçildi: {name}\nŞifre çözmek için 'Çöz' butonuna basın.",
                          ColorWarning, "⚠️");
            else
                SetStatus($"Dosya seçildi: {name}\nParolanızı girin ve 'Şifrele' butonuna basın.",
                          ColorAccent, "📂");
        }

        private bool ValidateForOperation()
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                SetStatus("⚠️ Lütfen önce bir dosya seçin.", ColorWarning, "⚠️");
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtPassword.Text) || txtPassword.Text.Length < 8)
            {
                SetStatus("⚠️ Parola en az 8 karakter olmalıdır.", ColorWarning, "⚠️");
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
        private static Panel CreatePanel(Color backColor, Rectangle bounds)
        {
            return new Panel
            {
                BackColor = backColor,
                Location = new Point(bounds.X, bounds.Y),
                Size = new Size(bounds.Width, bounds.Height)
            };
        }

        private static Button CreateButton(string text, Color backColor, Rectangle bounds)
        {
            return new Button
            {
                Text = text,
                BackColor = backColor,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(bounds.X, bounds.Y),
                Size = new Size(bounds.Width, bounds.Height),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
        }

        private static void StyleRoundedBorder(Panel panel)
        {
            // Kesik çizgili kenarlık simüle et (gerçek rounded corner WinForms'da daha karmaşık)
            panel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(99, 102, 241), 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                e.Graphics.DrawRectangle(pen, new Rectangle(1, 1, panel.Width - 3, panel.Height - 3));
            };
        }

        private static void AddHoverEffect(Button btn, Color normalColor, Color hoverColor)
        {
            btn.MouseEnter += (s, e) => btn.BackColor = hoverColor;
            btn.MouseLeave += (s, e) => btn.BackColor = normalColor;
        }

        // ------------------------------------------------------------------ //
        //  DESIGNER STUB (InitializeComponent)
        // ------------------------------------------------------------------ //
        private void InitializeComponent()
        {
            // Designer.cs içini boşalttığımız için burada Form özelliklerini set ediyoruz
            SuspendLayout();
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(620, 660);
            ResumeLayout(false);
        }
    }
}