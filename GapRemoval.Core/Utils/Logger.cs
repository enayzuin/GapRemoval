using System;
namespace GapRemoval.Core.Utils {
    public static class Logger {
        public static void Info(string message) {
            Console.WriteLine($"[INFO] {message}");
        }
    }
}
