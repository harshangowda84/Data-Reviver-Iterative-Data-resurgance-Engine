using DataReviver;
// Copyright (C) 2017  Joey Scarr, Lukas Korsika
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;
using DataReviver;
using DataReviver;

namespace DataReviver {
	static class Program {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// Generate DR icon before any forms are shown
			MainForm.GenerateDRIcon();

			if (IsWindows())
				AttachConsole(-1);

			ParseArgs(args);
			EnsureUserIsAdmin();

			if (!IsWindows()) {
				PrintUsage();
				Environment.Exit(0);
			}

			// Show new login form first
			bool loggedIn = false;
			while (!loggedIn)
			{
				using (var loginForm = new NewLoginForm())
				{
					var result = loginForm.ShowDialog();
					if (result == DialogResult.OK && loginForm.LoginSuccessful)
					{
						// Show improved case prompt form instead of MessageBox
						DataReviver.CaseManager caseManager = new DataReviver.CaseManager();
						DataReviver.ForensicCase selectedCase = null;
						DialogResult caseResult = DialogResult.None;
						while (selectedCase == null && caseResult != DialogResult.Cancel)
						{
							var casePrompt = new CasePromptForm();
							var promptResult = casePrompt.ShowDialog();
							if (promptResult == DialogResult.Yes)
							{
								selectedCase = caseManager.CreateNewCase();
								if (selectedCase == null) continue; // If cancelled, show case prompt again
								caseResult = DialogResult.OK;
							}
							else if (promptResult == DialogResult.No)
							{
								selectedCase = caseManager.OpenExistingCase();
								if (selectedCase == null) continue; // If cancelled, show case prompt again
								caseResult = DialogResult.OK;
							}
							else
							{
								caseResult = DialogResult.Cancel;
							}
						}
						if (selectedCase != null)
						{
							Application.Run(new MainForm(loginForm.CurrentUser, selectedCase));
							loggedIn = true;
						}
						// If Back pressed (DialogResult.Cancel), loop to show login again
					}
					else
					{
						// Login failed or cancelled, exit application
						Environment.Exit(0);
					}
				}
			}
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool AttachConsole(int dwProcessId);

		static void PrintUsage() {
			Console.Error.WriteLine("Usage: DataReviver {\n" +
															"         -listdisks: Show all attached disks, and their filesystems.\n" +
															"         -listfiles <FS>: List deleted files on the disk with name <FS>.\n" +
															"         -dumpfile <FS> <File>: Write the contents of file <File> on disk <FS> to stdout.\n" +
															"}");
		}

		static void ParseArgs(string[] args) {
			for (var i = 0; i < args.Count(); i++) {
				if (string.Equals(args[i], "-listdisks", StringComparison.OrdinalIgnoreCase)) {
					ConsoleCommands.ListDisks();
					Environment.Exit(0);
				} else if (string.Equals(args[i], "-listfiles", StringComparison.OrdinalIgnoreCase)) {
					if (i + 1 >= args.Count()) {
						PrintUsage();
						Console.Error.WriteLine("Expected: Disk name");
						Environment.Exit(1);
					}
					var disk = args[i + 1];
					ConsoleCommands.ListFiles(disk);
					Environment.Exit(0);
				} else if (string.Equals(args[i], "-dumpfile", StringComparison.OrdinalIgnoreCase)) {
					if (i + 2 >= args.Count()) {
						PrintUsage();
						Console.Error.WriteLine("Expected: Disk name and file name.");
						Environment.Exit(1);
					}
					var disk = args[i + 1];
					var file = args[i + 2];
					ConsoleCommands.DumpFile(disk, file);
					Environment.Exit(0);
				} else {
					PrintUsage();
					Console.Error.WriteLine("Unknown parameter: " + args[i]);
					Environment.Exit(1);
				}
			}
		}

		static bool IsWindows() {
			int p = (int)Environment.OSVersion.Platform;
			return ((p != 4) && (p != 6) && (p != 128));
		}

		static void EnsureUserIsAdmin() {
			if (IsWindows()) {
				WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
				if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
				{
					using (var dialog = new AdminWarningDialogForm())
					{
						if (dialog.ShowDialog() == DialogResult.OK && dialog.UserAccepted)
						{
							RelaunchAsAdmin();
						}
					}
				}
			}
		}

		static void RelaunchAsAdmin() {
			try {
				ProcessStartInfo psi = new ProcessStartInfo(Application.ExecutablePath);
				psi.Verb = "runas";
				Process.Start(psi);
			} catch { }

			Environment.Exit(0);
		}
	}
}

