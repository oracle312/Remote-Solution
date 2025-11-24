using System;
using System.Drawing;
using System.Windows.Forms;

namespace DioRemoteControl.Agent.Controls
{
    /// <summary>
    /// 세션 패널 - 화면 표시 및 원격 제어
    /// </summary>
    public partial class SessionPanel : UserControl
    {
        private PictureBox _pictureScreen;
        private Label _lblFps;
        private Label _lblResolution;
        private Label _lblControl;
        private Panel _screenPanel;

        // FPS 계산
        private DateTime _lastFrameTime = DateTime.Now;
        private int _frameCount = 0;
        private double _currentFps = 0;

        // 원격 화면 정보
        private int _remoteWidth = 1920;
        private int _remoteHeight = 1080;

        public PictureBox ScreenPictureBox => _pictureScreen;

        // 마우스 이벤트
        public event EventHandler<MouseEventArgs> RemoteMouseMove;
        public event EventHandler<MouseEventArgs> RemoteMouseClick;
        public event EventHandler<MouseEventArgs> RemoteMouseDown;
        public event EventHandler<MouseEventArgs> RemoteMouseUp;

        // ✅ 키보드 이벤트
        public event EventHandler<KeyEventArgs> RemoteKeyDown;
        public event EventHandler<KeyEventArgs> RemoteKeyUp;

        public SessionPanel()
        {
            InitializeScreenPanel();
        }

        /// <summary>
        /// 화면 표시 패널 초기화
        /// </summary>
        private void InitializeScreenPanel()
        {
            // ✅ UserControl 자체 설정
            this.Size = new Size(800, 600);
            this.TabStop = true;  // Tab으로 포커스 받을 수 있음

            // 화면 표시 패널
            _screenPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                TabStop = true  // ✅ 포커스 받을 수 있음
            };

            // 화면 PictureBox
            _pictureScreen = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                TabStop = true  // ✅ 포커스 받을 수 있음
            };

            // ✅ 마우스 이벤트 등록
            _pictureScreen.MouseMove += PictureScreen_MouseMove;
            _pictureScreen.MouseClick += PictureScreen_MouseClick;
            _pictureScreen.MouseDown += PictureScreen_MouseDown;
            _pictureScreen.MouseUp += PictureScreen_MouseUp;

            // ✅ 키보드 이벤트 등록 (여러 컨트롤에서 받기)
            this.KeyDown += SessionPanel_KeyDown;
            this.KeyUp += SessionPanel_KeyUp;
            _screenPanel.KeyDown += SessionPanel_KeyDown;
            _screenPanel.KeyUp += SessionPanel_KeyUp;
            _pictureScreen.KeyDown += SessionPanel_KeyDown;
            _pictureScreen.KeyUp += SessionPanel_KeyUp;

            // ✅ 클릭 시 포커스 주기
            _pictureScreen.MouseClick += (s, e) =>
            {
                _pictureScreen.Focus();
                this.Focus();
                _lblControl.ForeColor = Color.LightGreen;
            };

