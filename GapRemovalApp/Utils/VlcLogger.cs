using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace GapRemovalApp.Utils
{
    public static class VlcLogger
    {
        public static void AttachLogHandler(LibVLC libVLC)
        {
            libVLC.Log += (sender, args) =>
            {
                var message = $"[VLC][{args.Level}] {args.Module}: {args.Message}";
                switch (args.Level)
                {
                    case LibVLCSharp.Shared.LogLevel.Debug:
                        Logger.Debug(message);
                        break;
                    case LibVLCSharp.Shared.LogLevel.Notice:
                        Logger.Info(message);
                        break;
                    case LibVLCSharp.Shared.LogLevel.Warning:
                        Logger.Warn(message);
                        break;
                    case LibVLCSharp.Shared.LogLevel.Error:
                        Logger.Error(message);
                        break;
                    default:
                        Logger.Info(message);
                        break;
                }
            };
        }
    }
}
