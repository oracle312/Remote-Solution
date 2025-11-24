using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using DioRemoteControl.Common.Protocol;
using DioRemoteControl.Common;
using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Drawing.Imaging;
using DioRemoteControl.Agent.Controls;

namespace DioRemoteControl.Agent.Forms
{
    /// <summary>
    /// 상담원 메인 폼 - 표준 WebSocket 사용
    /// </summary>
    public partial class MainForm : Form
    {
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private string _agentId;
        private string _agentName;
        private string _authCode;
        private int _sessionId;
        private List<SessionPanel> _sessionPanels;
        private Dictionary<string, SessionInfo> _sessions; // 추가
        private const int MAX_SESSIONS = 3;
        private System.Threading.SemaphoreSlim _sendLock = new System.Threading.SemaphoreSlim(1, 1);

        // UI 컨트롤
        private Label lblAuthCode;
        private Label lblStatus;
        private Panel panelSessions;
        private Button btnSettings;
        private Button btnDisconnect;
        private TextBox txtLog;

        // 세션 정보 클래스
        private class SessionInfo
        {
            public string ClientId { get; set; }
            public SessionPanel Panel { get; set; }
            public DateTime ConnectedAt { get; set; }
        }

        public MainForm()
        {
            InitializeComponent();
            InitializeUI();
            _sessionPanels = new List<SessionPanel>();
            _sessions = new Dictionary<string, SessionInfo>(); // 초기화
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            this.Text = "DIO-SYSTEM 원격제어 - 상담원";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 상단 패널
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(102, 126, 234),
                Padding = new Padding(20, 10, 20, 10)
            };

            Label lblTitle = new Label
            {
                Text = "DIO-SYSTEM 원격지원",
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            lblAuthCode = new Label
            {
                Text = "인증번호: ------",
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 20, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 42)
            };

            lblStatus = new Label
            {
                Text = "연결 중...",
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 10),
                AutoSize = true,
                Location = new Point(this.Width - 200, 30)
            };

            btnSettings = new Button
            {
                Text = "⚙ 설정",
                Size = new Size(80, 35),
                Location = new Point(this.Width - 280, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(118, 75, 162),
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 10)
            };
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.Click += BtnSettings_Click;

            btnDisconnect = new Button
            {
                Text = "연결 종료",
                Size = new Size(100, 35),
                Location = new Point(this.Width - 190, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 10)
            };
            btnDisconnect.FlatAppearance.BorderSize = 0;
            btnDisconnect.Click += BtnDisconnect_Click;

            topPanel.Controls.AddRange(new Control[] { lblTitle, lblAuthCode, lblStatus, btnSettings, btnDisconnect });

