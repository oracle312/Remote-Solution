using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;

namespace DioRemoteControl.Client.Forms
{
    /// <summary>
    /// 클라이언트 메인 폼 - 화면 공유 기능 추가
    /// </summary>
    public partial class ClientMainForm : Form
    {
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private string _authCode;
        private string _clientId;
        private bool _isScreenSharing = false;

        // 화면 캡처 타이머
        private System.Threading.Timer _screenCaptureTimer;
        private int _captureInterval = 100; // 100ms = 10 FPS

        // UI 컨트롤
        private Label lblTitle;
        private Label lblStatus;
        private Label lblAuthLabel;
        private TextBox txtAuthCode;
        private Button btnConnect;
        private TextBox txtLog;
        private Panel topPanel;
        private Panel mainPanel;
        private Label lblScreenStatus;

        public ClientMainForm()
        {
            InitializeComponent();
            InitializeUI();
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            this.Text = "DIO-SYSTEM 원격지원 - 고객";
            this.Size = new Size(600, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // 상단 패널
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.FromArgb(102, 126, 234),
                Padding = new Padding(20)
            };

            lblTitle = new Label
            {
                Text = "DIO-SYSTEM 원격지원",
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 18, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 10)
            };

            lblStatus = new Label
            {
                Text = "인증번호를 입력하세요",
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 10),
                AutoSize = true,
                Location = new Point(20, 45)
            };

