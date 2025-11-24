using System;
using System.Net;
using System.Windows.Forms;
using DioRemoteControl.Agent.Forms;
using System.Net.Security;
using System.Runtime.InteropServices;

namespace DioRemoteControl.Agent
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 |
                SecurityProtocolType.Tls11 |
                SecurityProtocolType.Tls;

            ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;

            SetProcessDPIAware();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}