using log4net;
using LunkerChatServer.src.agent;
using LunkerChatServer.src.utils;
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
        private static ILog logger = ChatLogger.GetLoggerInstance();
        private static bool appState = Constants.AppRun;
        private static ChatServer chatServer = null;
        private static MessageBroker mBroker = MessageBroker.GetInstance();

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
                  
                    Off(MessageType.ShutdownApp);
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
        public static void Off(MessageType type)
        {
            chatServer.Stop();
            appState = Constants.AppStop;

            //====================================나눌 필요가 있나 ? 
            if (type == MessageType.ShutdownApp) {
                // 종료 알림.
                logger.Debug("--------------------------------------------Shutdown!!!-----------------------------------------------------");
                MessageBroker.GetInstance().Publish(new AAHeader(MessageType.ShutdownApp, MessageState.Success, Constants.None));
                MessageBroker.GetInstance().Release();
            }
            else
            {
                MessageBroker.GetInstance().Publish(new AAHeader(MessageType.RestartApp, MessageState.Success, Constants.None));
                MessageBroker.GetInstance().Release();
            }

            logger.Debug("--------------------------------------------Exit Program-----------------------------------------------------");
            Environment.Exit(0);
        }
    }
}
