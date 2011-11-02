using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Security.Principal;
using System.Diagnostics;

namespace KickassUndelete {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
			EnsureUserIsAdmin();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

		static void EnsureUserIsAdmin() {
			WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
			if (!principal.IsInRole(WindowsBuiltInRole.Administrator)) {
				RelaunchAsAdmin();
			}
		}

		static void RelaunchAsAdmin() {
			ProcessStartInfo psi = new ProcessStartInfo(Application.ExecutablePath);
			psi.Verb = "runas";
			Process.Start(psi);

			Environment.Exit(0);
		}
    }
}
