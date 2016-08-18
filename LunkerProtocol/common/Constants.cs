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

        public static int HeaderSize = 48;
        public static int AdminHeaderSize = 4;
        public static int None = 0;

        private static string socketServer = "socketserver.433.co.kr";
        private static string webServer = "webserver.433.co.kr";
        private static string loginServer = "loginserver.433.co.kr";
        private static string beServer = "beserver.433.co.kr";

        public static string SocketServer
        {
            get { return socketServer; }
        }

        public static string WebServer
        {
            get { return webServer; }
        }

        public static string LoginServer
        {
            get { return loginServer; }
        }

        public static string BeServer
        {
            get { return beServer; }
        }
    }
}
