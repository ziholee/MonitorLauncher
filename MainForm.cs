using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonitorLauncher
{
    public partial class MainForm : Form
    {
        private ComboBox? cmbMonitors;
        private TextBox? txtExecutablePath;
        private TextBox? txtArguments;
        private ComboBox? cmbWindowState;
        private Button? btnBrowse;
        private Button? btnLaunch;
        private Button? btnRefreshMonitors;
        private Button? btnSaveProfile;
        private Button? btnDeleteProfile;
        private ListBox? lstProfiles;
        private Label? lblStatus;
        private List<Profile> profiles = new List<Profile>();
        private string profilesFilePath = Profile.GetProfilesFilePath();

        public MainForm()
        {
            InitializeComponent();
            LoadProfiles();
            RefreshMonitorList();
            RefreshProfileList();
        }

        private void InitializeComponent()
        {
            this.Text = "Monitor Launcher v1.0";
            this.Size = new Size(600, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            int yPos = 20;
            int labelWidth = 120;
            int controlWidth = 400;
            int spacing = 35;

            // 모니터 선택
            var lblMonitor = new Label
            {
                Text = "모니터:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblMonitor);

            cmbMonitors = new ComboBox
            {
                Location = new Point(150, yPos),
                Size = new Size(controlWidth - 50, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(cmbMonitors);

            btnRefreshMonitors = new Button
            {
                Text = "새로고침",
                Location = new Point(500, yPos),
                Size = new Size(70, 23)
            };
            btnRefreshMonitors.Click += BtnRefreshMonitors_Click;
            this.Controls.Add(btnRefreshMonitors);

            yPos += spacing;

            // 실행 파일 경로
            var lblExecutable = new Label
            {
                Text = "실행 파일:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblExecutable);

            txtExecutablePath = new TextBox
            {
                Location = new Point(150, yPos),
                Size = new Size(controlWidth - 50, 23)
            };
            this.Controls.Add(txtExecutablePath);

            btnBrowse = new Button
            {
                Text = "찾아보기...",
                Location = new Point(500, yPos),
                Size = new Size(70, 23)
            };
            btnBrowse.Click += BtnBrowse_Click;
            this.Controls.Add(btnBrowse);

            yPos += spacing;

            // 실행 인자
            var lblArguments = new Label
            {
                Text = "인자 (선택):",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblArguments);

            txtArguments = new TextBox
            {
                Location = new Point(150, yPos),
                Size = new Size(controlWidth, 23)
            };
            this.Controls.Add(txtArguments);

            yPos += spacing;

            // 창 상태
            var lblWindowState = new Label
            {
                Text = "창 상태:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblWindowState);

            cmbWindowState = new ComboBox
            {
                Location = new Point(150, yPos),
                Size = new Size(controlWidth, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbWindowState.Items.AddRange(new[] { "전체화면", "창모드", "복원" });
            cmbWindowState.SelectedIndex = 0;
            this.Controls.Add(cmbWindowState);

            yPos += spacing + 10;

            // 실행 버튼
            btnLaunch = new Button
            {
                Text = "실행",
                Location = new Point(150, yPos),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnLaunch.Click += BtnLaunch_Click;
            this.Controls.Add(btnLaunch);

            yPos += spacing + 20;

            // 프로필 섹션
            var lblProfiles = new Label
            {
                Text = "프로필 (즐겨찾기):",
                Location = new Point(20, yPos),
                Size = new Size(200, 23),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(lblProfiles);

            yPos += 25;

            lstProfiles = new ListBox
            {
                Location = new Point(20, yPos),
                Size = new Size(controlWidth + 30, 120)
            };
            lstProfiles.SelectedIndexChanged += LstProfiles_SelectedIndexChanged;
            lstProfiles.DoubleClick += LstProfiles_DoubleClick;
            this.Controls.Add(lstProfiles);

            yPos += 130;

            // 프로필 관리 버튼
            btnSaveProfile = new Button
            {
                Text = "프로필 저장",
                Location = new Point(20, yPos),
                Size = new Size(100, 30)
            };
            btnSaveProfile.Click += BtnSaveProfile_Click;
            this.Controls.Add(btnSaveProfile);

            btnDeleteProfile = new Button
            {
                Text = "프로필 삭제",
                Location = new Point(130, yPos),
                Size = new Size(100, 30)
            };
            btnDeleteProfile.Click += BtnDeleteProfile_Click;
            this.Controls.Add(btnDeleteProfile);

            yPos += 40;

            // 상태 표시
            lblStatus = new Label
            {
                Text = "준비됨",
                Location = new Point(20, yPos),
                Size = new Size(550, 23),
                ForeColor = Color.Gray
            };
            this.Controls.Add(lblStatus);
        }

        private void RefreshMonitorList()
        {
            if (cmbMonitors == null) return;

            cmbMonitors.Items.Clear();
            var screens = Screen.AllScreens;

            foreach (var screen in screens)
            {
                string displayName = screen.Primary ? $"{screen.DeviceName} (Primary) - {screen.Bounds.Width}x{screen.Bounds.Height}"
                    : $"{screen.DeviceName} - {screen.Bounds.Width}x{screen.Bounds.Height}";
                cmbMonitors.Items.Add(displayName);
            }

            if (cmbMonitors.Items.Count > 0)
                cmbMonitors.SelectedIndex = 0;
        }

        private void BtnRefreshMonitors_Click(object? sender, EventArgs e)
        {
            RefreshMonitorList();
            UpdateStatus("모니터 목록이 새로고침되었습니다.");
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "실행 파일 (*.exe)|*.exe|모든 파일 (*.*)|*.*",
                Title = "실행 파일 선택"
            };

            if (dialog.ShowDialog() == DialogResult.OK && txtExecutablePath != null)
            {
                txtExecutablePath.Text = dialog.FileName;
            }
        }

        private async void BtnLaunch_Click(object? sender, EventArgs e)
        {
            if (cmbMonitors == null || txtExecutablePath == null || cmbWindowState == null)
                return;

            if (string.IsNullOrWhiteSpace(txtExecutablePath.Text))
            {
                MessageBox.Show("실행 파일을 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(txtExecutablePath.Text))
            {
                MessageBox.Show("선택한 파일이 존재하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (cmbMonitors.SelectedIndex < 0)
            {
                MessageBox.Show("모니터를 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var targetScreen = Screen.AllScreens[cmbMonitors.SelectedIndex];
            var windowState = cmbWindowState.SelectedIndex switch
            {
                0 => AppWindowState.Maximized,
                1 => AppWindowState.Normal,
                2 => AppWindowState.Restore,
                _ => AppWindowState.Maximized
            };

            btnLaunch!.Enabled = false;
            UpdateStatus("프로그램 실행 중...");

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = txtExecutablePath.Text,
                    UseShellExecute = false
                };

                if (txtArguments != null && !string.IsNullOrWhiteSpace(txtArguments.Text))
                    startInfo.Arguments = txtArguments.Text;

                var process = Process.Start(startInfo);
                if (process == null)
                {
                    UpdateStatus("프로그램 실행 실패");
                    return;
                }

                await Task.Delay(200); // 프로세스 초기화 대기

                bool success = await WindowController.MoveWindowToMonitor(process, targetScreen, windowState);

                if (success)
                    UpdateStatus($"프로그램이 {targetScreen.DeviceName}에서 실행되었습니다.");
                else
                    UpdateStatus("프로그램은 실행되었지만 창 위치 제어에 실패했습니다.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"오류: {ex.Message}");
            }
            finally
            {
                btnLaunch.Enabled = true;
            }
        }

        private void LoadProfiles()
        {
            try
            {
                profiles = Profile.LoadProfiles(profilesFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로필 로드 실패: {ex.Message}", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                profiles = new List<Profile>();
            }
        }

        private void RefreshProfileList()
        {
            if (lstProfiles == null) return;

            lstProfiles.Items.Clear();
            foreach (var profile in profiles)
            {
                lstProfiles.Items.Add(profile.Name);
            }
        }

        private void LstProfiles_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstProfiles == null || lstProfiles.SelectedIndex < 0)
                return;

            var profile = profiles[lstProfiles.SelectedIndex];
            if (txtExecutablePath != null)
                txtExecutablePath.Text = profile.ExecutablePath;
            if (txtArguments != null)
                txtArguments.Text = profile.Arguments;
            if (cmbWindowState != null)
                cmbWindowState.SelectedIndex = profile.WindowState switch
                {
                    AppWindowState.Maximized => 0,
                    AppWindowState.Normal => 1,
                    AppWindowState.Restore => 2,
                    _ => 0
                };

            // 모니터 선택
            if (cmbMonitors != null)
            {
                var screens = Screen.AllScreens;
                for (int i = 0; i < screens.Length; i++)
                {
                    if (screens[i].DeviceName == profile.MonitorDeviceName)
                    {
                        cmbMonitors.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private async void LstProfiles_DoubleClick(object? sender, EventArgs e)
        {
            if (lstProfiles == null || lstProfiles.SelectedIndex < 0)
                return;

            var profile = profiles[lstProfiles.SelectedIndex];
            if (!File.Exists(profile.ExecutablePath))
            {
                MessageBox.Show("저장된 실행 파일이 존재하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var screens = Screen.AllScreens;
            Screen? targetScreen = null;
            foreach (var screen in screens)
            {
                if (screen.DeviceName == profile.MonitorDeviceName)
                {
                    targetScreen = screen;
                    break;
                }
            }

            if (targetScreen == null)
            {
                MessageBox.Show("저장된 모니터를 찾을 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (btnLaunch != null)
                btnLaunch.Enabled = false;
            UpdateStatus("프로필에서 프로그램 실행 중...");

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = profile.ExecutablePath,
                    UseShellExecute = false
                };

                if (!string.IsNullOrWhiteSpace(profile.Arguments))
                    startInfo.Arguments = profile.Arguments;

                var process = Process.Start(startInfo);
                if (process == null)
                {
                    UpdateStatus("프로그램 실행 실패");
                    return;
                }

                await Task.Delay(200);
                bool success = await WindowController.MoveWindowToMonitor(process, targetScreen, profile.WindowState);

                if (success)
                    UpdateStatus($"프로필 '{profile.Name}' 실행 완료");
                else
                    UpdateStatus("프로그램은 실행되었지만 창 위치 제어에 실패했습니다.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"오류: {ex.Message}");
            }
            finally
            {
                if (btnLaunch != null)
                    btnLaunch.Enabled = true;
            }
        }

        private void BtnSaveProfile_Click(object? sender, EventArgs e)
        {
            if (cmbMonitors == null || txtExecutablePath == null || cmbWindowState == null)
                return;

            if (string.IsNullOrWhiteSpace(txtExecutablePath.Text))
            {
                MessageBox.Show("실행 파일을 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbMonitors.SelectedIndex < 0)
            {
                MessageBox.Show("모니터를 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dialog = new InputDialog("프로필 이름을 입력하세요:");
            if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                var targetScreen = Screen.AllScreens[cmbMonitors.SelectedIndex];
                var profile = new Profile
                {
                    Name = dialog.InputText,
                    ExecutablePath = txtExecutablePath.Text,
                    Arguments = txtArguments?.Text ?? string.Empty,
                    MonitorDeviceName = targetScreen.DeviceName,
                    WindowState = cmbWindowState.SelectedIndex switch
                    {
                        0 => AppWindowState.Maximized,
                        1 => AppWindowState.Normal,
                        2 => AppWindowState.Restore,
                        _ => AppWindowState.Maximized
                    }
                };

                var existing = profiles.FirstOrDefault(p => p.Name == profile.Name);
                if (existing != null)
                {
                    profiles.Remove(existing);
                }

                profiles.Add(profile);

                try
                {
                    Profile.SaveProfiles(profiles, profilesFilePath);
                    RefreshProfileList();
                    UpdateStatus($"프로필 '{profile.Name}'이 저장되었습니다.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"프로필 저장 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnDeleteProfile_Click(object? sender, EventArgs e)
        {
            if (lstProfiles == null || lstProfiles.SelectedIndex < 0)
            {
                MessageBox.Show("삭제할 프로필을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var profile = profiles[lstProfiles.SelectedIndex];
            if (MessageBox.Show($"프로필 '{profile.Name}'을(를) 삭제하시겠습니까?", "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                profiles.RemoveAt(lstProfiles.SelectedIndex);
                try
                {
                    Profile.SaveProfiles(profiles, profilesFilePath);
                    RefreshProfileList();
                    UpdateStatus($"프로필 '{profile.Name}'이 삭제되었습니다.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"프로필 삭제 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateStatus(string message)
        {
            if (lblStatus != null)
            {
                lblStatus.Text = message;
                lblStatus.Refresh();
            }
        }
    }

    public class InputDialog : Form
    {
        public string InputText { get; private set; } = string.Empty;
        private TextBox? textBox;

        public InputDialog(string prompt)
        {
            this.Text = "프로필 이름";
            this.Size = new Size(400, 120);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            var lblPrompt = new Label
            {
                Text = prompt,
                Location = new Point(10, 10),
                Size = new Size(370, 20)
            };
            this.Controls.Add(lblPrompt);

            textBox = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(370, 23)
            };
            this.Controls.Add(textBox);

            var btnOk = new Button
            {
                Text = "확인",
                DialogResult = DialogResult.OK,
                Location = new Point(220, 65),
                Size = new Size(75, 30)
            };
            btnOk.Click += (s, e) => { InputText = textBox.Text; };
            this.Controls.Add(btnOk);

            var btnCancel = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Location = new Point(305, 65),
                Size = new Size(75, 30)
            };
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
}
