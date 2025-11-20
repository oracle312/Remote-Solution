using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DioRemoteControl.Client.Core
{
    /// <summary>
    /// 마우스 및 키보드 입력 시뮬레이터
    /// </summary>
    public class InputSimulator
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        // Mouse events
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const int MOUSEEVENTF_WHEEL = 0x0800;
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;

        // Keyboard events
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        // System metrics
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        #endregion

        /// <summary>
        /// 마우스 이동
        /// </summary>
        public void MouseMove(int x, int y)
        {
            SetCursorPos(x, y);
        }

        /// <summary>
        /// 상대 좌표로 마우스 이동 (원격 화면 좌표 -> 로컬 화면 좌표 변환)
        /// </summary>
        public void MouseMove(int x, int y, int remoteWidth, int remoteHeight)
        {
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            // 비율 계산
            double ratioX = (double)screenWidth / remoteWidth;
            double ratioY = (double)screenHeight / remoteHeight;

            int localX = (int)(x * ratioX);
            int localY = (int)(y * ratioY);

            SetCursorPos(localX, localY);
        }

        /// <summary>
        /// 마우스 왼쪽 버튼 클릭
        /// </summary>
        public void MouseLeftClick(int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            System.Threading.Thread.Sleep(10);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        /// <summary>
        /// 마우스 왼쪽 버튼 다운
        /// </summary>
        public void MouseLeftDown(int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        }

        /// <summary>
        /// 마우스 왼쪽 버튼 업
        /// </summary>
        public void MouseLeftUp(int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        /// <summary>
        /// 마우스 오른쪽 버튼 클릭
        /// </summary>
        public void MouseRightClick(int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            System.Threading.Thread.Sleep(10);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }

        /// <summary>
        /// 마우스 오른쪽 버튼 다운
        /// </summary>
        public void MouseRightDown(int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
        }

        /// <summary>
        /// 마우스 오른쪽 버튼 업
        /// </summary>
        public void MouseRightUp(int x, int y)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }

        /// <summary>
        /// 마우스 휠 스크롤
        /// </summary>
        public void MouseWheel(int delta)
        {
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, delta, 0);
        }

        /// <summary>
        /// 키 누르기 (다운 + 업)
        /// </summary>
        public void KeyPress(Keys key)
        {
            KeyDown(key);
            System.Threading.Thread.Sleep(10);
            KeyUp(key);
        }

        /// <summary>
        /// 키 다운
        /// </summary>
        public void KeyDown(Keys key)
        {
            keybd_event((byte)key, 0, 0, UIntPtr.Zero);
        }

        /// <summary>
        /// 키 업
        /// </summary>
        public void KeyUp(Keys key)
        {
            keybd_event((byte)key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        /// <summary>
        /// 텍스트 입력
        /// </summary>
        public void TypeText(string text)
        {
            foreach (char c in text)
            {
                SendKeys.SendWait(c.ToString());
                System.Threading.Thread.Sleep(10);
            }
        }

        /// <summary>
        /// 단축키 실행 (예: Ctrl+C)
        /// </summary>
        public void SendShortcut(Keys modifiers, Keys key)
        {
            // 수정키 누르기
            if ((modifiers & Keys.Control) == Keys.Control)
                KeyDown(Keys.ControlKey);
            if ((modifiers & Keys.Alt) == Keys.Alt)
                KeyDown(Keys.Menu);
            if ((modifiers & Keys.Shift) == Keys.Shift)
                KeyDown(Keys.ShiftKey);

            // 키 누르기
            KeyPress(key);

            // 수정키 떼기
            if ((modifiers & Keys.Shift) == Keys.Shift)
                KeyUp(Keys.ShiftKey);
            if ((modifiers & Keys.Alt) == Keys.Alt)
                KeyUp(Keys.Menu);
            if ((modifiers & Keys.Control) == Keys.Control)
                KeyUp(Keys.ControlKey);
        }

        /// <summary>
        /// Ctrl+Alt+Del 시뮬레이션 (작업 관리자 실행으로 대체)
        /// </summary>
        public void SendCtrlAltDel()
        {
            // Ctrl+Alt+Del은 보안상 직접 시뮬레이션 불가
            // 대신 작업 관리자 실행
            System.Diagnostics.Process.Start("taskmgr.exe");
        }

        /// <summary>
        /// 현재 마우스 위치 가져오기
        /// </summary>
        public System.Drawing.Point GetMousePosition()
        {
            POINT point;
            GetCursorPos(out point);
            return new System.Drawing.Point(point.X, point.Y);
        }
    }
}
