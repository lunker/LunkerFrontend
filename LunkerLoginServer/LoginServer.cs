using log4net;
using LunkerLibrary.common.Utils;
using LunkerLoginServer.src.workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LunkerLoginServer
{
    public class LoginServer
    {
        private ILog logger = Logger.GetLoggerInstance();

        private MainWorker mainWorker = MainWorker.GetInstance();

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
