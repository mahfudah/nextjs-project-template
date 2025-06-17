using System;
using System.Runtime.InteropServices;

namespace RawPrinterClient
{
    public class PrinterManager
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, Int32 level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, Int32 dwCount, out Int32 dwWritten);

        public string SelectedPrinter { get; set; }

        public bool PrintRaw(string rawData)
        {
            if (string.IsNullOrEmpty(SelectedPrinter))
                throw new InvalidOperationException("No printer selected");

            IntPtr hPrinter = IntPtr.Zero;
            
            try
            {
                if (!OpenPrinter(SelectedPrinter, out hPrinter, IntPtr.Zero))
                {
                    throw new Exception($"Cannot open printer: {SelectedPrinter}");
                }

                var di = new DOCINFOA
                {
                    pDocName = "Raw Print Job",
                    pDataType = "RAW"
                };

                if (!StartDocPrinter(hPrinter, 1, di))
                {
                    throw new Exception("Cannot start document");
                }

                if (!StartPagePrinter(hPrinter))
                {
                    EndDocPrinter(hPrinter);
                    throw new Exception("Cannot start page");
                }

                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(rawData);
                int dwWritten;

                // Allocate unmanaged memory and copy the managed byte array into it
                IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);

                try
                {
                    if (!WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out dwWritten))
                    {
                        throw new Exception("Failed to write to printer");
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pUnmanagedBytes);
                }

                if (!EndPagePrinter(hPrinter))
                {
                    throw new Exception("Cannot end page");
                }

                if (!EndDocPrinter(hPrinter))
                {
                    throw new Exception("Cannot end document");
                }

                return true;
            }
            catch
            {
                throw;
            }
            finally
            {
                if (hPrinter != IntPtr.Zero)
                {
                    ClosePrinter(hPrinter);
                }
            }
        }
    }
}
