using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DioRemoteControl.Client.Core
{
    /// <summary>
    /// 화면 캡처 클래스
    /// </summary>
    public class ScreenCapture
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO pIconInfo);

        [DllImport("user32.dll")]
        private static extern bool DrawIcon(IntPtr hdc, int x, int y, IntPtr hIcon);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public Point ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        private const int CURSOR_SHOWING = 0x00000001;

        private int _quality = 75;
        private ColorDepth _colorDepth = ColorDepth.TrueColor;
        private bool _captureCursor = true;

        public enum ColorDepth
        {
            Color64 = 64,      // 6비트 (64색)
            Color256 = 256,    // 8비트 (256색)
            TrueColor = 16     // 16비트 (True Color)
        }

        /// <summary>
        /// 화면 캡처 품질 설정 (1-100)
        /// </summary>
        public int Quality
        {
            get => _quality;
            set => _quality = Math.Max(1, Math.Min(100, value));
        }

        /// <summary>
        /// 색상 깊이 설정
        /// </summary>
        public ColorDepth Depth
        {
            get => _colorDepth;
            set => _colorDepth = value;
        }

        /// <summary>
        /// 커서 캡처 여부
        /// </summary>
        public bool CaptureCursor
        {
            get => _captureCursor;
            set => _captureCursor = value;
        }

        /// <summary>
        /// 특정 모니터의 화면 캡처
        /// </summary>
        public Bitmap CaptureScreen(int monitorIndex = 0)
        {
            try
            {
                Screen[] screens = Screen.AllScreens;
                if (monitorIndex < 0 || monitorIndex >= screens.Length)
                    monitorIndex = 0;

                Screen screen = screens[monitorIndex];
                Rectangle bounds = screen.Bounds;

                Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    // 화면 캡처
                    g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);

                    // 커서 그리기
                    if (_captureCursor)
                    {
                        DrawCursor(g, bounds);
                    }
                }

                // 색상 깊이 변환
                if (_colorDepth != ColorDepth.TrueColor)
                {
                    bitmap = ConvertColorDepth(bitmap, _colorDepth);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new Exception($"Screen capture failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 전체 화면 캡처 (모든 모니터)
        /// </summary>
        public Bitmap CaptureAllScreens()
        {
            try
            {
                int minX = int.MaxValue;
                int minY = int.MaxValue;
                int maxX = int.MinValue;
                int maxY = int.MinValue;

                foreach (Screen screen in Screen.AllScreens)
                {
                    minX = Math.Min(minX, screen.Bounds.X);
                    minY = Math.Min(minY, screen.Bounds.Y);
                    maxX = Math.Max(maxX, screen.Bounds.Right);
                    maxY = Math.Max(maxY, screen.Bounds.Bottom);
                }

                int width = maxX - minX;
                int height = maxY - minY;

                Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(minX, minY, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

                    if (_captureCursor)
                    {
                        DrawCursor(g, new Rectangle(minX, minY, width, height));
                    }
                }

                if (_colorDepth != ColorDepth.TrueColor)
                {
                    bitmap = ConvertColorDepth(bitmap, _colorDepth);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new Exception($"Multi-screen capture failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Bitmap을 Base64 문자열로 변환
        /// </summary>
        public string BitmapToBase64(Bitmap bitmap)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
                    EncoderParameters encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)_quality);

                    bitmap.Save(ms, jpegCodec, encoderParams);
                    byte[] imageBytes = ms.ToArray();
                    return Convert.ToBase64String(imageBytes);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Bitmap encoding failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 커서 그리기
        /// </summary>
        private void DrawCursor(Graphics g, Rectangle bounds)
        {
            try
            {
                CURSORINFO cursorInfo;
                cursorInfo.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

                if (GetCursorInfo(out cursorInfo) && cursorInfo.flags == CURSOR_SHOWING)
                {
                    IntPtr hIcon = CopyIcon(cursorInfo.hCursor);
                    if (hIcon != IntPtr.Zero)
                    {
                        ICONINFO iconInfo;
                        if (GetIconInfo(hIcon, out iconInfo))
                        {
                            int x = cursorInfo.ptScreenPos.X - bounds.X - iconInfo.xHotspot;
                            int y = cursorInfo.ptScreenPos.Y - bounds.Y - iconInfo.yHotspot;

                            IntPtr hdc = g.GetHdc();
                            DrawIcon(hdc, x, y, hIcon);
                            g.ReleaseHdc(hdc);

                            DeleteObject(iconInfo.hbmColor);
                            DeleteObject(iconInfo.hbmMask);
                        }
                    }
                }
            }
            catch
            {
                // 커서 그리기 실패 시 무시
            }
        }

        /// <summary>
        /// 색상 깊이 변환
        /// </summary>
        private Bitmap ConvertColorDepth(Bitmap original, ColorDepth depth)
        {
            try
            {
                PixelFormat format;
                switch (depth)
                {
                    case ColorDepth.Color64:
                        format = PixelFormat.Format4bppIndexed;
                        break;
                    case ColorDepth.Color256:
                        format = PixelFormat.Format8bppIndexed;
                        break;
                    default:
                        return original;
                }

                Bitmap converted = original.Clone(new Rectangle(0, 0, original.Width, original.Height), format);
                return converted;
            }
            catch
            {
                return original;
            }
        }

        /// <summary>
        /// JPEG 인코더 정보 가져오기
        /// </summary>
        private ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.MimeType == mimeType)
                {
                    return codec;
                }
            }
            return null;
        }

        /// <summary>
        /// 모니터 개수 가져오기
        /// </summary>
        public static int GetMonitorCount()
        {
            return Screen.AllScreens.Length;
        }

        /// <summary>
        /// 모니터 정보 가져오기
        /// </summary>
        public static string GetMonitorInfo()
        {
            var info = new System.Text.StringBuilder();
            var screens = Screen.AllScreens;

            for (int i = 0; i < screens.Length; i++)
            {
                var screen = screens[i];
                info.AppendLine($"Monitor {i + 1}:");
                info.AppendLine($"  Resolution: {screen.Bounds.Width}x{screen.Bounds.Height}");
                info.AppendLine($"  Position: ({screen.Bounds.X}, {screen.Bounds.Y})");
                info.AppendLine($"  Primary: {screen.Primary}");
                info.AppendLine($"  Device: {screen.DeviceName}");
            }

            return info.ToString();
        }
    }
}
