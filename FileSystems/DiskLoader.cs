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
