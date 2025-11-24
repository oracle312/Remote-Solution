using System;
using System.Windows.Forms;
using System.IO;
using DioRemoteControl.Client.Forms;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;

namespace DioRemoteControl.Client
{
    static class Program
    {
        /// <summary>
        /// 애플리케이션의 주 진입점입니다.
        /// </summary>
        /// 
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main(string[] args)
        {
            SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // EXE 파일에서 임베디드된 설정 읽기
            string authCode = LoadAuthCode(args);

            if (string.IsNullOrEmpty(authCode))
            {
                MessageBox.Show("인증번호를 찾을 수 없습니다.", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new ClientMainForm(authCode));
        }

        /// <summary>
        /// 인증번호 로드 (EXE에 임베디드되거나 명령줄 인자로 전달)
        /// </summary>
        private static string LoadAuthCode(string[] args)
        {
            try
            {
                // 1. 명령줄 인자 확인
                if (args != null && args.Length > 0)
                {
                    return args[0];
                }

                // 2. EXE 파일 끝에서 설정 읽기
                string exePath = Application.ExecutablePath;

                using (FileStream fs = new FileStream(exePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // 파일이 너무 작으면 스킵
                    if (fs.Length < 100)
                        return null;

                    // 마지막 10KB 읽기
                    long startPos = Math.Max(0, fs.Length - 10240);
                    fs.Seek(startPos, SeekOrigin.Begin);

                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string content = sr.ReadToEnd();

                        // 설정 블록 찾기
                        int startIdx = content.IndexOf("<!--CONFIG_START-->");
                        int endIdx = content.IndexOf("<!--CONFIG_END-->");

                        if (startIdx >= 0 && endIdx > startIdx)
                        {
                            string configJson = content.Substring(
                                startIdx + "<!--CONFIG_START-->".Length,
                                endIdx - startIdx - "<!--CONFIG_START-->".Length
                            ).Trim();

                            // JSON 파싱
                            var config = JObject.Parse(configJson);
                            return config["auth_code"]?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 로그 기록 (선택사항)
                System.Diagnostics.Debug.WriteLine($"Failed to load auth code: {ex.Message}");
            }

            return null;
        }
    }
}