            // 상태 정보 패널
            Panel statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(5)
            };

            _lblFps = new Label
            {
                Text = "FPS: 0",
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9),
                AutoSize = true,
                Location = new Point(10, 7)
            };

            _lblResolution = new Label
            {
                Text = "해상도: -",
                ForeColor = Color.LightGray,
                Font = new Font("Consolas", 9),
                AutoSize = true,
                Location = new Point(100, 7)
            };

            _lblControl = new Label
            {
                Text = "🖱️ 제어 활성",
                ForeColor = Color.Yellow,
                Font = new Font("Consolas", 9),
                AutoSize = true,
                Location = new Point(250, 7)
            };

            statusPanel.Controls.AddRange(new Control[] { _lblFps, _lblResolution, _lblControl });

            _screenPanel.Controls.Add(_pictureScreen);
            _screenPanel.Controls.Add(statusPanel);

            this.Controls.Add(_screenPanel);
        }

        /// <summary>
        /// ✅ 키보드 다운 이벤트
        /// </summary>
        private void SessionPanel_KeyDown(object sender, KeyEventArgs e)
        {
            RemoteKeyDown?.Invoke(this, e);
            e.Handled = true;  // 이벤트 전파 중단
            e.SuppressKeyPress = true;  // 소리 방지
        }

        /// <summary>
        /// ✅ 키보드 업 이벤트
        /// </summary>
        private void SessionPanel_KeyUp(object sender, KeyEventArgs e)
        {
            RemoteKeyUp?.Invoke(this, e);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        /// <summary>
        /// 마우스 이동
        /// </summary>
        private void PictureScreen_MouseMove(object sender, MouseEventArgs e)
        {
            var remotePoint = ConvertToRemoteCoordinates(e.X, e.Y);
            var args = new MouseEventArgs(e.Button, e.Clicks, remotePoint.X, remotePoint.Y, e.Delta);
            RemoteMouseMove?.Invoke(this, args);
        }

        /// <summary>
        /// 마우스 클릭
        /// </summary>
        private void PictureScreen_MouseClick(object sender, MouseEventArgs e)
        {
            var remotePoint = ConvertToRemoteCoordinates(e.X, e.Y);
            var args = new MouseEventArgs(e.Button, e.Clicks, remotePoint.X, remotePoint.Y, e.Delta);
            RemoteMouseClick?.Invoke(this, args);
        }

        /// <summary>
        /// 마우스 다운
        /// </summary>
        private void PictureScreen_MouseDown(object sender, MouseEventArgs e)
        {
            var remotePoint = ConvertToRemoteCoordinates(e.X, e.Y);
            var args = new MouseEventArgs(e.Button, e.Clicks, remotePoint.X, remotePoint.Y, e.Delta);
            RemoteMouseDown?.Invoke(this, args);
        }

        /// <summary>
        /// 마우스 업
        /// </summary>
        private void PictureScreen_MouseUp(object sender, MouseEventArgs e)
        {
            var remotePoint = ConvertToRemoteCoordinates(e.X, e.Y);
            var args = new MouseEventArgs(e.Button, e.Clicks, remotePoint.X, remotePoint.Y, e.Delta);
            RemoteMouseUp?.Invoke(this, args);
        }

        /// <summary>
        /// PictureBox 좌표를 원격 화면 좌표로 변환
        /// </summary>
        private Point ConvertToRemoteCoordinates(int x, int y)
        {
            if (_pictureScreen.Image == null)
                return new Point(x, y);

            // PictureBox 크기
            int boxWidth = _pictureScreen.ClientSize.Width;
            int boxHeight = _pictureScreen.ClientSize.Height;

            // 이미지 크기
            int imgWidth = _pictureScreen.Image.Width;
            int imgHeight = _pictureScreen.Image.Height;

            // Zoom 모드: 비율 유지하며 fit
            float boxRatio = (float)boxWidth / boxHeight;
            float imgRatio = (float)imgWidth / imgHeight;

            int displayWidth, displayHeight;
            int offsetX = 0, offsetY = 0;

            if (imgRatio > boxRatio)
            {
                // 이미지가 더 넓음 → 가로 기준 fit
                displayWidth = boxWidth;
                displayHeight = (int)(boxWidth / imgRatio);
                offsetY = (boxHeight - displayHeight) / 2;
            }
            else
            {
                // 이미지가 더 높음 → 세로 기준 fit
                displayHeight = boxHeight;
                displayWidth = (int)(boxHeight * imgRatio);
                offsetX = (boxWidth - displayWidth) / 2;
            }

            // 클릭 좌표가 이미지 영역 밖이면 보정
            int localX = x - offsetX;
            int localY = y - offsetY;

            if (localX < 0) localX = 0;
            if (localY < 0) localY = 0;
            if (localX >= displayWidth) localX = displayWidth - 1;
            if (localY >= displayHeight) localY = displayHeight - 1;

            // 원격 화면 좌표로 변환
            int remoteX = (int)((float)localX / displayWidth * imgWidth);
            int remoteY = (int)((float)localY / displayHeight * imgHeight);

            return new Point(remoteX, remoteY);
        }

        /// <summary>
        /// 원격 화면 크기 설정
        /// </summary>
        public void SetRemoteResolution(int width, int height)
        {
            _remoteWidth = width;
            _remoteHeight = height;
            _lblResolution.Text = $"해상도: {width}x{height}";
        }

        /// <summary>
        /// FPS 업데이트
        /// </summary>
        public void UpdateFps()
        {
            _frameCount++;

            var now = DateTime.Now;
            var elapsed = (now - _lastFrameTime).TotalSeconds;

            if (elapsed >= 1.0)
            {
                _currentFps = _frameCount / elapsed;
                _frameCount = 0;
                _lastFrameTime = now;

                _lblFps.Text = $"FPS: {_currentFps:F1}";
            }

            // 해상도 업데이트
            if (_pictureScreen.Image != null)
            {
                _lblResolution.Text = $"해상도: {_pictureScreen.Image.Width}x{_pictureScreen.Image.Height}";
            }
        }

        /// <summary>
        /// 화면 지우기
        /// </summary>
        public void ClearScreen()
        {
            if (_pictureScreen.Image != null)
            {
                var oldImage = _pictureScreen.Image;
                _pictureScreen.Image = null;
                oldImage.Dispose();
            }

            _lblFps.Text = "FPS: 0";
            _lblResolution.Text = "해상도: -";
            _currentFps = 0;
            _frameCount = 0;
        }

        /// <summary>
        /// ✅ 포커스 강제로 주기
        /// </summary>
        public void GrabFocus()
        {
            this.Focus();
            _pictureScreen.Focus();
            _lblControl.ForeColor = Color.LightGreen;
        }
    }
}