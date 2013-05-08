// Copyright (C) 2011  Joey Scarr, Lukas Korsika
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
		static void Main(string[] args) {
			ParseArgs(args);

			EnsureUserIsAdmin();

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}

		static void ParseArgs(string[] args) {
			for (var i = 0; i < args.Count(); i++) {
				if (string.Equals(args[i], "-listdisks", StringComparison.OrdinalIgnoreCase)) {
					ConsoleCommands.ListDisks();
					Environment.Exit(0);
				} else if (string.Equals(args[i], "-listfiles", StringComparison.OrdinalIgnoreCase)) {
					if (i + 1 >= args.Count()) {
						Console.WriteLine("Expected: Disk name");
						Environment.Exit(1);
					} 
					var disk = args[i + 1];
					ConsoleCommands.ListFiles(disk);
					Environment.Exit(0);
				} else if (string.Equals(args[i], "-dumpfile", StringComparison.OrdinalIgnoreCase)) {
					if (i + 2 >= args.Count()) {
						Console.WriteLine("Expected: Disk name and file name.");
						Environment.Exit(1);
					}
					var disk = args[i + 1];
					var file = args[i + 2];
					ConsoleCommands.DumpFile(disk, file);
					Environment.Exit(0);
				}else {
					Console.WriteLine("Unknown parameter: " + args[i]);
					Console.WriteLine("Usage: KickassUndelete [-listdisks|-listfiles]");
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
				if (!principal.IsInRole(WindowsBuiltInRole.Administrator)) {
					RelaunchAsAdmin();
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

