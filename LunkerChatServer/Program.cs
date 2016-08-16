using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using LunkerLibrary.common.Utils;

namespace LunkerChatServer
{
    class Program
    {
        private static ILog logger = Logger.GetLoggerInstance();

        static void Main(string[] args)
        {
            logger.Debug("\n\n\n--------------------------------------------START PROGRAM--------------------------------------------");
            bool appState = Constants.AppRun;

            ChatServer chatServer = new ChatServer();
            chatServer.Start();

            while (appState)
            {
                Console.Write("어플리케이션을 종료하시겠습니까? (y/n) : ");
                string close = Console.ReadLine();
                if (close.Equals("y") || close.Equals("Y"))
                {
                    Console.Clear();
                    Console.Write("어플리케이션을 종료중입니다 . . .");
                    chatServer.Stop();
                    appState = Constants.AppStop;
            

                    logger.Debug("--------------------------------------------Exit Program-----------------------------------------------------");
                    Environment.Exit(0);
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine("다시 입력하십시오.");
                }
            }
            
        }// end method
    }// end class
}
