using System;
using System.Text;
using System.IO;
using KFA.Disks;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KFA.DataStream {
    public static class Util {
        public static string ByteFormat(ulong count) {
            double val = count;
            string units = " bytes";
            if (val > 1024) {
                val /= 1024.0;
                units = "KB";
            }
            if (val > 1024) {
                val /= 1024.0;
                units = "MB";
            }
            if (val > 1024) {
                val /= 1024.0;
                units = "GB";
            }
            if (val > 1024) {
                val /= 1024.0;
                units = "TB";
            }

            return string.Format("{0:0.##}{1}", val, units);
        }

        public static string SpacifyEnum(string s) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++) {
                if (char.IsUpper(s, i)) {
                    sb.Append(' ');
                }
                sb.Append(s[i]);
            }
            return sb.ToString().Trim();
        }

        public static byte[] GetBytes(IDataStream stream) {
            return stream.GetBytes(0, stream.StreamLength);
        }

        public static int GetInt32(IDataStream stream, ulong offset) {
            return BitConverter.ToInt32(stream.GetBytes(offset, 4), 0);
        }

        public static uint GetUInt32(IDataStream stream, ulong offset) {
            return BitConverter.ToUInt32(stream.GetBytes(offset, 4), 0);
        }

        public static short GetInt16(IDataStream stream, ulong offset) {
            return BitConverter.ToInt16(stream.GetBytes(offset, 2), 0);
        }

        public static ushort GetUInt16(IDataStream stream, ulong offset) {
            return BitConverter.ToUInt16(stream.GetBytes(offset, 2), 0);
        }

        public static int GetInt24(IDataStream stream, ulong offset) {
            byte[] bytes32 = new byte[4];
            byte[] bytes24 = stream.GetBytes(offset, 3);
            Array.Copy(bytes24, bytes32, 3);
            int res = BitConverter.ToInt32(bytes32, 0);
            if ((bytes32[2] & 0x80) == 0x80) {
                return res - 0x1000000;
            } else {
                return res;
            }
        }

        public static uint GetUInt24(IDataStream stream, ulong offset) {
            byte[] bytes32 = new byte[4];
            byte[] bytes24 = stream.GetBytes(offset, 3);
            Array.Copy(bytes24, bytes32, 3);
            return BitConverter.ToUInt32(bytes32, 0);
        }

        public static long GetInt64(IDataStream stream, ulong offset) {
            return BitConverter.ToInt64(stream.GetBytes(offset, 8), 0);
        }

        public static ulong GetUInt64(IDataStream stream, ulong offset) {
            return BitConverter.ToUInt64(stream.GetBytes(offset, 8), 0);
        }

        public static ulong GetArbitraryUInt(IDataStream stream, ulong offset, ulong intSize) {
            if (intSize > 8) throw new Exception("We can't get numbers bigger than ulongs");
            //intSize = Math.Min(intSize, 8);
            ulong result = 0;
            byte[] bytesArb = stream.GetBytes(offset, intSize);
            for (int b = 0; (ulong)b < intSize; b++) {
                result += ((ulong)bytesArb[b]) << (b * 8);
            }
            return result;
        }

        public static long GetArbitraryInt(IDataStream stream, ulong offset, ulong intSize) {
            if (intSize > 8) {
                throw new Exception("We can't get numbers bigger than longs");
            } else if (intSize == 8) {
                return GetInt64(stream, offset);
            } else {
                byte[] bytes64 = new byte[8];
                byte[] bytesArb = stream.GetBytes(offset, intSize);
                Array.Copy(bytesArb, bytes64, (int)intSize);
                long res = BitConverter.ToInt64(bytes64, 0);
                if ((bytes64[intSize - 1] & 0x80) == 0x80) {
                    res -= ((long)1 << ((int)intSize * 8));
                }

                if (intSize == 2) {
                    Debug.Assert(GetInt16(stream, offset) == res);
                } else if (intSize == 4) {
                    Debug.Assert(GetInt32(stream, offset) == res);
                }

                return res;
            }
        }

        public static ulong StrLen(IDataStream stream, ulong offset) {
            ulong i = 0;
            while (offset + i < stream.StreamLength && stream.GetByte(offset + i) != 0) i++;
            return i;
        }

        public static ulong StrLen(IDataStream stream, ulong offset, ulong max) {
            ulong i = 0;
            while (i < max && offset + i < stream.StreamLength && stream.GetByte(offset + i) != 0) i++;
            return i;
        }

        public static string GetASCIIString(IDataStream stream, ulong offset, ulong count) {
            count = Math.Min(count, stream.StreamLength - offset);
            return Encoding.ASCII.GetString(stream.GetBytes(offset, count), 0, (int)count);
        }

        public static string GetHexString(IDataStream stream, ulong offset, ulong count) {
            count = Math.Min(count, stream.StreamLength - offset);
            return BitConverter.ToString(stream.GetBytes(offset, count));
        }

        public static string GetUnicodeString(IDataStream stream, ulong offset, ulong count) {
            count = Math.Min(count, stream.StreamLength - offset);
            return Encoding.Unicode.GetString(stream.GetBytes(offset, count), 0, (int)count);
        }


        public static string CreateTemporaryFile(IDataStream stream) {
            ulong BLOCK_SIZE = 1024 * 1024; // Write 1MB at a time
            string tempFile = Path.GetTempFileName();
            BinaryWriter writer = new BinaryWriter(new FileStream(tempFile, FileMode.Create));
            ulong offset = 0;
            while (offset < stream.StreamLength) {
                ulong read = Math.Min(BLOCK_SIZE,stream.StreamLength-offset);
                writer.Write(stream.GetBytes(offset, read));
                offset += read;
            }
            writer.Close();
            return tempFile;
        }

        public static string CreateTemporaryDirectory() {
            string tempFile = Path.GetTempFileName();
            File.Delete(tempFile);
            Directory.CreateDirectory(tempFile);
            return tempFile;
        }

        public static string GetRandomString(int length) {
            Random r = new Random();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++) {
                sb.Append((char)r.Next('a', 'z' + 1));
            }
            return sb.ToString();
        }

        public static ulong GetDiskSize(SafeFileHandle handle) {
            uint dummy;
            DISK_GEOMETRY diskGeo = new DISK_GEOMETRY();
            Win32.DeviceIoControl(handle, EIOControlCode.DiskGetDriveGeometry, IntPtr.Zero, 0,
                                ref diskGeo, (uint)Marshal.SizeOf(typeof(DISK_GEOMETRY)), out dummy, IntPtr.Zero);
            return (ulong)((DISK_GEOMETRY)diskGeo).DiskSize;
        }

        /// <summary>
        /// Removes any special characters from the string.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string Sanitise(string message) {
            byte[] asciiBytes = ASCIIEncoding.ASCII.GetBytes(message);
            byte[] res = new byte[asciiBytes.Length];
            int bytesread = 0;
            for (int i = 0; i < asciiBytes.Length; ++i) {
                if (asciiBytes[i] >= 32 && asciiBytes[i] <= 126) {
                    res[bytesread] = asciiBytes[i];
                    bytesread++;
                }
            }
            return ASCIIEncoding.ASCII.GetString(res);
        }
    }
}
