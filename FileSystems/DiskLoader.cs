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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KFA.Disks;
using System.Management;

namespace FileSystems {
    public static class DiskLoader {
        public static List<PhysicalDisk> LoadDisks() {
            List<PhysicalDisk> res = new List<PhysicalDisk>();
            try {
                ManagementScope ms = new ManagementScope();
                ObjectQuery oq = new ObjectQuery("SELECT * FROM Win32_DiskDrive");
                ManagementObjectSearcher mos = new ManagementObjectSearcher(ms, oq);
                ManagementObjectCollection moc = mos.Get();
                foreach (ManagementObject mo in moc) {
                    PhysicalDisk disk = new PhysicalDisk(mo);
                    res.Add(disk);
                }
                
            } catch { }
            return res;
        }

        public static List<LogicalDisk> LoadLogicalVolumes() {
            List<LogicalDisk> res = new List<LogicalDisk>();
            try {
                ManagementScope ms = new ManagementScope();
                ObjectQuery oq = new ObjectQuery("SELECT * FROM Win32_LogicalDisk");
                ManagementObjectSearcher mos = new ManagementObjectSearcher(ms, oq);
                ManagementObjectCollection moc = mos.Get();
                foreach (ManagementObject mo in moc) {
                    try {
                        LogicalDisk disk = new LogicalDisk(mo);
                        res.Add(disk);
                    } catch { }
                }
            } catch { }
            return res;
        }
    }
}