            // 세션 패널 컨테이너
            panelSessions = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245),
                AutoScroll = true,
                Padding = new Padding(10, 80, 10, 100)  // ✅ 위쪽 20px로 증가
            };

            // 하단 로그 패널
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                BackColor = Color.White
            };

            txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9)
            };

            bottomPanel.Controls.Add(txtLog);

            // ✅ Controls 추가 순서 중요! (Bottom → Top → Fill)
            this.Controls.Add(bottomPanel);   // 1. 하단 먼저
            this.Controls.Add(topPanel);      // 2. 상단 다음
            this.Controls.Add(panelSessions); // 3. 중간 Fill 마지막

            // ✅ 폼 크기 변경 시 패널 재배치
            this.Resize += (s, e) => ResizeSessionPanels();
        }

        /// <summary>
        /// 폼 로드 시
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // 상담원 정보 입력 (실제로는 로그인 폼에서 가져옴)
            _agentId = "agent001";
            _agentName = "김상담";

            // WebSocket 연결
            ConnectToServer();
        }

        /// <summary>
        /// 서버 연결
        /// </summary>
        private async void ConnectToServer()
        {
            try
            {
                Log("=== WebSocket 서버 연결 시작 ===");
                UpdateStatus("연결 중...", Color.Orange);

                _ws = new ClientWebSocket();
                _cts = new CancellationTokenSource();
                var uri = new Uri("wss://remote.dio-system.com/ws");

                Log($"URL: {uri}");
                Log("연결 시도...");

                // 연결 (타임아웃 30초)
                var connectTask = _ws.ConnectAsync(uri, _cts.Token);
                var timeoutTask = Task.Delay(30000);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Log("❌ 연결 타임아웃 (30초)");
                    MessageBox.Show("서버 연결 시간이 초과되었습니다.", "타임아웃",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                await connectTask; // 예외 발생 시 catch로

                Log("✅ WebSocket 연결 성공!");
                UpdateStatus("연결됨", Color.LightGreen);

                // 메시지 수신 시작
                _ = Task.Run(() => ReceiveLoop());

                // 서버 연결 메시지 대기 (1초)
                await Task.Delay(1000);

                // 상담원 등록
                await RegisterAgent();
            }
            catch (WebSocketException wsEx)
            {
                Log($"❌ WebSocket 예외: {wsEx.Message}");
                Log($"오류 코드: {wsEx.WebSocketErrorCode}");
                MessageBox.Show($"WebSocket 연결 실패:\n{wsEx.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
            catch (Exception ex)
            {
                Log($"❌ 연결 예외: {ex.Message}");
                MessageBox.Show($"연결 오류:\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        /// <summary>
        /// 메시지 수신 루프
        /// </summary>
        private async Task ReceiveLoop()
        {
            var buffer = new byte[65536]; // 64KB로 증가 (8KB → 64KB)

            try
            {
                while (_ws.State == WebSocketState.Open)
                {
                    // 메시지 조각들을 모을 리스트
                    var messageBuilder = new System.Collections.Generic.List<byte>();
                    WebSocketReceiveResult result;

                    // 완전한 메시지를 받을 때까지 반복
                    do
                    {
                        result = await _ws.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            _cts.Token
                        );

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Log("서버가 연결을 종료했습니다.");
                            this.Invoke(new Action(() =>
                            {
                                UpdateStatus("연결 끊김", Color.Red);
                                MessageBox.Show("서버와의 연결이 종료되었습니다.",
                                    "연결 종료", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                this.Close();
                            }));
                            return;
                        }

                        // 받은 데이터를 리스트에 추가
                        if (result.Count > 0)
                        {
                            byte[] chunk = new byte[result.Count];
                            Array.Copy(buffer, 0, chunk, 0, result.Count);
                            messageBuilder.AddRange(chunk);
                        }

                    } while (!result.EndOfMessage); // 메시지 끝까지 반복

                    // 완전한 메시지 처리
                    if (result.MessageType == WebSocketMessageType.Text && messageBuilder.Count > 0)
                    {
                        string message = Encoding.UTF8.GetString(messageBuilder.ToArray());

                        // 너무 긴 메시지는 로그에서 축약
                        string logMessage = message.Length > 200
                            ? message.Substring(0, 200) + $"... (총 {message.Length} chars)"
                            : message;

                        Log($"📨 메시지 수신 (크기: {message.Length} chars): {logMessage}");

                        // 메시지 처리
                        this.Invoke(new Action(() => HandleMessage(message)));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log("수신 루프 취소됨");
            }
            catch (WebSocketException wsEx)
            {
                Log($"❌ WebSocket 수신 오류: {wsEx.Message}");
                this.Invoke(new Action(() =>
                {
                    UpdateStatus("연결 끊김", Color.Red);
                }));
            }
            catch (Exception ex)
            {
                Log($"❌ 수신 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 메시지 전송
        /// </summary>
        private async Task SendMessage(object message)
        {
            await _sendLock.WaitAsync();  // ✅ 전송 락 획득
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

                // 전송 성공 로그 (screen_data는 너무 많으니 제외)
                var msgType = Newtonsoft.Json.Linq.JObject.Parse(json)["type"]?.ToString();
                if (msgType != "screen_data")
                {
                    Log($"📤 메시지 전송: {msgType}");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ 전송 오류: {ex.Message}");
            }
            finally
            {
                _sendLock.Release();  // ✅ 전송 락 해제
            }
        }

        /// <summary>
        /// 상담원 등록
        /// </summary>
        private async Task RegisterAgent()
        {
            try
            {
                var message = new
                {
                    type = "register_agent",
                    agent_id = _agentId,
                    agent_name = _agentName
                };

                await SendMessage(message);
                Log($"✅ 상담원 등록 요청: {_agentName}");
            }
            catch (Exception ex)
            {
                Log($"❌ 상담원 등록 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 메시지 처리
        /// </summary>
        private void HandleMessage(string jsonMessage)
        {
            try
            {
                var data = JObject.Parse(jsonMessage);
                string type = data["type"]?.ToString();

                switch (type)
                {
                    case "connected":
                        Log("서버 연결 확인 메시지 수신");
                        break;

                    case "auth_code":
                        HandleAuthCode(data);
                        break;

                    case "client_connected":
                        HandleClientConnected(data);
                        break;

                    case "client_disconnected":
                        HandleClientDisconnected(data);
                        break;

                    case "screen_data":
                        HandleScreenData(data);
                        break;

                    case "chat_message":
                        HandleChatMessage(data);
                        break;

                    case "error":
                        HandleError(data);
                        break;

                    default:
                        Log($"⚠️ 알 수 없는 메시지 타입: {type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"❌ 메시지 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 인증번호 수신
        /// </summary>
        private void HandleAuthCode(JObject data)
        {
            _authCode = data["auth_code"]?.ToString();
            _sessionId = data["session_id"]?.ToObject<int>() ?? 0;

            lblAuthCode.Text = $"인증번호: {_authCode}";
            Log($"✅ 인증번호 생성: {_authCode}");

            UpdateStatus("대기 중 (고객 연결 대기)", Color.Yellow);
        }

        /// <summary>
        /// 클라이언트 연결
        /// </summary>
        private void HandleClientConnected(JObject data)
        {
            // 서버에서 보낸 client 객체에서 정보 추출
            var client = data["client"];
            if (client == null)
            {
                Log("❌ client_connected 메시지에 client 정보가 없습니다.");
                return;
            }

            string clientId = client["id"]?.ToString();
            string clientName = client["name"]?.ToString() ?? "알 수 없음";
            string clientInfo = client["info"]?.ToString() ?? "";

            if (string.IsNullOrEmpty(clientId))
            {
                Log("❌ client_id가 비어있습니다.");
                return;
            }

            Log($"📥 클라이언트 연결 데이터 수신: ID={clientId}, Name={clientName}");

            // 새 세션 패널 생성
            if (_sessionPanels.Count < MAX_SESSIONS)
            {
                SessionPanel panel = new SessionPanel();

                panelSessions.Controls.Add(panel);
                _sessionPanels.Add(panel);

                _sessions[clientId] = new SessionInfo
                {
                    ClientId = clientId,
                    Panel = panel,
                    ConnectedAt = DateTime.Now
                };

                // ===== 마우스 이벤트 등록 =====
                panel.RemoteMouseMove += (s, e) => SendMouseEvent(clientId, "move", e);
                panel.RemoteMouseDown += (s, e) => SendMouseEvent(clientId, "down", e);
                panel.RemoteMouseUp += (s, e) => SendMouseEvent(clientId, "up", e);
                panel.RemoteMouseClick += (s, e) => SendMouseEvent(clientId, "click", e);

                // ===== 키보드 이벤트 등록 ===== ✅ 추가!
                panel.RemoteKeyDown += async (s, e) =>
                {
                    await SendKeyboardEvent(clientId, "key_down", (int)e.KeyCode,
                        e.Control, e.Alt, e.Shift);
                };

                panel.RemoteKeyUp += async (s, e) =>
                {
                    await SendKeyboardEvent(clientId, "key_up", (int)e.KeyCode,
                        e.Control, e.Alt, e.Shift);
                };

                // 포커스 주기 ✅
                panel.GrabFocus();

                // ✅ 패널 크기 재조정
                ResizeSessionPanels();

                Log($"✅ 고객 연결 완료: {clientName} ({clientId})");
                Log($"📺 SessionPanel 생성 완료 (총 {_sessionPanels.Count}개)");
                Log($"⌨️ 키보드 이벤트 등록 완료");
                UpdateStatus($"연결됨 ({_sessionPanels.Count}/{MAX_SESSIONS})", Color.LightGreen);
            }
            else
            {
                Log($"⚠️ 최대 세션 수 초과 ({MAX_SESSIONS}개)");
            }
        }

        /// <summary>
        /// 세션 패널들의 크기를 재조정 (균등 분할)
        /// </summary>
        private void ResizeSessionPanels()
        {
            if (_sessionPanels.Count == 0) return;

            // ✅ panelSessions의 Padding 값 반영
            int topPadding = panelSessions.Padding.Top;
            int bottomPadding = panelSessions.Padding.Bottom;
            int leftPadding = panelSessions.Padding.Left;
            int rightPadding = panelSessions.Padding.Right;
            int spacing = 10;

            // 사용 가능한 영역 계산 (Padding 포함)
            int availableWidth = panelSessions.ClientSize.Width - leftPadding - rightPadding;
            int availableHeight = panelSessions.ClientSize.Height - topPadding - bottomPadding - (spacing * (_sessionPanels.Count - 1));

            // 각 패널의 높이 (균등 분할)
            int panelHeight = availableHeight / _sessionPanels.Count;

            // 각 패널 크기 및 위치 설정
            for (int i = 0; i < _sessionPanels.Count; i++)
            {
                _sessionPanels[i].Size = new Size(availableWidth, panelHeight);
                // ✅ Y 좌표에 topPadding 반영
                _sessionPanels[i].Location = new Point(leftPadding, topPadding + (i * (panelHeight + spacing)));
            }
        }

        /// <summary>
        /// 클라이언트 연결 해제
        /// </summary>
        private void HandleClientDisconnected(JObject data)
        {
            string clientId = data["client_id"]?.ToString();

            if (_sessions.ContainsKey(clientId))
            {
                var session = _sessions[clientId];

                // 패널 제거
                if (session.Panel != null)
                {
                    panelSessions.Controls.Remove(session.Panel);
                    _sessionPanels.Remove(session.Panel);
                    session.Panel.Dispose();
                }

                _sessions.Remove(clientId);
            }

            // ✅ 남은 패널들 재배치
            ResizeSessionPanels();

            Log($"고객 연결 해제: {clientId}");
            UpdateStatus($"대기 중 ({_sessionPanels.Count}/{MAX_SESSIONS})",
                _sessionPanels.Count > 0 ? Color.LightGreen : Color.Yellow);
        }

        /// <summary>
        /// 마우스 이벤트를 서버로 전송
        /// </summary>
        private async void SendMouseEvent(string clientId, string action, MouseEventArgs e)
        {
            try
            {
                string button = e.Button == MouseButtons.Left ? "left" :
                               e.Button == MouseButtons.Right ? "right" :
                               e.Button == MouseButtons.Middle ? "middle" : "none";

                int screenWidth = 1920;
                int screenHeight = 1080;

                if (_sessions.ContainsKey(clientId) &&
                    _sessions[clientId].Panel.ScreenPictureBox.Image != null)
                {
                    screenWidth = _sessions[clientId].Panel.ScreenPictureBox.Image.Width;
                    screenHeight = _sessions[clientId].Panel.ScreenPictureBox.Image.Height;
                }

                var message = new
                {
                    type = "mouse_event",
                    target_id = clientId,
                    @event = new
                    {
                        action = action,
                        button = button,
                        x = e.X,
                        y = e.Y,
                        delta = e.Delta,
                        screen_width = screenWidth,
                        screen_height = screenHeight
                    }
                };

                await SendMessage(message);

                if (action != "move")
                {
                    Log($"🖱️ 마우스: {action} ({e.X}, {e.Y}) → {clientId}");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ 마우스 전송 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 키보드 이벤트를 서버로 전송 ✅ 새로 추가!
        /// </summary>
        private async Task SendKeyboardEvent(string clientId, string action, int keyCode,
            bool ctrl, bool alt, bool shift)
        {
            try
            {
                var message = new
                {
                    type = "keyboard_event",
                    target_id = clientId,
                    @event = new
                    {
                        action = action,  // "key_down", "key_up", "key_press"
                        key_code = keyCode,
                        modifiers = new
                        {
                            ctrl = ctrl,
                            alt = alt,
                            shift = shift,
                            win = false
                        }
                    }
                };

                await SendMessage(message);

                // 키 이름 가져오기
                string keyName = ((Keys)keyCode).ToString();
                string modifiers = "";
                if (ctrl) modifiers += "Ctrl+";
                if (alt) modifiers += "Alt+";
                if (shift) modifiers += "Shift+";

                Log($"⌨️ 키보드: {action} ({modifiers}{keyName}) → {clientId}");
            }
            catch (Exception ex)
            {
                Log($"❌ 키보드 전송 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 화면 데이터 수신
        /// </summary>
        private void HandleScreenData(JObject data)
        {
            try
            {
                string clientId = data["client_id"]?.ToString();
                string base64Image = data["data"]?.ToString();
                int width = data["width"]?.ToObject<int>() ?? 0;
                int height = data["height"]?.ToObject<int>() ?? 0;

                Log($"📥 screen_data 수신: clientId={clientId}, 데이터 크기={base64Image?.Length ?? 0} chars, 해상도={width}x{height}");

                if (string.IsNullOrEmpty(base64Image))
                {
                    Log("❌ base64Image가 비어있습니다!");
                    return;
                }

                Log($"🔄 Base64 → 이미지 변환 중...");

                // Base64를 이미지로 변환
                byte[] imageBytes = Convert.FromBase64String(base64Image);

                Log($"✅ Base64 디코딩 완료: {imageBytes.Length} bytes");

                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    Image screenImage = Image.FromStream(ms);

                    Log($"✅ 이미지 생성 완료: {screenImage.Width}x{screenImage.Height}");

                    // UI 업데이트 (메인 스레드에서)
                    this.Invoke(new Action(() =>
                    {
                        DisplayScreen(clientId, screenImage);
                    }));
                }
            }
            catch (Exception ex)
            {
                Log($"❌ 화면 데이터 처리 오류: {ex.Message}");
                Log($"   스택 트레이스: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 화면 표시
        /// </summary>
        private void DisplayScreen(string clientId, Image screenImage)
        {
            Log($"🖼️ 화면 데이터 표시 시도: clientId={clientId}, 이미지 크기={screenImage.Width}x{screenImage.Height}");

            // 연결된 세션 찾기
            if (_sessions.ContainsKey(clientId))
            {
                var session = _sessions[clientId];
                Log($"✅ 세션 찾음: clientId={clientId}");

                // 세션 패널의 PictureBox에 표시
                if (session.Panel != null && session.Panel.ScreenPictureBox != null)
                {
                    Log($"📺 PictureBox에 이미지 설정 중...");

                    // 기존 이미지 Dispose
                    var oldImage = session.Panel.ScreenPictureBox.Image;
                    session.Panel.ScreenPictureBox.Image = screenImage;
                    oldImage?.Dispose();

                    // FPS 업데이트
                    session.Panel.UpdateFps();

                    Log($"✅ 화면 표시 완료!");
                }
                else
                {
                    Log($"❌ Panel 또는 PictureBox가 null입니다. Panel={session.Panel != null}, PictureBox={session.Panel?.ScreenPictureBox != null}");
                }
            }
            else
            {
                Log($"❌ clientId={clientId}에 해당하는 세션을 찾을 수 없습니다.");
                Log($"   현재 등록된 세션: {string.Join(", ", _sessions.Keys)}");
            }
        }

        /// <summary>
        /// 채팅 메시지 수신
        /// </summary>
        private void HandleChatMessage(JObject data)
        {
            string clientId = data["client_id"]?.ToString();
            string message = data["message"]?.ToString();
            string sender = data["sender"]?.ToString();

            Log($"💬 [{sender}] {message}");
        }

        /// <summary>
        /// 에러 처리
        /// </summary>
        private void HandleError(JObject data)
        {
            string error = data["message"]?.ToString();
            Log($"❌ 에러: {error}");
            MessageBox.Show(error, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// 상태 업데이트
        /// </summary>
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

        /// <summary>
        /// 로그 출력
        /// </summary>
        private void Log(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => Log(message)));
                return;
            }

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }

        /// <summary>
        /// 설정 버튼 클릭
        /// </summary>
        private void BtnSettings_Click(object sender, EventArgs e)
        {
            MessageBox.Show("설정 기능은 준비 중입니다.", "알림",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 연결 종료 버튼 클릭
        /// </summary>
        private void BtnDisconnect_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("연결을 종료하시겠습니까?", "확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Disconnect();
                this.Close();
            }
        }

        /// <summary>
        /// WebSocket 연결 종료
        /// </summary>
        private void Disconnect()
        {
            try
            {
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
                _sendLock?.Dispose();  // ✅ SemaphoreSlim 해제

                Log("연결 종료됨");
            }
            catch (Exception ex)
            {
                Log($"종료 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 폼 종료 시
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            Disconnect();
        }
    }
}
