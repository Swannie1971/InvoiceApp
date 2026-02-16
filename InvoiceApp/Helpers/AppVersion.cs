using System;

namespace InvoiceApp.Helpers
{
    public static class AppVersion
    {
        public const string Version = "1.0.0";
        public const string ReleaseDate = "2026-02-13";
        public const string AppName = "Invoice App";

        public static string FullVersion => $"{AppName} v{Version}";
        public static string AboutText => $"{AppName}\nVersion {Version}\nReleased: {ReleaseDate}";
    }
}