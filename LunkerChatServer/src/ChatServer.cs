using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using log4net;

using System.Net;

using System.Net.WebSockets;
using System.Threading;
using LunkerLibrary.common.Utils;

namespace LunkerChatServer
{
    /**
     * Chatting Server 
     */
    public class ChatServer
    {
        private ILog logger = Logger.GetLoggerInstance();
        private static ChatServer instance = null;

        private MainWorker mainWorker = MainWorker.GetInstance();
        
        public static ChatServer GetInstance()
        {
            if(instance == null)
            {
                instance = new ChatServer();
            }
            return instance;
        }

        private ChatServer() { }

        public void Start()
        {
            Thread frontThread = new Thread(new ThreadStart(mainWorker.Start));
            frontThread.Start();
        }

        public void Stop()
        {
            mainWorker.Stop();

            mainWorker = null;
     
            //appState = Constants.AppStop;
        }
    }// end class 
}
