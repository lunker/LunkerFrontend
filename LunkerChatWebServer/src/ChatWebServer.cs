using log4net;
using LunkerLibrary.common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LunkerChatWebServer.src
{

    public class ChatWebServer
    {
        private ILog logger = Logger.GetLoggerInstance();
        private static ChatWebServer instance = null;

        private MainWorker mainWorker = MainWorker.GetInstance();

        public static ChatWebServer GetInstance()
        {
            if (instance == null)
            {
                instance = new ChatWebServer();
            }
            return instance;
        }

        private ChatWebServer() { }

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
    }
  
}
