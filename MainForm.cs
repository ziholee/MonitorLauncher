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
        private Button? btnCaptureWorkspace;
        private Button? btnSaveWorkspace;
        private Button? btnLaunchWorkspace;
        private Button? btnDeleteWorkspace;
        private Button? btnGatherWindows;
        private TextBox? txtWorkspaceName;
        private ListBox? lstProfiles;
        private ListBox? lstWorkspaces;
        private CheckedListBox? chkCapturedWindows;
        private Label? lblStatus;
        private List<Profile> profiles = new List<Profile>();
        private List<WorkspaceProfile> workspaceProfiles = new List<WorkspaceProfile>();
        private string profilesFilePath = Profile.GetProfilesFilePath();
        private string workspacesFilePath = WorkspaceProfile.GetWorkspacesFilePath();
        private NotifyIcon? trayIcon;
        private ContextMenuStrip? trayMenu;
        private readonly AppLauncherService appLauncherService = new AppLauncherService();
        private readonly WindowCaptureService windowCaptureService = new WindowCaptureService();
        private readonly WorkspaceRestoreService workspaceRestoreService = new WorkspaceRestoreService();
        private bool isLoadingProfileSelection;
        private bool launchBlockedByUnresolvedProfileMonitor;
        private ToolStripMenuItem? trayProfilesMenuItem;
        private ToolStripMenuItem? trayWorkspacesMenuItem;
        private ToolTip? uiToolTip;
        private const int LayoutMinWidth = 980;
        private const int LayoutMinHeight = 860;

        public MainForm()
        {
            InitializeComponent();
            InitializeTrayIcon();
            LoadProfiles();
            LoadWorkspaces();
            RefreshMonitorList();
            RefreshProfileList();
            RefreshWorkspaceList();
            AdjustWindowToSelectedMonitor();
        }

        private void InitializeComponent()
        {
            this.Text = "Monitor Launcher v2.0.0";
            this.Size = new Size(LayoutMinWidth, LayoutMinHeight);
            this.MinimumSize = new Size(LayoutMinWidth, LayoutMinHeight);
            this.StartPosition = FormStartPosition.Manual;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(241, 243, 247);
            this.Icon = LoadApplicationIcon();
            this.Resize += MainForm_Resize;

            Font uiFont = new Font("Segoe UI", 9F, FontStyle.Regular);
            Font labelFont = new Font("Segoe UI", 9F, FontStyle.Bold);
            Font titleFont = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
            Font cardTitleFont = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            uiToolTip = new ToolTip();

            PictureBox? logoPicture = null;
            try
            {
                string logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "logo.png");
                if (File.Exists(logoPath))
                {
                    logoPicture = new PictureBox
                    {
                        Image = Image.FromFile(logoPath),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Location = new Point(28, 24),
                        Size = new Size(68, 68),
                        BackColor = Color.Transparent
                    };
                    this.Controls.Add(logoPicture);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MonitorLauncher] 로고 로드 실패: {ex}");
            }

            var lblTitle = new Label
            {
                Text = "Monitor Launcher",
                Location = new Point(logoPicture == null ? 28 : 112, 24),
                Size = new Size(360, 36),
                Font = titleFont,
                ForeColor = Color.FromArgb(32, 37, 45),
                BackColor = Color.Transparent
            };
            this.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "실행 설정과 프로필 관리를 분리해 빠르게 원하는 모니터로 프로그램을 실행합니다.",
                Location = new Point(logoPicture == null ? 28 : 114, 62),
                Size = new Size(620, 22),
                Font = uiFont,
                ForeColor = Color.FromArgb(99, 107, 122),
                BackColor = Color.Transparent
            };
            this.Controls.Add(lblSubtitle);

            var leftShadow = CreateShadowPanel(new Rectangle(28, 118, 430, 440));
            var leftCard = CreateCardPanel(new Rectangle(22, 112, 430, 440));
            var rightShadow = CreateShadowPanel(new Rectangle(486, 118, 466, 440));
            var rightCard = CreateCardPanel(new Rectangle(480, 112, 466, 440));
            this.Controls.Add(leftShadow);
            this.Controls.Add(rightShadow);
            this.Controls.Add(leftCard);
            this.Controls.Add(rightCard);
            leftShadow.SendToBack();
            rightShadow.SendToBack();
            leftCard.BringToFront();
            rightCard.BringToFront();

            var lblSettingsTitle = new Label
            {
                Text = "실행 설정",
                Location = new Point(26, 20),
                Size = new Size(140, 24),
                Font = cardTitleFont,
                ForeColor = Color.FromArgb(35, 41, 52),
                BackColor = Color.Transparent
            };
            leftCard.Controls.Add(lblSettingsTitle);

            var lblSettingsSubtitle = new Label
            {
                Text = "모니터, 실행 파일, 창 상태를 선택한 뒤 바로 실행합니다.",
                Location = new Point(26, 46),
                Size = new Size(360, 20),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(111, 118, 130),
                BackColor = Color.Transparent
            };
            leftCard.Controls.Add(lblSettingsSubtitle);

            var lblMonitor = CreateFieldLabel("모니터", new Point(26, 84), labelFont);
            leftCard.Controls.Add(lblMonitor);

            cmbMonitors = new ComboBox
            {
                Location = new Point(26, 108),
                Size = new Size(324, 31),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = uiFont
            };
            cmbMonitors.SelectedIndexChanged += CmbMonitors_SelectedIndexChanged;
            leftCard.Controls.Add(cmbMonitors);

            btnRefreshMonitors = CreateIconButton("\uE72C", new Point(362, 108), "모니터 새로고침");
            btnRefreshMonitors.Click += BtnRefreshMonitors_Click;
            leftCard.Controls.Add(btnRefreshMonitors);

            var lblExecutable = CreateFieldLabel("실행 파일", new Point(26, 154), labelFont);
            leftCard.Controls.Add(lblExecutable);

            var executableShell = CreateInputShell(new Point(26, 178), new Size(324, 38), out var executableTextBox);
            txtExecutablePath = executableTextBox;
            txtExecutablePath.TextChanged += ClearUnresolvedProfileLaunchBlock;
            leftCard.Controls.Add(executableShell);

            btnBrowse = CreateIconButton("\uE8B7", new Point(362, 178), "실행 파일 찾기");
            btnBrowse.Click += BtnBrowse_Click;
            leftCard.Controls.Add(btnBrowse);

            var lblArguments = CreateFieldLabel("인자 (선택)", new Point(26, 224), labelFont);
            leftCard.Controls.Add(lblArguments);

            var argumentsShell = CreateInputShell(new Point(26, 248), new Size(392, 38), out var argumentsTextBox);
            txtArguments = argumentsTextBox;
            txtArguments.TextChanged += ClearUnresolvedProfileLaunchBlock;
            leftCard.Controls.Add(argumentsShell);

            var lblWindowState = CreateFieldLabel("창 상태", new Point(26, 294), labelFont);
            leftCard.Controls.Add(lblWindowState);

            cmbWindowState = new ComboBox
            {
                Location = new Point(26, 318),
                Size = new Size(392, 31),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = uiFont
            };
            cmbWindowState.Items.AddRange(new[] { "전체화면", "창모드", "복원" });
            cmbWindowState.SelectedIndex = 0;
            cmbWindowState.SelectedIndexChanged += ClearUnresolvedProfileLaunchBlock;
            leftCard.Controls.Add(cmbWindowState);

            btnLaunch = new Button
            {
                Text = "실행",
                Location = new Point(26, 372),
                Size = new Size(392, 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLaunch.FlatAppearance.BorderSize = 0;
            btnLaunch.Click += BtnLaunch_Click;
            leftCard.Controls.Add(btnLaunch);

            var lblProfiles = new Label
            {
                Text = "프로필",
                Location = new Point(26, 20),
                Size = new Size(120, 24),
                Font = cardTitleFont,
                ForeColor = Color.FromArgb(35, 41, 52),
                BackColor = Color.Transparent
            };
            rightCard.Controls.Add(lblProfiles);

            var lblProfilesSubtitle = new Label
            {
                Text = "자주 쓰는 실행 구성을 저장하고 한 번에 다시 실행합니다.",
                Location = new Point(26, 46),
                Size = new Size(390, 20),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(111, 118, 130),
                BackColor = Color.Transparent
            };
            rightCard.Controls.Add(lblProfilesSubtitle);

            var profileListShell = new Panel
            {
                Location = new Point(26, 84),
                Size = new Size(414, 286),
                BackColor = Color.FromArgb(248, 249, 252),
                Padding = new Padding(8)
            };
            profileListShell.Paint += ProfileListShell_Paint;
            rightCard.Controls.Add(profileListShell);

            lstProfiles = new ListBox
            {
                Location = new Point(8, 8),
                Size = new Size(398, 270),
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 62,
                Font = uiFont,
                IntegralHeight = false,
                BackColor = Color.FromArgb(248, 249, 252),
                ForeColor = Color.FromArgb(35, 41, 52),
                HorizontalScrollbar = false
            };
            lstProfiles.SelectedIndexChanged += LstProfiles_SelectedIndexChanged;
            lstProfiles.DoubleClick += LstProfiles_DoubleClick;
            lstProfiles.DrawItem += LstProfiles_DrawItem;
            profileListShell.Controls.Add(lstProfiles);

            btnSaveProfile = new Button
            {
                Text = "프로필 저장",
                Location = new Point(26, 388),
                Size = new Size(198, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(237, 245, 255),
                ForeColor = Color.FromArgb(0, 120, 215),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSaveProfile.FlatAppearance.BorderColor = Color.FromArgb(188, 213, 245);
            btnSaveProfile.Click += BtnSaveProfile_Click;
            rightCard.Controls.Add(btnSaveProfile);

            btnDeleteProfile = new Button
            {
                Text = "프로필 삭제",
                Location = new Point(242, 388),
                Size = new Size(198, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(253, 241, 242),
                ForeColor = Color.FromArgb(194, 59, 75),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDeleteProfile.FlatAppearance.BorderColor = Color.FromArgb(238, 195, 200);
            btnDeleteProfile.Click += BtnDeleteProfile_Click;
            rightCard.Controls.Add(btnDeleteProfile);

            InitializeWorkspaceTab(uiFont, labelFont, cardTitleFont);

            lblStatus = new Label
            {
                Text = "준비됨",
                Location = new Point(30, 814),
                Size = new Size(920, 23),
                Font = uiFont,
                ForeColor = Color.FromArgb(105, 112, 126),
                BackColor = Color.Transparent
            };
            this.Controls.Add(lblStatus);
            lblStatus.BringToFront();
        }

        private void InitializeWorkspaceTab(Font uiFont, Font labelFont, Font cardTitleFont)
        {
            var workspaceTabs = new TabControl
            {
                Location = new Point(22, 574),
                Size = new Size(924, 220),
                Font = uiFont
            };

            var workspacePage = new TabPage("워크스페이스")
            {
                BackColor = Color.FromArgb(241, 243, 247)
            };
            workspaceTabs.TabPages.Add(workspacePage);
            this.Controls.Add(workspaceTabs);

            var lblWorkspaceTitle = new Label
            {
                Text = "워크스페이스",
                Location = new Point(18, 14),
                Size = new Size(160, 24),
                Font = cardTitleFont,
                ForeColor = Color.FromArgb(35, 41, 52),
                BackColor = Color.Transparent
            };
            workspacePage.Controls.Add(lblWorkspaceTitle);

            var lblWorkspaceName = CreateFieldLabel("이름", new Point(18, 48), labelFont);
            workspacePage.Controls.Add(lblWorkspaceName);

            var workspaceNameShell = CreateInputShell(new Point(18, 72), new Size(260, 34), out var workspaceNameTextBox);
            txtWorkspaceName = workspaceNameTextBox;
            workspacePage.Controls.Add(workspaceNameShell);

            btnCaptureWorkspace = new Button
            {
                Text = "현재 배치 가져오기",
                Location = new Point(294, 72),
                Size = new Size(150, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(61, 72, 91),
                Font = uiFont,
                Cursor = Cursors.Hand
            };
            btnCaptureWorkspace.Click += BtnCaptureWorkspace_Click;
            workspacePage.Controls.Add(btnCaptureWorkspace);

            btnSaveWorkspace = new Button
            {
                Text = "저장",
                Location = new Point(454, 72),
                Size = new Size(78, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(237, 245, 255),
                ForeColor = Color.FromArgb(0, 120, 215),
                Font = uiFont,
                Cursor = Cursors.Hand
            };
            btnSaveWorkspace.Click += BtnSaveWorkspace_Click;
            workspacePage.Controls.Add(btnSaveWorkspace);

            chkCapturedWindows = new CheckedListBox
            {
                Location = new Point(18, 114),
                Size = new Size(514, 72),
                CheckOnClick = true,
                Font = uiFont,
                IntegralHeight = false
            };
            workspacePage.Controls.Add(chkCapturedWindows);

            var lblSavedWorkspaces = CreateFieldLabel("저장된 워크스페이스", new Point(556, 18), labelFont);
            workspacePage.Controls.Add(lblSavedWorkspaces);

            lstWorkspaces = new ListBox
            {
                Location = new Point(556, 42),
                Size = new Size(332, 84),
                Font = uiFont,
                IntegralHeight = false
            };
            workspacePage.Controls.Add(lstWorkspaces);

            btnLaunchWorkspace = new Button
            {
                Text = "실행",
                Location = new Point(556, 138),
                Size = new Size(76, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = uiFont,
                Cursor = Cursors.Hand
            };
            btnLaunchWorkspace.Click += BtnLaunchWorkspace_Click;
            workspacePage.Controls.Add(btnLaunchWorkspace);

            btnDeleteWorkspace = new Button
            {
                Text = "삭제",
                Location = new Point(642, 138),
                Size = new Size(76, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(253, 241, 242),
                ForeColor = Color.FromArgb(194, 59, 75),
                Font = uiFont,
                Cursor = Cursors.Hand
            };
            btnDeleteWorkspace.Click += BtnDeleteWorkspace_Click;
            workspacePage.Controls.Add(btnDeleteWorkspace);

            btnGatherWindows = new Button
            {
                Text = "모든 창 주 모니터로",
                Location = new Point(728, 138),
                Size = new Size(160, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(61, 72, 91),
                Font = uiFont,
                Cursor = Cursors.Hand
            };
            btnGatherWindows.Click += BtnGatherWindows_Click;
            workspacePage.Controls.Add(btnGatherWindows);
        }

        private void RefreshMonitorList(bool selectDefaultWhenUnmatched = true)
        {
            if (cmbMonitors == null) return;

            var previousSelection = cmbMonitors.SelectedItem as MonitorOption;
            cmbMonitors.Items.Clear();
            var screens = Screen.AllScreens;
            int primaryIndex = -1;
            int restoredIndex = -1;

            for (int i = 0; i < screens.Length; i++)
            {
                var screen = screens[i];
                string displayName;
                
                if (screen.Primary)
                {
                    displayName = $"모니터 {i + 1} (기본) - {screen.Bounds.Width}x{screen.Bounds.Height}";
                    primaryIndex = i;
                }
                else
                {
                    displayName = $"모니터 {i + 1} - {screen.Bounds.Width}x{screen.Bounds.Height}";
                }

                var option = new MonitorOption(displayName, screen);
                int itemIndex = cmbMonitors.Items.Add(option);
                if (previousSelection != null && previousSelection.Matches(screen))
                {
                    restoredIndex = itemIndex;
                }
            }

            if (restoredIndex >= 0)
            {
                cmbMonitors.SelectedIndex = restoredIndex;
            }
            else if (selectDefaultWhenUnmatched && primaryIndex >= 0)
            {
                cmbMonitors.SelectedIndex = primaryIndex;
            }
            else if (selectDefaultWhenUnmatched && cmbMonitors.Items.Count > 0)
            {
                // Primary 모니터를 찾지 못한 경우 첫 번째 모니터 선택
                cmbMonitors.SelectedIndex = 0;
            }
            else
            {
                cmbMonitors.SelectedIndex = -1;
            }
        }

        private void CmbMonitors_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ClearUnresolvedProfileLaunchBlock(sender, e);

            // 모니터 선택 변경 시 창을 자동으로 이동하지 않음
            // (사용자가 창을 수동으로 이동한 경우를 고려)
        }

        private void AdjustWindowToSelectedMonitor()
        {
            if (cmbMonitors == null || cmbMonitors.SelectedIndex < 0)
                return;

            if (!TryResolveSelectedMonitor(out var selectedScreen) || selectedScreen == null)
                return;

            var screenBounds = selectedScreen.Bounds;

            const int screenPadding = 80;
            int availableWidth = Math.Max(screenBounds.Width - screenPadding, LayoutMinWidth);
            int availableHeight = Math.Max(screenBounds.Height - screenPadding, LayoutMinHeight);

            int windowWidth = Math.Max(Math.Min(availableWidth, 1180), LayoutMinWidth);
            int windowHeight = Math.Max(Math.Min(availableHeight, 760), LayoutMinHeight);

            // 창을 모니터 중앙에 배치
            int windowX = screenBounds.X + (screenBounds.Width - windowWidth) / 2;
            int windowY = screenBounds.Y + (screenBounds.Height - windowHeight) / 2;

            this.Size = new Size(windowWidth, windowHeight);
            this.Location = new Point(Math.Max(screenBounds.X, windowX), Math.Max(screenBounds.Y, windowY));
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
                Filter = "실행 가능 항목 (*.exe;*.lnk)|*.exe;*.lnk|모든 파일 (*.*)|*.*",
                Title = "실행 파일 선택"
            };

            if (dialog.ShowDialog() == DialogResult.OK && txtExecutablePath != null)
            {
                txtExecutablePath.Text = dialog.FileName;
            }
        }

        private async void BtnLaunch_Click(object? sender, EventArgs e)
        {
            if (btnLaunch == null)
                return;

            var request = BuildLaunchRequestFromInputs();
            if (request == null)
            {
                return;
            }

            if (launchBlockedByUnresolvedProfileMonitor)
            {
                MessageBox.Show("저장된 프로필 모니터를 찾지 못했습니다. 모니터를 다시 선택한 후 실행해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLaunch.Enabled = false;
            UpdateStatus("프로그램 실행 중...");

            try
            {
                await LaunchAndDisplayResultAsync(request);
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
                RebuildTrayProfilesMenu();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로필 로드 실패: {ex.Message}", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                profiles = new List<Profile>();
            }
        }

        private void LoadWorkspaces()
        {
            try
            {
                workspaceProfiles = WorkspaceProfile.LoadWorkspaces(workspacesFilePath);
                RebuildTrayWorkspacesMenu();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"워크스페이스 로드 실패: {ex.Message}", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                workspaceProfiles = new List<WorkspaceProfile>();
            }
        }

        private void RefreshProfileList()
        {
            if (lstProfiles == null) return;

            lstProfiles.Items.Clear();
            foreach (var profile in profiles)
            {
                lstProfiles.Items.Add(profile);
            }

            RebuildTrayProfilesMenu();
        }

        private void RefreshWorkspaceList()
        {
            if (lstWorkspaces == null)
            {
                RebuildTrayWorkspacesMenu();
                return;
            }

            lstWorkspaces.Items.Clear();
            foreach (var workspace in workspaceProfiles.OrderBy(workspace => workspace.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                lstWorkspaces.Items.Add(workspace);
            }

            RebuildTrayWorkspacesMenu();
        }

        private void LstProfiles_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstProfiles == null || lstProfiles.SelectedIndex < 0)
                return;

            if (lstProfiles.SelectedItem is not Profile profile)
                return;

            isLoadingProfileSelection = true;

            try
            {
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
            }
            finally
            {
                isLoadingProfileSelection = false;
            }

            launchBlockedByUnresolvedProfileMonitor = !TrySelectMonitorForProfile(profile);
        }

        private async void LstProfiles_DoubleClick(object? sender, EventArgs e)
        {
            if (lstProfiles == null || lstProfiles.SelectedIndex < 0 || btnLaunch == null)
                return;

            if (lstProfiles.SelectedItem is not Profile profile)
                return;

            await LaunchProfileAsync(profile, "프로필에서 프로그램 실행 중...");
        }

        private LaunchRequest? BuildLaunchRequestFromInputs()
        {
            if (cmbMonitors == null || txtExecutablePath == null || cmbWindowState == null)
                return null;

            if (string.IsNullOrWhiteSpace(txtExecutablePath.Text))
            {
                MessageBox.Show("실행 파일을 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            bool isNonFileUri = Uri.TryCreate(txtExecutablePath.Text, UriKind.Absolute, out var uri) && !uri.IsFile;
            if (!isNonFileUri && !File.Exists(txtExecutablePath.Text))
            {
                MessageBox.Show("선택한 파일이 존재하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            if (cmbMonitors.SelectedIndex < 0)
            {
                MessageBox.Show("모니터를 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            if (!TryGetSelectedMonitor("실행", out var targetScreen) || targetScreen == null)
            {
                return null;
            }

            return new LaunchRequest
            {
                ExecutablePath = txtExecutablePath.Text,
                Arguments = txtArguments?.Text ?? string.Empty,
                MonitorDeviceName = targetScreen.DeviceName,
                MonitorWasPrimary = targetScreen.Primary,
                MonitorBoundsX = targetScreen.Bounds.X,
                MonitorBoundsY = targetScreen.Bounds.Y,
                MonitorBoundsWidth = targetScreen.Bounds.Width,
                MonitorBoundsHeight = targetScreen.Bounds.Height,
                WindowState = cmbWindowState.SelectedIndex switch
                {
                    0 => AppWindowState.Maximized,
                    1 => AppWindowState.Normal,
                    2 => AppWindowState.Restore,
                    _ => AppWindowState.Maximized
                }
            };
        }

        private bool TryGetSelectedMonitor(string actionName, out Screen? targetScreen)
        {
            targetScreen = null;

            if (cmbMonitors == null)
            {
                return false;
            }

            if (cmbMonitors.SelectedIndex < 0)
            {
                MessageBox.Show("모니터를 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!TryResolveSelectedMonitor(out targetScreen))
            {
                string message = $"모니터 연결 상태가 변경되어 선택한 모니터를 {actionName}에 사용할 수 없습니다. 모니터 목록을 새로고침했습니다. 사용할 모니터를 다시 선택해주세요.";
                MessageBox.Show(message, "모니터 확인 필요", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdateStatus(message);
                RefreshMonitorList(selectDefaultWhenUnmatched: false);
                return false;
            }

            return true;
        }

        private bool TryResolveSelectedMonitor(out Screen? targetScreen)
        {
            targetScreen = null;

            if (cmbMonitors?.SelectedItem is not MonitorOption selectedMonitor)
            {
                return false;
            }

            foreach (var screen in Screen.AllScreens)
            {
                if (selectedMonitor.Matches(screen))
                {
                    targetScreen = screen;
                    return true;
                }
            }

            return false;
        }

        private static LaunchRequest BuildLaunchRequestFromProfile(Profile profile)
        {
            return new LaunchRequest
            {
                ExecutablePath = profile.ExecutablePath,
                Arguments = profile.Arguments,
                MonitorDeviceName = profile.MonitorDeviceName,
                MonitorWasPrimary = profile.MonitorWasPrimary,
                MonitorBoundsX = profile.MonitorBoundsX,
                MonitorBoundsY = profile.MonitorBoundsY,
                MonitorBoundsWidth = profile.MonitorBoundsWidth,
                MonitorBoundsHeight = profile.MonitorBoundsHeight,
                WindowState = profile.WindowState,
                ProfileName = profile.Name
            };
        }

        private async Task LaunchAndDisplayResultAsync(LaunchRequest request)
        {
            var result = await appLauncherService.LaunchAsync(request);

            if (result.FileMissing)
            {
                string message = string.IsNullOrWhiteSpace(request.ProfileName)
                    ? "선택한 파일이 존재하지 않습니다."
                    : "저장된 실행 파일이 존재하지 않습니다.";
                MessageBox.Show(message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (result.MonitorMissing)
            {
                MessageBox.Show("저장된 모니터를 찾을 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateStatus(result.StatusMessage);
        }

        private async Task LaunchProfileAsync(Profile profile, string statusMessage)
        {
            if (btnLaunch == null)
            {
                return;
            }

            var request = BuildLaunchRequestFromProfile(profile);
            btnLaunch.Enabled = false;
            UpdateStatus(statusMessage);

            try
            {
                await LaunchAndDisplayResultAsync(request);
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

        private bool TrySelectMonitorForProfile(Profile profile)
        {
            if (cmbMonitors == null)
            {
                return false;
            }

            var request = BuildLaunchRequestFromProfile(profile);
            if (!appLauncherService.TryResolveTargetScreen(request, out var resolvedScreen, out var usedFallback) || resolvedScreen == null)
            {
                UpdateStatus($"프로필 '{profile.Name}'의 저장된 모니터를 찾지 못했습니다. 모니터를 다시 선택해주세요.");
                return false;
            }

            for (int i = 0; i < cmbMonitors.Items.Count; i++)
            {
                if (cmbMonitors.Items[i] is MonitorOption option && option.Matches(resolvedScreen))
                {
                    cmbMonitors.SelectedIndex = i;
                    UpdateStatus(usedFallback
                        ? $"프로필 '{profile.Name}'의 저장된 모니터를 찾지 못해 가장 유사한 모니터를 선택했습니다."
                        : $"프로필 '{profile.Name}'을 불러왔습니다.");
                    return true;
                }
            }

            UpdateStatus($"프로필 '{profile.Name}'의 저장된 모니터를 찾지 못했습니다. 모니터를 다시 선택해주세요.");
            return false;
        }

        private void ClearUnresolvedProfileLaunchBlock(object? sender, EventArgs e)
        {
            if (isLoadingProfileSelection)
            {
                return;
            }

            launchBlockedByUnresolvedProfileMonitor = false;
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
                if (!TryGetSelectedMonitor("프로필 저장", out var targetScreen) || targetScreen == null)
                {
                    return;
                }

                var profile = new Profile
                {
                    Name = dialog.InputText,
                    ExecutablePath = txtExecutablePath.Text,
                    Arguments = txtArguments?.Text ?? string.Empty,
                    MonitorDeviceName = targetScreen.DeviceName,
                    MonitorWasPrimary = targetScreen.Primary,
                    MonitorBoundsX = targetScreen.Bounds.X,
                    MonitorBoundsY = targetScreen.Bounds.Y,
                    MonitorBoundsWidth = targetScreen.Bounds.Width,
                    MonitorBoundsHeight = targetScreen.Bounds.Height,
                    WindowState = cmbWindowState.SelectedIndex switch
                    {
                        0 => AppWindowState.Maximized,
                        1 => AppWindowState.Normal,
                        2 => AppWindowState.Restore,
                        _ => AppWindowState.Maximized
                    }
                };

                var updatedProfiles = profiles
                    .Where(p => p.Name != profile.Name)
                    .ToList();
                updatedProfiles.Add(profile);

                try
                {
                    Profile.SaveProfiles(updatedProfiles, profilesFilePath);
                    profiles = updatedProfiles;
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

            if (lstProfiles.SelectedItem is not Profile profile)
            {
                MessageBox.Show("삭제할 프로필을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"프로필 '{profile.Name}'을(를) 삭제하시겠습니까?", "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var updatedProfiles = profiles
                    .Where(p => !ReferenceEquals(p, profile))
                    .ToList();

                try
                {
                    Profile.SaveProfiles(updatedProfiles, profilesFilePath);
                    profiles = updatedProfiles;
                    RefreshProfileList();
                    UpdateStatus($"프로필 '{profile.Name}'이 삭제되었습니다.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"프로필 삭제 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnCaptureWorkspace_Click(object? sender, EventArgs e)
        {
            CaptureWindowsIntoChecklist();
        }

        private void BtnSaveWorkspace_Click(object? sender, EventArgs e)
        {
            if (chkCapturedWindows == null || txtWorkspaceName == null)
            {
                return;
            }

            if (chkCapturedWindows.Items.Count == 0)
            {
                CaptureWindowsIntoChecklist();
            }

            var selectedWindows = chkCapturedWindows.CheckedItems
                .OfType<CapturedWindowInfo>()
                .ToList();

            if (selectedWindows.Count == 0)
            {
                MessageBox.Show("워크스페이스에 포함할 창을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string workspaceName = txtWorkspaceName.Text.Trim();
            if (string.IsNullOrWhiteSpace(workspaceName))
            {
                using var dialog = new InputDialog("워크스페이스 이름을 입력하세요:");
                if (dialog.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.InputText))
                {
                    return;
                }

                workspaceName = dialog.InputText.Trim();
                txtWorkspaceName.Text = workspaceName;
            }

            var now = DateTime.Now;
            var existing = workspaceProfiles.FirstOrDefault(workspace => workspace.Name == workspaceName);
            var workspaceProfile = new WorkspaceProfile
            {
                Name = workspaceName,
                Apps = selectedWindows.Select(window => window.ToAppWindowProfile()).ToList(),
                CreatedAt = existing?.CreatedAt ?? now,
                UpdatedAt = now
            };

            var updatedWorkspaces = workspaceProfiles
                .Where(workspace => workspace.Name != workspaceProfile.Name)
                .ToList();
            updatedWorkspaces.Add(workspaceProfile);

            try
            {
                WorkspaceProfile.SaveWorkspaces(updatedWorkspaces, workspacesFilePath);
                workspaceProfiles = updatedWorkspaces;
                RefreshWorkspaceList();
                UpdateStatus($"워크스페이스 '{workspaceProfile.Name}'이 저장되었습니다.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"워크스페이스 저장 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnLaunchWorkspace_Click(object? sender, EventArgs e)
        {
            if (lstWorkspaces?.SelectedItem is not WorkspaceProfile workspace)
            {
                MessageBox.Show("실행할 워크스페이스를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await RestoreWorkspaceAsync(workspace, $"워크스페이스 '{workspace.Name}' 실행 중...");
        }

        private void BtnDeleteWorkspace_Click(object? sender, EventArgs e)
        {
            if (lstWorkspaces?.SelectedItem is not WorkspaceProfile workspace)
            {
                MessageBox.Show("삭제할 워크스페이스를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"워크스페이스 '{workspace.Name}'을(를) 삭제하시겠습니까?", "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            var updatedWorkspaces = workspaceProfiles
                .Where(item => !ReferenceEquals(item, workspace))
                .ToList();

            try
            {
                WorkspaceProfile.SaveWorkspaces(updatedWorkspaces, workspacesFilePath);
                workspaceProfiles = updatedWorkspaces;
                RefreshWorkspaceList();
                UpdateStatus($"워크스페이스 '{workspace.Name}'이 삭제되었습니다.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"워크스페이스 삭제 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGatherWindows_Click(object? sender, EventArgs e)
        {
            int movedCount = workspaceRestoreService.GatherWindowsToPrimaryMonitor();
            UpdateStatus(movedCount == 0
                ? "주 모니터로 이동할 창이 없습니다."
                : $"{movedCount}개 창을 주 모니터로 이동했습니다.");
        }

        private void CaptureWindowsIntoChecklist()
        {
            if (chkCapturedWindows == null)
            {
                return;
            }

            var windows = windowCaptureService.CaptureOpenWindows();
            chkCapturedWindows.Items.Clear();

            foreach (var window in windows)
            {
                chkCapturedWindows.Items.Add(window, true);
            }

            UpdateStatus(windows.Count == 0
                ? "캡처할 일반 창을 찾지 못했습니다."
                : $"{windows.Count}개 창을 캡처했습니다. 저장할 창만 체크해주세요.");
        }

        private async Task RestoreWorkspaceAsync(WorkspaceProfile workspace, string statusMessage)
        {
            ToggleWorkspaceButtons(false);
            UpdateStatus(statusMessage);

            try
            {
                var result = await workspaceRestoreService.RestoreAsync(workspace);
                UpdateStatus(result.Summary);

                if (result.FailedApps > 0)
                {
                    MessageBox.Show(string.Join(Environment.NewLine, result.Messages), "워크스페이스 복원 결과", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"워크스페이스 실행 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"워크스페이스 실행 실패: {ex.Message}");
            }
            finally
            {
                ToggleWorkspaceButtons(true);
            }
        }

        private void ToggleWorkspaceButtons(bool enabled)
        {
            if (btnLaunchWorkspace != null) btnLaunchWorkspace.Enabled = enabled;
            if (btnSaveWorkspace != null) btnSaveWorkspace.Enabled = enabled;
            if (btnDeleteWorkspace != null) btnDeleteWorkspace.Enabled = enabled;
            if (btnCaptureWorkspace != null) btnCaptureWorkspace.Enabled = enabled;
            if (btnGatherWindows != null) btnGatherWindows.Enabled = enabled;
        }

        private void UpdateStatus(string message)
        {
            if (lblStatus != null)
            {
                lblStatus.Text = message;
                lblStatus.Refresh();
            }
        }

        private static Label CreateFieldLabel(string text, Point location, Font font)
        {
            return new Label
            {
                Text = text,
                Location = location,
                Size = new Size(180, 20),
                Font = font,
                ForeColor = Color.FromArgb(54, 60, 72),
                BackColor = Color.Transparent
            };
        }

        private static Panel CreateShadowPanel(Rectangle bounds)
        {
            return new ShadowPanel
            {
                Bounds = bounds,
                BackColor = Color.FromArgb(223, 227, 235)
            };
        }

        private static Panel CreateCardPanel(Rectangle bounds)
        {
            return new CardPanel
            {
                Bounds = bounds,
                BackColor = Color.White
            };
        }

        private Button CreateIconButton(string text, Point location, string tooltipText)
        {
            var button = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(56, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(61, 72, 91),
                Font = new Font("Segoe MDL2 Assets", 12F, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(207, 214, 224);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(238, 242, 248);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 247, 251);
            uiToolTip?.SetToolTip(button, tooltipText);
            return button;
        }

        private Panel CreateInputShell(Point location, Size size, out TextBox textBox)
        {
            var shell = new Panel
            {
                Location = location,
                Size = size,
                BackColor = Color.White,
                Padding = new Padding(12, 10, 12, 8),
                Tag = false
            };
            shell.Paint += InputShell_Paint;

            var innerTextBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(39, 43, 52)
            };
            innerTextBox.Enter += InputControl_Enter;
            innerTextBox.Leave += InputControl_Leave;
            shell.Click += (_, _) => innerTextBox.Focus();
            shell.MouseDown += (_, _) => innerTextBox.Focus();
            shell.Controls.Add(innerTextBox);
            textBox = innerTextBox;
            return shell;
        }

        private void InputControl_Enter(object? sender, EventArgs e)
        {
            if (sender is Control control && control.Parent is Panel shell)
            {
                shell.Tag = true;
                shell.Invalidate();
            }
        }

        private void InputControl_Leave(object? sender, EventArgs e)
        {
            if (sender is Control control && control.Parent is Panel shell)
            {
                shell.Tag = false;
                shell.Invalidate();
            }
        }

        private void InputShell_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel shell)
            {
                return;
            }

            bool focused = shell.Tag is bool isFocused && isFocused;
            Color borderColor = focused ? Color.FromArgb(0, 120, 215) : Color.FromArgb(207, 214, 224);
            Rectangle rect = new Rectangle(0, 0, shell.Width - 1, shell.Height - 1);

            using var pen = new Pen(borderColor, focused ? 2F : 1F);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            DrawRoundedRectangle(e.Graphics, pen, rect, 10);
        }

        private void LstProfiles_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (lstProfiles == null || e.Index < 0 || e.Index >= lstProfiles.Items.Count)
            {
                return;
            }

            var profile = (Profile)lstProfiles.Items[e.Index];
            e.DrawBackground();

            Rectangle cardBounds = new Rectangle(e.Bounds.X + 6, e.Bounds.Y + 4, e.Bounds.Width - 12, e.Bounds.Height - 8);
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color fillColor = selected ? Color.FromArgb(234, 244, 255) : Color.FromArgb(248, 249, 252);
            Color borderColor = selected ? Color.FromArgb(0, 120, 215) : Color.FromArgb(226, 231, 238);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(fillColor))
            using (var pen = new Pen(borderColor))
            {
                FillRoundedRectangle(e.Graphics, brush, cardBounds, 12);
                DrawRoundedRectangle(e.Graphics, pen, cardBounds, 12);
            }

            using var titleFont = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            using var detailFont = new Font("Segoe UI", 8.2F);
            using var titleBrush = new SolidBrush(Color.FromArgb(35, 41, 52));
            using var detailBrush = new SolidBrush(Color.FromArgb(108, 116, 128));

            string secondary = $"{GetMonitorSummary(profile)}  ·  {Path.GetFileName(profile.ExecutablePath)}";
            string tertiary = profile.ExecutablePath;

            e.Graphics.DrawString(profile.Name, titleFont, titleBrush, new PointF(cardBounds.X + 14, cardBounds.Y + 10));
            e.Graphics.DrawString(secondary, detailFont, detailBrush, new PointF(cardBounds.X + 14, cardBounds.Y + 31));

            Rectangle tertiaryBounds = new Rectangle(cardBounds.X + 14, cardBounds.Y + 44, cardBounds.Width - 28, 14);
            TextRenderer.DrawText(e.Graphics, tertiary, detailFont, tertiaryBounds, Color.FromArgb(130, 136, 145),
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.Left);

            e.DrawFocusRectangle();
        }

        private void ProfileListShell_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel shell)
            {
                return;
            }

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, shell.Width - 1, shell.Height - 1);
            using var brush = new SolidBrush(shell.BackColor);
            using var pen = new Pen(Color.FromArgb(220, 225, 233));
            FillRoundedRectangle(e.Graphics, brush, rect, 14);
            DrawRoundedRectangle(e.Graphics, pen, rect, 14);
        }

        private static string GetMonitorSummary(Profile profile)
        {
            var screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i].DeviceName == profile.MonitorDeviceName)
                {
                    return $"모니터 {i + 1}";
                }
            }

            return profile.MonitorWasPrimary ? "기본 모니터" : "저장된 모니터";
        }

        private static void DrawRoundedRectangle(Graphics graphics, Pen pen, Rectangle bounds, int radius)
        {
            using var path = BuildRoundedPath(bounds, radius);
            graphics.DrawPath(pen, path);
        }

        private static void FillRoundedRectangle(Graphics graphics, Brush brush, Rectangle bounds, int radius)
        {
            using var path = BuildRoundedPath(bounds, radius);
            graphics.FillPath(brush, path);
        }

        private static System.Drawing.Drawing2D.GraphicsPath BuildRoundedPath(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayProfilesMenuItem = new ToolStripMenuItem("프로필 실행");
            trayMenu.Items.Add(trayProfilesMenuItem);

            trayWorkspacesMenuItem = new ToolStripMenuItem("워크스페이스 실행");
            trayMenu.Items.Add(trayWorkspacesMenuItem);

            var showMenuItem = new ToolStripMenuItem("표시");
            showMenuItem.Click += ShowMenuItem_Click;
            trayMenu.Items.Add(showMenuItem);

            var exitMenuItem = new ToolStripMenuItem("종료");
            exitMenuItem.Click += ExitMenuItem_Click;
            trayMenu.Items.Add(exitMenuItem);

            trayIcon = new NotifyIcon
            {
                Icon = LoadApplicationIcon(),
                ContextMenuStrip = trayMenu,
                Text = "Monitor Launcher",
                Visible = true
            };

            trayIcon.DoubleClick += TrayIcon_DoubleClick;
            this.FormClosing += MainForm_FormClosing;
            RebuildTrayProfilesMenu();
            RebuildTrayWorkspacesMenu();
        }

        private static Icon LoadApplicationIcon()
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "favicon.ico");
            if (!File.Exists(iconPath))
            {
                return SystemIcons.Application;
            }

            try
            {
                return new Icon(iconPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MonitorLauncher] 아이콘 로드 실패: {ex}");
                return SystemIcons.Application;
            }
        }

        private void RebuildTrayProfilesMenu()
        {
            if (trayProfilesMenuItem == null)
            {
                return;
            }

            trayProfilesMenuItem.DropDownItems.Clear();

            if (profiles.Count == 0)
            {
                var emptyItem = new ToolStripMenuItem("저장된 프로필 없음")
                {
                    Enabled = false
                };
                trayProfilesMenuItem.DropDownItems.Add(emptyItem);
                return;
            }

            foreach (var profile in profiles.OrderBy(profile => profile.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                var profileMenuItem = new ToolStripMenuItem(profile.Name)
                {
                    Tag = profile
                };
                profileMenuItem.Click += TrayProfileMenuItem_Click;
                trayProfilesMenuItem.DropDownItems.Add(profileMenuItem);
            }
        }

        private void RebuildTrayWorkspacesMenu()
        {
            if (trayWorkspacesMenuItem == null)
            {
                return;
            }

            trayWorkspacesMenuItem.DropDownItems.Clear();

            if (workspaceProfiles.Count == 0)
            {
                var emptyItem = new ToolStripMenuItem("저장된 워크스페이스 없음")
                {
                    Enabled = false
                };
                trayWorkspacesMenuItem.DropDownItems.Add(emptyItem);
                return;
            }

            foreach (var workspace in workspaceProfiles.OrderBy(workspace => workspace.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                var workspaceMenuItem = new ToolStripMenuItem(workspace.Name)
                {
                    Tag = workspace
                };
                workspaceMenuItem.Click += TrayWorkspaceMenuItem_Click;
                trayWorkspacesMenuItem.DropDownItems.Add(workspaceMenuItem);
            }
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                if (trayIcon != null)
                    trayIcon.ShowBalloonTip(2000, "Monitor Launcher", "프로그램이 백그라운드로 실행 중입니다.", ToolTipIcon.Info);
            }
        }

        private void TrayIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowWindow();
        }

        private void ShowMenuItem_Click(object? sender, EventArgs e)
        {
            ShowWindow();
        }

        private async void TrayProfileMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem menuItem || menuItem.Tag is not Profile profile)
            {
                return;
            }

            await LaunchProfileAsync(profile, $"트레이에서 프로필 '{profile.Name}' 실행 중...");
        }

        private async void TrayWorkspaceMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem menuItem || menuItem.Tag is not WorkspaceProfile workspace)
            {
                return;
            }

            await RestoreWorkspaceAsync(workspace, $"트레이에서 워크스페이스 '{workspace.Name}' 실행 중...");
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void ExitMenuItem_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Monitor Launcher를 종료하시겠습니까?", "종료 확인", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                trayIcon?.Dispose();
                Application.Exit();
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                
                using var dialog = new CloseDialog();
                var result = dialog.ShowDialog(this);
                
                if (result == DialogResult.Yes)
                {
                    // 프로그램 종료
                    trayIcon?.Dispose();
                    Application.Exit();
                }
                else if (result == DialogResult.No)
                {
                    // 창만 닫기 (백그라운드 실행)
                    this.Hide();
                    if (trayIcon != null)
                        trayIcon.ShowBalloonTip(2000, "Monitor Launcher", "프로그램이 백그라운드로 실행 중입니다.", ToolTipIcon.Info);
                }
                // DialogResult.Cancel인 경우 아무것도 하지 않음 (창 유지)
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                trayIcon?.Dispose();
                trayMenu?.Dispose();
                uiToolTip?.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class MonitorOption
        {
            public MonitorOption(string displayName, Screen screen)
            {
                DisplayName = displayName;
                DeviceName = screen.DeviceName;
                Bounds = screen.Bounds;
            }

            private string DisplayName { get; }
            private string DeviceName { get; }
            private Rectangle Bounds { get; }

            public bool Matches(Screen screen)
            {
                return DeviceName == screen.DeviceName &&
                    Bounds == screen.Bounds;
            }

            public override string ToString()
            {
                return DisplayName;
            }
        }
    }

    public class ShadowPanel : Panel
    {
        public ShadowPanel()
        {
            this.DoubleBuffered = true;
            this.Resize += (_, _) => UpdateRegion();
            UpdateRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            using var brush = new SolidBrush(this.BackColor);
            using var path = BuildRoundedPath(bounds, 22);
            e.Graphics.FillPath(brush, path);
        }

        private void UpdateRegion()
        {
            if (this.Width <= 0 || this.Height <= 0)
            {
                return;
            }

            using var path = BuildRoundedPath(new Rectangle(0, 0, this.Width, this.Height), 22);
            this.Region = new Region(path);
        }

        private static System.Drawing.Drawing2D.GraphicsPath BuildRoundedPath(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class CardPanel : Panel
    {
        public CardPanel()
        {
            this.DoubleBuffered = true;
            this.Resize += (_, _) => UpdateRegion();
            UpdateRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            using var brush = new SolidBrush(this.BackColor);
            using var pen = new Pen(Color.FromArgb(228, 232, 238));
            using var path = BuildRoundedPath(bounds, 18);
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
        }

        private void UpdateRegion()
        {
            if (this.Width <= 0 || this.Height <= 0)
            {
                return;
            }

            using var path = BuildRoundedPath(new Rectangle(0, 0, this.Width, this.Height), 18);
            this.Region = new Region(path);
        }

        private static System.Drawing.Drawing2D.GraphicsPath BuildRoundedPath(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class InputDialog : Form
    {
        public string InputText { get; private set; } = string.Empty;
        private TextBox? textBox;

        public InputDialog(string prompt)
        {
            this.Text = "프로필 이름";
            this.Size = new Size(400, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            var lblPrompt = new Label
            {
                Text = prompt,
                Location = new Point(10, 15),
                Size = new Size(370, 20)
            };
            this.Controls.Add(lblPrompt);

            textBox = new TextBox
            {
                Location = new Point(10, 40),
                Size = new Size(370, 23)
            };
            this.Controls.Add(textBox);

            var btnOk = new Button
            {
                Text = "확인",
                DialogResult = DialogResult.OK,
                Location = new Point(220, 80),
                Size = new Size(75, 30)
            };
            btnOk.Click += (s, e) => { InputText = textBox.Text; };
            this.Controls.Add(btnOk);

            var btnCancel = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Location = new Point(305, 80),
                Size = new Size(75, 30)
            };
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }

    public class CloseDialog : Form
    {
        public CloseDialog()
        {
            this.Text = "Monitor Launcher";
            this.Size = new Size(380, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            var lblMessage = new Label
            {
                Text = "프로그램을 어떻게 하시겠습니까?",
                Location = new Point(20, 20),
                Size = new Size(340, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            this.Controls.Add(lblMessage);

            var btnExit = new Button
            {
                Text = "종료",
                DialogResult = DialogResult.Yes,
                Location = new Point(20, 70),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(btnExit);

            var btnMinimize = new Button
            {
                Text = "백그라운드 실행",
                DialogResult = DialogResult.No,
                Location = new Point(130, 70),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(btnMinimize);

            var btnCancel = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Location = new Point(260, 70),
                Size = new Size(100, 35)
            };
            this.Controls.Add(btnCancel);

            this.CancelButton = btnCancel;
        }
    }
}
