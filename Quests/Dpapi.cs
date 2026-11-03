using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DiscordGameSpoofer.Quests
{
    /// <summary>
    /// Small wrapper around CryptProtectData / CryptUnprotectData. Discord encrypts
    /// its Local State key with the current Windows user, so we use the same API
    /// instead of shipping a third-party DPAPI library.
    /// </summary>
    internal static class Dpapi
    {
        private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;

        [StructLayout(LayoutKind.Sequential)]
        private struct DataBlob
        {
            public int cbData;
            public IntPtr pbData;
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CryptProtectData(
            ref DataBlob pDataIn,
            string? szDataDescr,
            IntPtr pOptionalEntropy,
            IntPtr pvReserved,
            IntPtr pPromptStruct,
            int dwFlags,
            ref DataBlob pDataOut);

        [DllImport("crypt32.dll", SetLastError = true)]
        private static extern bool CryptUnprotectData(
            ref DataBlob pDataIn,
            IntPtr ppszDataDescr,
            IntPtr pOptionalEntropy,
            IntPtr pvReserved,
            IntPtr pPromptStruct,
            int dwFlags,
            ref DataBlob pDataOut);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        public static byte[] Protect(byte[] data)
        {
            DataBlob input = CreateBlob(data);
            DataBlob output = default;

            try
            {
                if (!CryptProtectData(ref input, "Discord Game Spoofer", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                        CRYPTPROTECT_UI_FORBIDDEN, ref output))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows could not encrypt the data.");
                }

                return CopyBlob(output);
            }
            finally
            {
                FreeBlob(input);
                FreeBlob(output);
            }
        }

        public static byte[] Unprotect(byte[] data)
        {
            DataBlob input = CreateBlob(data);
            DataBlob output = default;

            try
            {
                if (!CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                        CRYPTPROTECT_UI_FORBIDDEN, ref output))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows could not decrypt the data.");
                }

                return CopyBlob(output);
            }
            finally
            {
                FreeBlob(input);
                FreeBlob(output);
            }
        }

        private static DataBlob CreateBlob(byte[] data)
        {
            DataBlob blob = new DataBlob
            {
                cbData = data.Length,
                pbData = Marshal.AllocHGlobal(data.Length),
            };

            if (data.Length > 0)
                Marshal.Copy(data, 0, blob.pbData, data.Length);

            return blob;
        }

        private static byte[] CopyBlob(DataBlob blob)
        {
            if (blob.cbData <= 0 || blob.pbData == IntPtr.Zero)
                return Array.Empty<byte>();

            byte[] result = new byte[blob.cbData];
            Marshal.Copy(blob.pbData, result, 0, blob.cbData);
            return result;
        }

        private static void FreeBlob(DataBlob blob)
        {
            if (blob.pbData != IntPtr.Zero)
                LocalFree(blob.pbData);
        }
    }
}
