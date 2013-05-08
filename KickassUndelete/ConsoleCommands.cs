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
using System.Linq;
using System.Threading;

using FileSystems;
using KFA.Disks;

namespace KickassUndelete 
{
	public class ConsoleCommands {
		public static void ListDisks() {
			Console.WriteLine("Logical Disks:\n==============\n");
			foreach (var disk in DiskLoader.LoadLogicalVolumes()) {
				Console.WriteLine(disk + ": " + ((IFileSystemStore)disk).FS);
			}
		}

		public static void ListFiles(string dev) {
			var volumes = DiskLoader.LoadLogicalVolumes();
			var volume = volumes.FirstOrDefault(x => x.ToString().Contains(dev));
			if (volume == null) {
				Console.WriteLine("Disk not found: " + dev);
				return;
			}
			dev = volume.ToString();

			var fs = ((IFileSystemStore)volume).FS;
			if (fs == null) {
				Console.WriteLine("Disk " + dev + " contains no readable FS.");
				return;
			}

			Console.WriteLine("Deleted files on " + dev);
			Console.WriteLine("=================" + new String('=', dev.Length));
			var scan_state = new ScanState(fs);
			scan_state.ScanFinished += new EventHandler(ScanFinished);
			scan_state.StartScan();
			while (!scan_finished) {
				Thread.Sleep(100);
			}
			var files = scan_state.GetDeletedFiles();
			foreach (var file in files) {
				Console.WriteLine(file.Name);
			}
		}

		public static void DumpFile(string dev, string filename) {
			var volumes = DiskLoader.LoadLogicalVolumes();
			var volume = volumes.FirstOrDefault(x => x.ToString().Contains(dev));
			if (volume == null) {
				Console.WriteLine("Disk not found: " + dev);
				return;
			}
			dev = volume.ToString();

			var fs = ((IFileSystemStore)volume).FS;
			if (fs == null) {
				Console.WriteLine("Disk " + dev + " contains no readable FS.");
				return;
			}

			var scan_state = new ScanState(fs);
			scan_state.ScanFinished += new EventHandler(ScanFinished);
			scan_state.StartScan();
			while (!scan_finished) {
				Thread.Sleep(100);
			}

			var files = scan_state.GetDeletedFiles();
			var file = files.FirstOrDefault(x => x.Name == filename);
			if (file == null) {
				Console.WriteLine("File " + filename + " not found on device " + dev);
				return;
			}

			var node = file.GetFileSystemNode();
			var data = node.GetBytes(0, node.StreamLength);

			var output = Console.OpenStandardOutput();
			output.Write(data, 0, data.Length);
		}

		public static bool scan_finished = false;
		public static void ScanFinished(object ob, EventArgs e) {
			scan_finished = true;
		}
	}
}
