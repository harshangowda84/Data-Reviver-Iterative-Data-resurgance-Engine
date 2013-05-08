// Copyright (C) 2011  Joey Scarr, Josh Oosterman
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
using System.IO;
using System.Collections.Generic;
using System.Linq;
#if MONO
using Mono.Unix;
#endif

using KFA.Disks;

namespace FileSystems {
	public class LinDiskLoader : DiskLoader {
		protected override List<Disk> LoadDisksInternal() {
			throw new NotImplementedException();
		}

		protected override List<Disk> LoadLogicalVolumesInternal() {
			var files = new string[] {/* "./FAT32.img",*/ "./NTFS.img"/* "/dev/sdb5" */ };
			var disks = new List<Disk>();
			foreach (var file in files) {
				var disk  = new LinLogicalDisk(file);
				disks.Add(disk);
			}
			return disks;
			
			/*var disks = new List<Disk>();
			foreach (var file in Directory.GetFiles("/dev/disk/by-path")) {
				var actual_path = new UnixSymbolicLinkInfo(file).GetContents().FullName;
				Console.WriteLine(actual_path);
				try {
					var disk  = new LinLogicalDisk(actual_path);
					disks.Add(disk);
				} catch (Exception e) {
					Console.WriteLine(e);
				}
			}
			return disks;*/
		}
	}
}
