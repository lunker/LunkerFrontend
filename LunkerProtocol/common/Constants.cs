﻿using System;
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

        public static int HeaderSize = 48;
        public static int AdminHeaderSize = 4;
        public static int None = 0;

        public static int NonExistedRoom { get; } = -1;
        public static string SocketServer { get; } = "socketserver.433.co.kr";

        public static string WebServer { get; } = "webserver.433.co.kr";

        public static string LoginServer { get; } = "loginserver.433.co.kr";

        public static string BeServer { get; } = "beserver.433.co.kr";

    }
}
