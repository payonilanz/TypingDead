using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using TypingDead.Forms;
using TypingDead.Tests;

namespace TypingDead
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (HasSelfTestArgument(args))
            {
                RunSelfTest();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
            {
                ShowFatalError(e.Exception);
            };

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                ShowFatalError(ex);
            }
        }

        private static bool HasSelfTestArgument(string[] args)
        {
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++)
            {
                if (String.Equals(args[i], "--selftest", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(args[i], "/selftest", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static void RunSelfTest()
        {
            string report;
            try
            {
                report = SelfTest.Run();
            }
            catch (Exception ex)
            {
                report = "TYPING DEAD // SELF TEST\r\nRESULT: FAIL\r\nUnexpected failure: " +
                    ex.GetType().Name + ": " + ex.Message + "\r\n" + ex.StackTrace;
            }

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "selftest-results.txt");
            try
            {
                File.WriteAllText(path, report, Encoding.UTF8);
            }
            catch
            {
                // The exit code still communicates success or failure.
            }

            Environment.ExitCode = report.IndexOf("RESULT: PASS", StringComparison.Ordinal) >= 0 ? 0 : 1;
        }

        private static void ShowFatalError(Exception ex)
        {
            try
            {
                string message = "TYPING DEAD encountered an unexpected error.\r\n\r\n" +
                    ex.GetType().Name + ": " + ex.Message +
                    "\r\n\r\nIf this keeps happening, delete the settings file in " +
                    "%AppData%\\TypingDead and try again.";
                MessageBox.Show(message, "TYPING DEAD // FAULT", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                // Nothing more can be reported if the UI itself is unavailable.
            }
        }
    }
}
