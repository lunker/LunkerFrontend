using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LunkerLibrary.common.Utils
{
    public class Constants
    {
        public static bool ThreadRun = true;
        public static bool ThreadStop = false;

        public static bool AppRun = true;
        public static bool AppStop = false;

        public static int HeaderSize = 52;
        public static int AdminHeaderSize = 8;
        public static int None = 0;

        public static int IdLength { get; } = 18;
        public static int PwdLength { get; } = 18;

        public static int NonExistedRoom { get; } = -1;
        public static string SocketServer { get; } = "socketserver.chat";

        public static string WebServer { get; } = "webserver.chat";

        public static string LoginServer { get; } = "loginserver.chat";

        public static string BeServer { get; } = "beserver.chat";

        public static bool ConsoleBlock { get; } = true;

        public static bool ConsoleNonBlock { get; } = false;

        public static int InitialState { get; } = (int) UIState.GetUserCommandInfo;

        public static int Lobby { get; } = 3;

        public static int Admin { get; } = 1;
        public static int Monitoring { get; } = 2;

        

    }
}