            lblScreenStatus = new Label
            {
                Text = "화면 공유: 대기 중",
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 9),
                AutoSize = true,
                Location = new Point(20, 70)
            };

            topPanel.Controls.AddRange(new Control[] { lblTitle, lblStatus, lblScreenStatus });
            this.Controls.Add(topPanel);

            // 메인 패널
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30)
            };

            // 안내 메시지
            Label lblInfo = new Label
            {
                Text = "상담원에게 받은 6자리 인증번호를 입력하세요.",
                Font = new Font("맑은 고딕", 11),
                TextAlign = ContentAlignment.TopCenter,
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(0, 20, 0, 0)
            };

            // 인증번호 입력 패널
            Panel authPanel = new Panel
            {
                Height = 120,
                Dock = DockStyle.Top
            };

            lblAuthLabel = new Label
            {
                Text = "인증번호:",
                Font = new Font("맑은 고딕", 11),
                AutoSize = true,
                Location = new Point(150, 20)
            };

            txtAuthCode = new TextBox
            {
                Font = new Font("Consolas", 24, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                MaxLength = 6,
                Width = 220,
                Location = new Point(150, 50)
            };

            // 숫자만 입력
            txtAuthCode.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }

                if (e.KeyChar == (char)13)
                {
                    btnConnect.PerformClick();
                }
            };

            btnConnect = new Button
            {
                Text = "연결",
                Font = new Font("맑은 고딕", 12, FontStyle.Bold),
                Size = new Size(100, 50),
                Location = new Point(380, 45),
                BackColor = Color.FromArgb(102, 126, 234),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnConnect.FlatAppearance.BorderSize = 0;
            btnConnect.Click += BtnConnect_Click;

            authPanel.Controls.AddRange(new Control[] { lblAuthLabel, txtAuthCode, btnConnect });

            // 로그
            txtLog = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 200,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9)
            };

            mainPanel.Controls.AddRange(new Control[] { lblInfo, authPanel, txtLog });
            this.Controls.Add(mainPanel);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Log("=== DIO-SYSTEM 원격지원 클라이언트 ===");
            Log("상담원에게 받은 인증번호를 입력하세요.");
            txtAuthCode.Focus();
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAuthCode.Text) || txtAuthCode.Text.Length != 6)
            {
                MessageBox.Show("6자리 인증번호를 입력하세요.", "입력 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAuthCode.Focus();
                return;
            }

            _authCode = txtAuthCode.Text.Trim();
            btnConnect.Enabled = false;
            txtAuthCode.Enabled = false;

            UpdateStatus("연결 중...", Color.Orange);
            Log($"인증번호: {_authCode}");

            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            try
            {
                Log("=== WebSocket 서버 연결 시작 ===");
                Log($"서버 URL: wss://remote.dio-system.com/ws");

                _ws = new ClientWebSocket();
                _cts = new CancellationTokenSource();
                var uri = new Uri("wss://remote.dio-system.com/ws");

                var connectTask = _ws.ConnectAsync(uri, _cts.Token);
                var timeoutTask = Task.Delay(30000);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Log("❌ 연결 타임아웃 (30초)");
                    UpdateStatus("연결 실패", Color.Red);
                    MessageBox.Show("서버 연결 시간이 초과되었습니다.", "타임아웃",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ResetUI();
                    return;
                }

                await connectTask;

                Log("✅ WebSocket 연결 성공!");
                UpdateStatus("서버 연결됨", Color.LightGreen);

                _ = Task.Run(() => ReceiveLoop());

                await Task.Delay(1000);
                await RegisterClient();
            }
            catch (Exception ex)
            {
                Log($"❌ 연결 실패: {ex.Message}");
                UpdateStatus("연결 실패", Color.Red);
                MessageBox.Show($"연결 중 오류가 발생했습니다.\n\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUI();
            }
        }

        private async Task RegisterClient()
        {
            try
            {
                var message = new
                {
                    type = "register_client",
                    auth_code = _authCode,
                    client_name = Environment.MachineName,
                    client_info = $"Windows {Environment.OSVersion.Version}"
                };

                await SendMessage(message);
                Log($"📤 클라이언트 등록 요청: {_authCode}");
                UpdateStatus("등록 중...", Color.Orange);
            }
            catch (Exception ex)
            {
                Log($"❌ 등록 실패: {ex.Message}");
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];

            try
            {
                while (_ws.State == WebSocketState.Open)
                {
                    var result = await _ws.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cts.Token
                    );

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Log("서버 연결 종료");
                        this.Invoke(new Action(() =>
                        {
                            UpdateStatus("연결 종료됨", Color.Red);
                            StopScreenSharing();
                            MessageBox.Show("서버와의 연결이 종료되었습니다.",
                                "연결 종료", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            ResetUI();
                        }));
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        this.Invoke(new Action(() => HandleMessage(message)));
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"❌ 수신 오류: {ex.Message}");
            }
        }

        private async Task SendMessage(object message)
        {
            try
            {
                if (_ws.State != WebSocketState.Open)
                {
                    Log("⚠️ WebSocket이 열려있지 않습니다.");
                    return;
                }

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                byte[] buffer = Encoding.UTF8.GetBytes(json);

                await _ws.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token
                );
            }
            catch (Exception ex)
            {
                Log($"❌ 전송 오류: {ex.Message}");
            }
        }

        private void HandleMessage(string jsonMessage)
        {
            try
            {
                var data = JObject.Parse(jsonMessage);
                string type = data["type"]?.ToString();

                switch (type)
                {
                    case "connected":
                        Log("✅ 서버 연결 확인");
                        UpdateStatus("서버 연결됨", Color.LightGreen);
                        break;

                    case "client_registered":
                        _clientId = data["client_id"]?.ToString();
                        Log("✅ 등록 완료!");

                        var agentName = data["agent_name"]?.ToString();
                        if (!string.IsNullOrEmpty(agentName))
                        {
                            Log($"✅ 상담원 연결: {agentName}");
                            UpdateStatus($"상담원 연결됨 ({agentName})", Color.LightGreen);

                            // 화면 공유 자동 시작
                            StartScreenSharing();

                            MessageBox.Show($"상담원({agentName})과 연결되었습니다.\n화면 공유가 시작됩니다.",
                                "연결 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            UpdateStatus("상담원 대기 중", Color.Yellow);
                        }
                        break;

                    case "agent_connected":
                        var agentName2 = data["agent_name"]?.ToString() ?? "상담원";
                        Log($"✅ 상담원 연결: {agentName2}");
                        UpdateStatus($"상담원 연결됨 ({agentName2})", Color.LightGreen);

                        // 화면 공유 시작
                        StartScreenSharing();

                        MessageBox.Show($"상담원({agentName2})과 연결되었습니다.\n화면 공유가 시작됩니다.",
                            "연결 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;

                    case "error":
                        var error = data["message"]?.ToString();
                        Log($"❌ 오류: {error}");
                        MessageBox.Show(error, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        if (error.Contains("인증번호"))
                        {
                            ResetUI();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"❌ 메시지 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 화면 공유 시작
        /// </summary>
        private void StartScreenSharing()
        {
            if (_isScreenSharing) return;

            _isScreenSharing = true;
            Log("🖥️ 화면 공유 시작");
            UpdateScreenStatus("화면 공유: 활성", Color.LightGreen);

            // 타이머 시작 (100ms마다 화면 캡처)
            _screenCaptureTimer = new System.Threading.Timer(
                CaptureAndSendScreen,
                null,
                0,
                _captureInterval
            );
        }

        /// <summary>
        /// 화면 공유 중지
        /// </summary>
        private void StopScreenSharing()
        {
            if (!_isScreenSharing) return;

            _isScreenSharing = false;
            _screenCaptureTimer?.Dispose();
            _screenCaptureTimer = null;

            Log("🖥️ 화면 공유 중지");
            UpdateScreenStatus("화면 공유: 중지됨", Color.Gray);
        }

        /// <summary>
        /// 화면 캡처 및 전송
        /// </summary>
        private async void CaptureAndSendScreen(object state)
        {
            if (!_isScreenSharing || _ws?.State != WebSocketState.Open)
                return;

            try
            {
                // 화면 캡처
                using (Bitmap screenshot = CaptureScreen())
                {
                    if (screenshot == null) return;

                    // JPEG 압축 (품질 75%)
                    string base64Image = BitmapToBase64(screenshot, 75);

                    // 서버로 전송
                    var message = new
                    {
                        type = "screen_data",
                        client_id = _clientId,
                        data = base64Image,
                        width = screenshot.Width,
                        height = screenshot.Height,
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    await SendMessage(message);
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시 로그만 출력 (화면 공유는 계속)
                if (_isScreenSharing)
                {
                    this.Invoke(new Action(() =>
                        Log($"⚠️ 화면 캡처 오류: {ex.Message}")
                    ));
                }
            }
        }

        /// <summary>
        /// 화면 캡처
        /// </summary>
        private Bitmap CaptureScreen()
        {
            try
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Bitmap을 Base64 문자열로 변환
        /// </summary>
        private string BitmapToBase64(Bitmap bitmap, int quality)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // JPEG 인코더 설정
                ImageCodecInfo jpegCodec = GetEncoder(ImageFormat.Jpeg);
                EncoderParameters encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);

                bitmap.Save(ms, jpegCodec, encoderParams);
                byte[] imageBytes = ms.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
        }

        /// <summary>
        /// 이미지 인코더 가져오기
        /// </summary>
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private void UpdateStatus(string status, Color color)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatus(status, color)));
                return;
            }

            lblStatus.Text = status;
            lblStatus.ForeColor = color;
        }

        private void UpdateScreenStatus(string status, Color color)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateScreenStatus(status, color)));
                return;
            }

            lblScreenStatus.Text = status;
            lblScreenStatus.ForeColor = color;
        }

        private void Log(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => Log(message)));
                return;
            }

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void ResetUI()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ResetUI));
                return;
            }

            StopScreenSharing();
            btnConnect.Enabled = true;
            txtAuthCode.Enabled = true;
            txtAuthCode.Focus();
            txtAuthCode.SelectAll();
        }

        private void Disconnect()
        {
            try
            {
                StopScreenSharing();
                _cts?.Cancel();

                if (_ws != null && _ws.State == WebSocketState.Open)
                {
                    _ws.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "사용자 종료",
                        CancellationToken.None
                    ).Wait(1000);
                }

                _ws?.Dispose();
                _cts?.Dispose();

                Log("연결 종료됨");
            }
            catch (Exception ex)
            {
                Log($"종료 중 오류: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            Disconnect();
        }
    }
}
