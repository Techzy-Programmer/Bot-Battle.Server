using System;
using System.IO;
using System.Text;
using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Chatbot.Server
{
    public static class Utils
    {
        public static string AuthKEY { get; private set; } = null;

        public static bool UpdateAuth(string NewKEY = "N/A")
        {
            try
            {
                Directory.CreateDirectory("Logs");
                Console.WriteLine("Starting Up The Server......");
                Newtonsoft.Json.Linq.JObject.FromObject(new { D = "" }).ToString();
                var RegKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Bot-Battle");
                if (AuthKEY == null) AuthKEY = RegKey.GetValue("AuthKEY", "B9229").ToString();
                if (string.IsNullOrWhiteSpace(NewKEY) || NewKEY.Length < 4) return false;
                else
                {
                    RegKey.SetValue("AuthKEY", NewKEY, RegistryValueKind.String);
                    AuthKEY = NewKEY;
                    return true;
                }
            }
            catch { return false; }
        }

        public static string MD5Y(string RawInput)
        {
            using (MD5 Md5Hasher = MD5.Create())
            {
                byte[] InputBytes = Encoding.ASCII.GetBytes(RawInput);
                byte[] HashBytes = Md5Hasher.ComputeHash(InputBytes);
                StringBuilder SBuilder = new StringBuilder();
                for (int I = 0; I < HashBytes.Length; I++)
                    SBuilder.Append(HashBytes[I].ToString("X2"));
                return SBuilder.ToString();
            }
        }

        public static void ErrorToDisk(this Exception ToLog)
        {
            var IBef = new StackTrace().GetFrame(1);
            bool IsOK = ToLog.InnerException != null;

            File.AppendAllLines("Logs\\Error-LOGS.txt", new string[]
            {
                "============================[EXCEPTION]============================",
                $"Caller => {IBef.GetMethod().DeclaringType}.{IBef.GetMethod().Name} [At Line => {IBef.GetFileLineNumber()}]",
                $"Inner (If/Any) => {(IsOK ? ToLog.InnerException.Message : "N/A")}",
                $"EType => {ToLog.GetType().Name}",
                $"Message => {ToLog.Message}",
                $"Trace => {ToLog.StackTrace}",
                "....................................................................",
                "...................................................................."
            });
        }

        public static void LogInfo(this ClashManager Clasher, string Logs)
            => File.AppendAllText("Logs\\Info-LOGS.txt", "\r\n========================" +
                "================\r\n"+ Logs + "\r\n------------------------------------\r\n");
    }
}
