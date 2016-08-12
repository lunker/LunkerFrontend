using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using LunkerRedis.src.Utils;

namespace LunkerChatServer
{
    class Program
    {
        private static ILog logger = Logger.GetLoggerInstance();
        static void Main(string[] args)
        {
            logger.Debug("\n\n\n--------------------------------------------START PROGRAM--------------------------------------------");
            ChatServer chatServer = new ChatServer();
            chatServer.Start();

            
        }
    }
}
