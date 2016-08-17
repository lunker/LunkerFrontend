using log4net;
using LunkerChatServer.src.agent;
using LunkerLibrary.common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunkerChatServer.src
{
    /// <summary>
    /// application start/shutdown 
    /// </summary>
    public static class Power
    {
        private static ILog logger = Logger.GetLoggerInstance();
        private static bool appState = Constants.AppRun;
        private static ChatServer chatServer = null;

        public static void On()
        {
            logger.Debug("\n\n\n--------------------------------------------START PROGRAM--------------------------------------------");


            chatServer = ChatServer.GetInstance();
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
        }

        /// <summary>
        /// <para>Shutdown Chat server</para>
        /// <para>publish success message to agent</para>
        /// <para></para>
        /// <para></para>
        /// </summary>
        public static void Off()
        {
            chatServer.Stop();
            appState = Constants.AppStop;
            // 종료 알림.
            MessageBroker.GetInstance().Publish(new AAHeader(MessageType.ShutdownApp, MessageState.Success));
            MessageBroker.GetInstance().Release();

            logger.Debug("--------------------------------------------Exit Program-----------------------------------------------------");
            Environment.Exit(0);
        }
    }
}
