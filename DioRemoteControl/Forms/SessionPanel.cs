using System;
using System.Drawing;
using System.Windows.Forms;

namespace DioRemoteControl.Agent.Controls
{
    /// <summary>
    /// 세션 패널 - 화면 표시 기능 추가
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

        public PictureBox ScreenPictureBox => _pictureScreen;

        public SessionPanel()
        {
            InitializeComponent();
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
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
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

            statusPanel.Controls.AddRange(new Control[] { _lblFps, _lblResolution });

            _screenPanel.Controls.Add(_pictureScreen);
            _screenPanel.Controls.Add(statusPanel);

            this.Controls.Add(_screenPanel);
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
