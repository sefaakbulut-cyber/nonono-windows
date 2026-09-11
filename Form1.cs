using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NoNoNo
{
    public partial class Form1 : Form
    {
        private const string CURRENT_VERSION = "1.0.1";
        private const string GITHUB_REPO_URL = "https://github.com/sefaakbulut-cyber/nonono-windows";
        private const string GITHUB_API_RELEASE_URL = "https://api.github.com/repos/sefaakbulut-cyber/nonono-windows/releases/latest";

        private const string DNS_IP = "163.192.96.101";
        private const string DOH_URL = "https://nonono.sefaakbulut.com/dns-query";
        private const string HOSTS_ENTRY = "163.192.96.101 nonono.sefaakbulut.com";

        private string currentLang = "EN";
        private bool isCurrentlyActive = false;

        private class LogItem
        {
            public DateTime Timestamp { get; set; }
            public string MessageEN { get; set; } = string.Empty;
            public string MessageTR { get; set; } = string.Empty;
            public string HexColor { get; set; } = "#8b949e";
        }

        private readonly List<LogItem> logHistory = new();

        private Label lblStatus = null!;
        private Button btnEnable = null!;
        private Button btnDisable = null!;
        private Button btnLang = null!;
        private Button btnInfo = null!;
        private Button btnGithub = null!;
        private RichTextBox txtLog = null!;

        public Form1()
        {
            if (!IsAdmin())
            {
                ElevateAndRestart();
                Environment.Exit(0);
                return;
            }

            InitializeCustomComponents();
            CheckCurrentStatus();
            _ = CheckForUpdatesAsync();
        }

        private bool IsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void ElevateAndRestart()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo);
            }
            catch
            {
                MessageBox.Show(
                    currentLang == "EN" ? "Administrator privileges are required to run this app." : "Bu uygulamanın çalışabilmesi için Yönetici izni gereklidir.",
                    currentLang == "EN" ? "Permission Error" : "Yetki Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        private void InitializeCustomComponents()
        {
            try
            {
                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }
            this.Text = $"NoNoNo v{CURRENT_VERSION}";
            this.Size = new Size(520, 465);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(13, 17, 23);

            // 1. Durum Göstergesi
            lblStatus = new Label
            {
                Location = new Point(20, 15),
                Size = new Size(385, 35),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = Color.White
            };
            this.Controls.Add(lblStatus);

            // 2. Dil Değiştirme Butonu
            btnLang = new Button
            {
                Location = new Point(415, 15),
                Size = new Size(70, 35),
                BackColor = Color.FromArgb(33, 38, 45),
                ForeColor = Color.FromArgb(88, 166, 255),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLang.FlatAppearance.BorderColor = Color.FromArgb(48, 54, 61);
            btnLang.Click += BtnLang_Click;
            this.Controls.Add(btnLang);

            // 3. Aktif Et Butonu
            btnEnable = new Button
            {
                Location = new Point(20, 60),
                Size = new Size(225, 45),
                BackColor = Color.FromArgb(35, 134, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEnable.FlatAppearance.BorderSize = 0;
            btnEnable.Click += BtnEnable_Click;
            this.Controls.Add(btnEnable);

            // 4. Deaktif Et Butonu
            btnDisable = new Button
            {
                Location = new Point(260, 60),
                Size = new Size(225, 45),
                BackColor = Color.FromArgb(218, 54, 51),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDisable.FlatAppearance.BorderSize = 0;
            btnDisable.Click += BtnDisable_Click;
            this.Controls.Add(btnDisable);

            // 5. Konsol Ekranı
            txtLog = new RichTextBox
            {
                Location = new Point(20, 120),
                Size = new Size(465, 240),
                BackColor = Color.FromArgb(22, 27, 34),
                ForeColor = Color.FromArgb(201, 209, 217),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Consolas", 9.5f),
                Padding = new Padding(10)
            };
            this.Controls.Add(txtLog);

            // 6. Teknik Bilgilendirme Butonu
            btnInfo = new Button
            {
                Location = new Point(20, 375),
                Size = new Size(350, 32),
                BackColor = Color.FromArgb(22, 27, 34),
                ForeColor = Color.FromArgb(139, 148, 158),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btnInfo.FlatAppearance.BorderColor = Color.FromArgb(48, 54, 61);
            btnInfo.Click += BtnInfo_Click;
            this.Controls.Add(btnInfo);

            // 7. GitHub Butonu (Güven ve Şeffaflık)
            btnGithub = new Button
            {
                Location = new Point(380, 375),
                Size = new Size(105, 32),
                BackColor = Color.FromArgb(33, 38, 45),
                ForeColor = Color.FromArgb(88, 166, 255),
                FlatStyle = FlatStyle.Flat,
                Text = "⭐ GitHub",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGithub.FlatAppearance.BorderColor = Color.FromArgb(48, 54, 61);
            btnGithub.Click += BtnGithub_Click;
            this.Controls.Add(btnGithub);

            UpdateLanguageUI();
        }

        private void BtnLang_Click(object? sender, EventArgs e)
        {
            currentLang = currentLang == "EN" ? "TR" : "EN";
            UpdateLanguageUI();
            RenderAllLogs();
        }

        private void BtnGithub_Click(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = GITHUB_REPO_URL,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void BtnInfo_Click(object? sender, EventArgs e)
        {
            string title = currentLang == "EN" ? "How It Works (Technical Overview)" : "Teknik Çalışma Mantığı";
            string message = currentLang == "EN"
                ? "This application configures Windows' native Encrypted DNS (DNS over HTTPS - DoH) feature:\n\n" +
                  "1. Bootstrap Entry (Hosts File):\n" +
                  "Adds the server domain to system hosts file to prevent DNS deadlocks.\n\n" +
                  "2. DNS Cache Flush:\n" +
                  "Executes 'ipconfig /flushdns' to clear cached DNS records.\n\n" +
                  "3. Registering DoH Template:\n" +
                  "Registers DoH template via 'netsh' with HTTP/2 protocol support.\n\n" +
                  "4. Routing Network Adapters:\n" +
                  "Routes IPv4 DNS queries on active network adapters to secure server IP (163.192.96.101).\n\n" +
                  "Source Code: https://github.com/sefaakbulut-cyber/nonono-windows"
                : "Bu uygulama Windows'un yerleşik Şifreli DNS (DoH) özelliğini yapılandırır:\n\n" +
                  "1. Adres Defteri (Hosts) Tanımlaması:\n" +
                  "Sunucu alan adını hosts dosyasına ekler.\n\n" +
                  "2. DNS Önbellek Temizliği:\n" +
                  "'ipconfig /flushdns' çalıştırarak eski çözümlenmiş DNS kayıtlarını temizler.\n\n" +
                  "3. DoH Şablon Kaydı:\n" +
                  "'netsh' komutuyla DoH şablonunu sisteme HTTP/2 protokolüyle kaydeder.\n\n" +
                  "4. Ağ Bağdaştırıcısı Yönlendirmesi:\n" +
                  "Aktif ağ kartlarının DNS adresini güvenli sunucu IP'sine (163.192.96.101) yönlendirir.\n\n" +
                  "Kaynak Kodlar: https://github.com/sefaakbulut-cyber/nonono-windows";

            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "NoNoNo-App");
                
                var response = await client.GetAsync(GITHUB_API_RELEASE_URL);
                if (response.IsSuccessStatusCode)
                {
                    using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                    if (doc.RootElement.TryGetProperty("tag_name", out var tagElement))
                    {
                        string latestVersion = tagElement.GetString()?.TrimStart('v') ?? "";
                        if (!string.IsNullOrEmpty(latestVersion) && latestVersion != CURRENT_VERSION)
                        {
                            WriteLog(
                                $"🔔 New version available (v{latestVersion})! Visit GitHub to update.",
                                $"🔔 Yeni bir sürüm mevcut (v{latestVersion})! Güncellemek için GitHub'ı ziyaret edin.",
                                "#f2cc60"
                            );
                        }
                    }
                }
            }
            catch { }
        }

        private void UpdateLanguageUI()
        {
            if (currentLang == "EN")
            {
                btnEnable.Text = "🛡️ Enable Secure DNS";
                btnDisable.Text = "⚡ Restore Defaults (Off)";
                btnLang.Text = "🌐 TR";
                btnInfo.Text = "ℹ️ How does this app work behind the scenes?";
            }
            else
            {
                btnEnable.Text = "🛡️ Güvenli DNS'i Aktif Et";
                btnDisable.Text = "⚡ Varsayılana Dön (Kapat)";
                btnLang.Text = "🌐 EN";
                btnInfo.Text = "ℹ️ Bu program arka planda nasıl çalışıyor?";
            }

            SetStatus(isCurrentlyActive);
        }

        private void BtnEnable_Click(object? sender, EventArgs e)
        {
            ClearLogHistory();
            WriteLog(
                "Encrypted DNS (DoH) activation started...",
                "Şifreli DNS (DoH) aktifleştirme işlemi başlatıldı...",
                "#f2cc60"
            );

            UpdateHosts(add: true);
            WriteLog("1/4 - Server address defined in system hosts file.", "1/4 - Sunucu adresi sistem adres defterine tanımlandı.", "#8b949e");

            RunSilentCommand("ipconfig", "/flushdns");
            WriteLog("2/4 - Windows DNS cache flushed and renewed.", "2/4 - Windows DNS önbelleği temizlendi ve yenilendi.", "#8b949e");

            RunSilentCommand("netsh", $"dns add encryption server={DNS_IP} dohtemplate=\"{DOH_URL}\" autoupgrade=yes udpfallback=yes");
            WriteLog("3/4 - Win 10/11 native HTTP/2 encrypted DNS (DoH) template registered.", "3/4 - Win 10/11 yerleşik HTTP/2 şifreli DNS (DoH) şablonu sisteme kaydoldu.", "#8b949e");

            var adapters = GetTargetAdapterNames();
            if (adapters.Count > 0)
            {
                foreach (var adapter in adapters)
                {
                    RunSilentCommand("powershell", $"-Command \"Set-DnsClientServerAddress -InterfaceAlias '{adapter}' -ServerAddresses '{DNS_IP}'\"");
                    WriteLog($"4/4 - [{adapter}] network adapter routed to secure server.", $"4/4 - [{adapter}] ağ bağdaştırıcısı güvenli sunucuya yönlendirildi.", "#8b949e");
                }
            }
            else
            {
                WriteLog("No active physical network adapter found.", "Aktif fiziksel ağ bağdaştırıcısı bulunamadı.", "#f85149");
            }

            SetStatus(active: true);
            WriteLog("Operation Successful! Your internet traffic is now encrypted and protected.", "İşlem Başarılı! İnternet trafiğiniz artık şifreli ve korumalı.", "#2ea44f");
        }

        private void BtnDisable_Click(object? sender, EventArgs e)
        {
            ClearLogHistory();
            WriteLog("Disabling Encrypted DNS...", "Şifreli DNS devre dışı bırakılıyor...", "#f2cc60");

            var adapters = GetTargetAdapterNames();
            if (adapters.Count > 0)
            {
                foreach (var adapter in adapters)
                {
                    RunSilentCommand("powershell", $"-Command \"Set-DnsClientServerAddress -InterfaceAlias '{adapter}' -ResetServerAddresses\"");
                    WriteLog($"1/3 - [{adapter}] network adapter restored to default DNS settings (DHCP).", $"1/3 - [{adapter}] ağ bağdaştırıcısı varsayılan DNS ayarlarına (DHCP) döndürüldü.", "#8b949e");
                }
            }

            UpdateHosts(add: false);
            WriteLog("2/3 - Server entry removed from system hosts file.", "2/3 - Sistem adres defterindeki sunucu kaydı temizlendi.", "#8b949e");

            RunSilentCommand("ipconfig", "/flushdns");
            WriteLog("3/3 - Windows DNS cache flushed.", "3/3 - Windows DNS önbelleği temizlendi.", "#8b949e");

            SetStatus(active: false);
            WriteLog("Restored to default settings successfully. Encrypted DNS disabled.", "Varsayılan ayarlara başarıyla dönüldü. Şifreli DNS kapatıldı.", "#2ea44f");
        }

        private void CheckCurrentStatus()
        {
            ClearLogHistory();
            var adapters = GetTargetAdapterNames();
            foreach (var adapter in adapters)
            {
                string output = RunSilentCommand("powershell", $"-Command \"(Get-DnsClientServerAddress -InterfaceAlias '{adapter}' -AddressFamily IPv4).ServerAddresses\"");
                if (output.Contains(DNS_IP))
                {
                    SetStatus(active: true);
                    WriteLog("Current Status: Encrypted DNS (DoH) is active.", "Mevcut Durum: Şifreli DNS (DoH) şu anda aktif.", "#2ea44f");
                    return;
                }
            }

            SetStatus(active: false);
            WriteLog("Current Status: Default DNS is in use (DoH Inactive).", "Mevcut Durum: Varsayılan DNS kullanılıyor (DoH Pasif).", "#8b949e");
        }

        private void SetStatus(bool active)
        {
            isCurrentlyActive = active;
            if (active)
            {
                lblStatus.Text = currentLang == "EN" ? "● STATUS: ACTIVE (Encrypted DoH Protection)" : "● DURUM: AKTİF (Güvenli DoH Koruması)";
                lblStatus.BackColor = Color.FromArgb(20, 60, 30);
                lblStatus.ForeColor = Color.FromArgb(46, 160, 79);
            }
            else
            {
                lblStatus.Text = currentLang == "EN" ? "○ STATUS: INACTIVE (Default DNS)" : "○ DURUM: PASİF (Varsayılan DNS)";
                lblStatus.BackColor = Color.FromArgb(40, 20, 20);
                lblStatus.ForeColor = Color.FromArgb(218, 54, 51);
            }
        }

        private void UpdateHosts(bool add)
        {
            try
            {
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                var lines = File.ReadAllLines(hostsPath).ToList();

                lines.RemoveAll(l => l.Contains("nonono.sefaakbulut.com"));

                if (add)
                {
                    lines.Add(HOSTS_ENTRY);
                }

                File.WriteAllLines(hostsPath, lines);
            }
            catch (Exception ex)
            {
                WriteLog($"Hosts Error: {ex.Message}", $"Hosts Hatası: {ex.Message}", "#f85149");
            }
        }

        private List<string> GetTargetAdapterNames()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                            !ni.Description.ToLower().Contains("virtual") &&
                            !ni.Description.ToLower().Contains("vpn") &&
                            !ni.Description.ToLower().Contains("tap") &&
                            !ni.Description.ToLower().Contains("tun") &&
                            !ni.Description.ToLower().Contains("wsl") &&
                            !ni.Description.ToLower().Contains("hyper-v") &&
                            !ni.Name.ToLower().Contains("vethernet"))
                .Select(ni => ni.Name)
                .ToList();
        }

        private string RunSilentCommand(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return string.Empty;

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void ClearLogHistory()
        {
            logHistory.Clear();
            txtLog.Clear();
        }

        private void WriteLog(string messageEN, string messageTR, string hexColor)
        {
            var item = new LogItem
            {
                Timestamp = DateTime.Now,
                MessageEN = messageEN,
                MessageTR = messageTR,
                HexColor = hexColor
            };

            logHistory.Add(item);
            AppendLogToUI(item);
        }

        private void RenderAllLogs()
        {
            txtLog.Clear();
            foreach (var item in logHistory)
            {
                AppendLogToUI(item);
            }
        }

        private void AppendLogToUI(LogItem item)
        {
            string message = currentLang == "EN" ? item.MessageEN : item.MessageTR;
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = ColorTranslator.FromHtml(item.HexColor);
            txtLog.AppendText($"[{item.Timestamp:HH:mm:ss}] {message}\n");
            txtLog.ScrollToCaret();
        }
    }
}