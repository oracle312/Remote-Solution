using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DioRemoteControl.Client.Core
{
    /// <summary>
    /// 시스템 명령어 실행 클래스
    /// </summary>
    public class SystemCommands
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYUP = 0x0002;

        #endregion

        /// <summary>
        /// 시작 메뉴 열기
        /// </summary>
        public void OpenStartMenu()
        {
            try
            {
                SendKeys.SendWait("^{ESC}"); // Ctrl+Esc
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to open Start menu: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 작업 관리자 실행
        /// </summary>
        public void OpenTaskManager()
        {
            try
            {
                Process.Start("taskmgr.exe");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to open Task Manager: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Ctrl+Alt+Del 시뮬레이션 (작업 관리자 실행으로 대체)
        /// 보안상 실제 Ctrl+Alt+Del은 시뮬레이션 불가능
        /// </summary>
        public void SendCtrlAltDel()
        {
            try
            {
                // Ctrl+Alt+Del은 보안 키 조합으로 직접 시뮬레이션 불가
                // 대신 작업 관리자를 실행
                OpenTaskManager();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute Ctrl+Alt+Del: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 명령 프롬프트 실행
        /// </summary>
        public void OpenCommandPrompt()
        {
            try
            {
                Process.Start("cmd.exe");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to open Command Prompt: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 탐색기 열기
        /// </summary>
        public void OpenExplorer(string path = null)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    Process.Start("explorer.exe");
                }
                else
                {
                    Process.Start("explorer.exe", path);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to open Explorer: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 바탕화면 보기
        /// </summary>
        public void ShowDesktop()
        {
            try
            {
                // Win+D 키 조합
                keybd_event((byte)Keys.LWin, 0, 0, UIntPtr.Zero);
                keybd_event((byte)Keys.D, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(10);
                keybd_event((byte)Keys.D, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event((byte)Keys.LWin, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to show desktop: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 컴퓨터 재부팅
        /// </summary>
        public void Reboot(bool force = false)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("shutdown", force ? "/r /f /t 0" : "/r /t 0");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to reboot: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 컴퓨터 종료
        /// </summary>
        public void Shutdown(bool force = false)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("shutdown", force ? "/s /f /t 0" : "/s /t 0");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to shutdown: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 로그오프
        /// </summary>
        public void Logoff()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("shutdown", "/l");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to logoff: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 잠금 화면
        /// </summary>
        public void LockWorkstation()
        {
            try
            {
                Process.Start("rundll32.exe", "user32.dll,LockWorkStation");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to lock workstation: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 모든 창 최소화
        /// </summary>
        public void MinimizeAllWindows()
        {
            try
            {
                // Win+M 키 조합
                keybd_event((byte)Keys.LWin, 0, 0, UIntPtr.Zero);
                keybd_event((byte)Keys.M, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(10);
                keybd_event((byte)Keys.M, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event((byte)Keys.LWin, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to minimize windows: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 특정 프로세스 종료
        /// </summary>
        public void KillProcess(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);
                foreach (Process process in processes)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to kill process: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램 실행
        /// </summary>
        public void StartProgram(string programPath, string arguments = "")
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(programPath, arguments);
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start program: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 시스템 정보 가져오기
        /// </summary>
        public string GetSystemInfo()
        {
            try
            {
                var info = new System.Text.StringBuilder();
                info.AppendLine($"OS: {Environment.OSVersion}");
                info.AppendLine($"Machine: {Environment.MachineName}");
                info.AppendLine($"User: {Environment.UserName}");
                info.AppendLine($"Domain: {Environment.UserDomainName}");
                info.AppendLine($"Processors: {Environment.ProcessorCount}");
                info.AppendLine($"64-bit: {Environment.Is64BitOperatingSystem}");
                info.AppendLine($"System Directory: {Environment.SystemDirectory}");

                return info.ToString();
            }
            catch (Exception ex)
            {
                return $"Failed to get system info: {ex.Message}";
            }
        }

        /// <summary>
        /// 클립보드에 텍스트 복사
        /// </summary>
        public void SetClipboardText(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set clipboard: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 클립보드에서 텍스트 가져오기
        /// </summary>
        public string GetClipboardText()
        {
            try
            {
                return Clipboard.GetText();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get clipboard: {ex.Message}", ex);
            }
        }
    }
}
