using System;
using KFA.DataStream;
using Microsoft.Win32.SafeHandles;

namespace KFA.Disks {
    public abstract class Disk : IDataStream {
        public SafeFileHandle Handle { get; protected set; }

        #region IDataStream Members
        /*private const int CACHE_SIZE = 4 * 1024; // 4 KB, a typical cluster size

        long cacheOffset = -1;
        byte[] cachedData;
        private object padlock = new object();
        public byte GetByte(ulong offset) {
            lock (padlock) {
                if (offset >= StreamLength) throw new Exception("Tried to read off the end of the disk!");
                long roundedDown = (long)(offset / CACHE_SIZE) * CACHE_SIZE;
                if (roundedDown != cacheOffset) {
                    // the data is not cached, so seek for it
                    long filePtr;
                    Win32.SetFilePointerEx(Handle, roundedDown, out filePtr, EMoveMethod.Begin);
                    uint numBytesRead;
                    cachedData = new byte[CACHE_SIZE];
                    bool readSuccess = Win32.ReadFile(Handle, cachedData, CACHE_SIZE, out numBytesRead, IntPtr.Zero);
                    if (!readSuccess) {
                        //return 0;
                    }
                }
                cacheOffset = roundedDown;
                return cachedData[(long)offset - roundedDown];
            }
        }

        public byte[] GetBytes(ulong offset, ulong length) {
            byte[] res = new byte[length];
            for (uint i = 0; i < length; i++) {
                res[i] = GetByte(offset + i);
            }
            return res;
        }*/

        
        public byte GetByte(ulong offset) {
            return GetBytes(offset, 1)[0];
        }

        private object padlock = new object();
        private const ulong CACHE_LINE_SIZE = 4 * 1024; // 4 KB, a typical cluster size
        private ulong current_cache_line = ulong.MaxValue;
        private byte[] cache = null;
        public byte[] GetBytes(ulong offset, ulong length) {
            if (length > 0) {
            //    return ForceReadBytes(offset, length);
            }
            byte[] result = new byte[length];
            ulong bytes_read = 0;
            while (bytes_read < length) {
                ulong cache_line = offset / CACHE_LINE_SIZE;
                lock (padlock) {
                    if (current_cache_line != cache_line) {
                        LoadCacheLine(cache_line);
                    }
                    ulong offset_in_cache_line = offset % CACHE_LINE_SIZE;
                    ulong num_to_read = Math.Min(CACHE_LINE_SIZE - offset_in_cache_line, length - bytes_read);
                    Array.Copy(cache, (int)offset_in_cache_line, result, (int)bytes_read, (int)num_to_read);
                    bytes_read += num_to_read;
                    offset += num_to_read;
                }
            }
            return result;
        }

        protected void LoadCacheLine(ulong cache_line) {
            current_cache_line = cache_line;
            cache = ForceReadBytes(cache_line * CACHE_LINE_SIZE, CACHE_LINE_SIZE);
        }

        protected byte[] ForceReadBytes(ulong offset, ulong length) {
            byte[] result = new byte[length];
            uint bytes_read = 0;

            long filePtr;
            Win32.SetFilePointerEx(Handle, (long)offset, out filePtr, EMoveMethod.Begin);
            bool readSuccess = Win32.ReadFile(Handle, result, (uint)length - bytes_read, out bytes_read, IntPtr.Zero);
            if (!readSuccess) {
                //throw new Exception("File's screwed!");
            }
            return result;
        }

        public abstract ulong StreamLength {
            get;
        }

        public ulong DeviceOffset {
            get { return 0; }
        }

        public abstract string StreamName {
            get;
        }

        public IDataStream Parent {
            get { return null; }
        }

        public void Open() { }

        public void Close() { }

        #endregion
    }
}
