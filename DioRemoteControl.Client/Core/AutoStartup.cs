using System;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;

namespace DioRemoteControl.Client.Core
{
    /// <summary>
    /// 재부팅 후 자동 재접속을 위한 자동 시작 클래스
    /// </summary>
    public class AutoStartup
    {
        private const string APP_NAME = "DioRemoteClient";
        private const string REGISTRY_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// 자동 시작 등록
        /// </summary>
        public static bool EnableAutoStartup()
        {
            try
            {
                string exePath = Application.ExecutablePath;

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
                {
                    if (key != null)
                    {
                        key.SetValue(APP_NAME, $"\"{exePath}\"");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to enable auto startup: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 자동 시작 해제
        /// </summary>
        public static bool DisableAutoStartup()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(APP_NAME, false);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to disable auto startup: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 자동 시작 상태 확인
        /// </summary>
        public static bool IsAutoStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, false))
                {
                    if (key != null)
                    {
                        object value = key.GetValue(APP_NAME);
                        return value != null;
                    }
                }
            }
            catch
            {
                // 예외 발생 시 false 반환
            }

            return false;
        }

        /// <summary>
        /// 작업 스케줄러를 사용한 자동 시작 (관리자 권한 필요)
        /// </summary>
        public static bool EnableAutoStartupWithTask()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                string taskName = "DioRemoteClient";

                // schtasks 명령으로 작업 생성
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "schtasks",
                    Arguments = $"/create /tn \"{taskName}\" /tr \"\\\"{exePath}\\\"\" /sc onlogon /rl highest /f",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var process = System.Diagnostics.Process.Start(psi);
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create task: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 작업 스케줄러 작업 삭제
        /// </summary>
        public static bool DisableAutoStartupTask()
        {
            try
            {
                string taskName = "DioRemoteClient";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "schtasks",
                    Arguments = $"/delete /tn \"{taskName}\" /f",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                var process = System.Diagnostics.Process.Start(psi);
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete task: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 재부팅 후 재접속을 위한 설정 저장
        /// </summary>
        public static void SaveReconnectInfo(string authCode, string sessionId)
        {
            try
            {
                string configPath = GetConfigFilePath();
                var config = new
                {
                    auth_code = authCode,
                    session_id = sessionId,
                    saved_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save reconnect info: {ex.Message}");
            }
        }

        /// <summary>
        /// 재접속 정보 로드
        /// </summary>
        public static (string authCode, string sessionId) LoadReconnectInfo()
        {
            try
            {
                string configPath = GetConfigFilePath();
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    dynamic config = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                    return (config.auth_code?.ToString(), config.session_id?.ToString());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load reconnect info: {ex.Message}");
            }

            return (null, null);
        }

        /// <summary>
        /// 재접속 정보 삭제
        /// </summary>
        public static void ClearReconnectInfo()
        {
            try
            {
                string configPath = GetConfigFilePath();
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }
            }
            catch
            {
                // 삭제 실패 시 무시
            }
        }

        /// <summary>
        /// 설정 파일 경로 가져오기
        /// </summary>
        private static string GetConfigFilePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appDataPath, "DioRemoteClient");

            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            return Path.Combine(configDir, "reconnect.json");
        }
    }
}