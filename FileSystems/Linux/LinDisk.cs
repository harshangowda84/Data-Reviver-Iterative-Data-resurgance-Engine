using System;
using System.IO;

namespace KFA.Disks {
		public abstract class LinDisk : Disk {
				protected FileStream Handle { get; set; }

				#region Disk Members
				protected override byte[] ForceReadBytes(ulong offset, ulong length) {
						byte[] result = new byte[length];

						Handle.Position = (long)offset;
						int bytes_read = Handle.Read(result, 0, (int)length);
						if (bytes_read != (int)length)
							throw new Exception("IO Error. Bug in Linux version: Tried to read O:" + offset + ", L:" + length);

						return result;
				}
				#endregion
		}
}
