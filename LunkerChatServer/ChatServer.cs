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
        
        private FrontListener frontListener = FrontListener.GetInstance();
        private BackendListener backListener = BackendListener.GetInstance();
        
        public void Start()
        {
            Thread frontThread = new Thread(new ThreadStart(frontListener.Start));
            frontThread.Start();

            Thread backThread = new Thread(new ThreadStart(backListener.Start));
            backThread.Start();

        }

        public void Stop()
        {
            frontListener.Stop();
            backListener.Stop();

            frontListener = null;
            backListener = null;
            //appState = Constants.AppStop;
        }
    }// end class 
}
