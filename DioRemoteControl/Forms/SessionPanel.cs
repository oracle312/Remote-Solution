using System;
using System.Drawing;
using System.Windows.Forms;

namespace DioRemoteControl.Agent.Controls
{
    /// <summary>
    /// 세션 패널 - 화면 표시 및 마우스/키보드 제어 기능 추가
    /// </summary>
    public partial class SessionPanel : UserControl
    {
        private PictureBox _pictureScreen;
        private Label _lblFps;
        private Label _lblResolution;
        private Panel _screenPanel;

        // FPS 계산
        private DateTime _lastFrameTime = DateTime.Now;
        private int _frameCount = 0;
        private double _currentFps = 0;

        // 이벤트 - 마우스/키보드 제어를 위해
        public event EventHandler<MouseEventArgs> RemoteMouseMove;
        public event EventHandler<MouseEventArgs> RemoteMouseClick;
        public event EventHandler<MouseEventArgs> RemoteMouseDown;
        public event EventHandler<MouseEventArgs> RemoteMouseUp;
        public event EventHandler<KeyEventArgs> RemoteKeyDown;
        public event EventHandler<KeyEventArgs> RemoteKeyUp;

        public PictureBox ScreenPictureBox => _pictureScreen;

        public SessionPanel()
        {
            
            InitializeScreenPanel();
        }

        /// <summary>
        /// 화면 표시 패널 초기화
        /// </summary>
        private void InitializeScreenPanel()
        {
            // 화면 표시 패널
            _screenPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 화면 PictureBox
            _pictureScreen = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom, // 비율 유지하며 확대/축소
                BackColor = Color.Black,
                Cursor = Cursors.Cross // 원격 제어 중임을 표시
            };

            // ========================================
            // 🖱️ 마우스 이벤트 핸들러 등록
            // ========================================
            _pictureScreen.MouseMove += PictureScreen_MouseMove;
            _pictureScreen.MouseClick += PictureScreen_MouseClick;
            _pictureScreen.MouseDown += PictureScreen_MouseDown;
            _pictureScreen.MouseUp += PictureScreen_MouseUp;

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

            Label lblControl = new Label
            {
                Text = "🖱️ 제어 활성",
                ForeColor = Color.Yellow,
                Font = new Font("Consolas", 9),
                AutoSize = true,
                Location = new Point(250, 7)
            };

            statusPanel.Controls.AddRange(new Control[] { _lblFps, _lblResolution, lblControl });

            _screenPanel.Controls.Add(_pictureScreen);
            _screenPanel.Controls.Add(statusPanel);

            this.Controls.Add(_screenPanel);
        }

        // ========================================
        // 🖱️ 마우스 이벤트 처리
        // ========================================

        /// <summary>
        /// 마우스 이동 이벤트
        /// </summary>
        private void PictureScreen_MouseMove(object sender, MouseEventArgs e)
        {
            // 원격 화면의 실제 좌표로 변환
            var remoteCoords = ConvertToRemoteCoordinates(e.Location);

            // 이벤트 발생 (MainForm에서 처리)
            RemoteMouseMove?.Invoke(this, new MouseEventArgs(
                e.Button,
                e.Clicks,
                remoteCoords.X,
                remoteCoords.Y,
                e.Delta
            ));
        }

        /// <summary>
        /// 마우스 클릭 이벤트
        /// </summary>
        private void PictureScreen_MouseClick(object sender, MouseEventArgs e)
        {
            var remoteCoords = ConvertToRemoteCoordinates(e.Location);
            RemoteMouseClick?.Invoke(this, new MouseEventArgs(
                e.Button,
                e.Clicks,
                remoteCoords.X,
                remoteCoords.Y,
                e.Delta
            ));
        }

        /// <summary>
        /// 마우스 다운 이벤트
        /// </summary>
        private void PictureScreen_MouseDown(object sender, MouseEventArgs e)
        {
            var remoteCoords = ConvertToRemoteCoordinates(e.Location);
            RemoteMouseDown?.Invoke(this, new MouseEventArgs(
                e.Button,
                e.Clicks,
                remoteCoords.X,
                remoteCoords.Y,
                e.Delta
            ));
        }

        /// <summary>
        /// 마우스 업 이벤트
        /// </summary>
        private void PictureScreen_MouseUp(object sender, MouseEventArgs e)
        {
            var remoteCoords = ConvertToRemoteCoordinates(e.Location);
            RemoteMouseUp?.Invoke(this, new MouseEventArgs(
                e.Button,
                e.Clicks,
                remoteCoords.X,
                remoteCoords.Y,
                e.Delta
            ));
        }

        /// <summary>
        /// PictureBox 좌표를 원격 화면의 실제 좌표로 변환
        /// </summary>
        private Point ConvertToRemoteCoordinates(Point pictureBoxPoint)
        {
            if (_pictureScreen.Image == null)
                return pictureBoxPoint;

            // PictureBox의 실제 표시 영역 계산 (Zoom 모드)
            int imageWidth = _pictureScreen.Image.Width;
            int imageHeight = _pictureScreen.Image.Height;
            int boxWidth = _pictureScreen.ClientSize.Width;
            int boxHeight = _pictureScreen.ClientSize.Height;

            // 비율 계산
            float imageRatio = (float)imageWidth / imageHeight;
            float boxRatio = (float)boxWidth / boxHeight;

            float scaleX, scaleY;
            int offsetX = 0, offsetY = 0;

            if (imageRatio > boxRatio)
            {
                // 이미지가 더 넓음 (좌우에 꽉 참)
                scaleX = (float)boxWidth / imageWidth;
                scaleY = scaleX;
                int displayHeight = (int)(imageHeight * scaleY);
                offsetY = (boxHeight - displayHeight) / 2;
            }
            else
            {
                // 이미지가 더 높음 (상하에 꽉 참)
                scaleY = (float)boxHeight / imageHeight;
                scaleX = scaleY;
                int displayWidth = (int)(imageWidth * scaleX);
                offsetX = (boxWidth - displayWidth) / 2;
            }

            // PictureBox 좌표를 원본 이미지 좌표로 변환
            int remoteX = (int)((pictureBoxPoint.X - offsetX) / scaleX);
            int remoteY = (int)((pictureBoxPoint.Y - offsetY) / scaleY);

            // 범위 제한
            remoteX = Math.Max(0, Math.Min(remoteX, imageWidth - 1));
            remoteY = Math.Max(0, Math.Min(remoteY, imageHeight - 1));

            return new Point(remoteX, remoteY);
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
    }
}