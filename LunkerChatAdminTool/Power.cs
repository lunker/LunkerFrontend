using log4net;
using LunkerLibrary.common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunkerChatAdminTool.src
{
    /// <summary>
    /// application start/shutdown 
    /// </summary>
    public static class Power
    {
        private static ILog logger = Logger.GetLoggerInstance();
        private static bool appState = Constants.AppRun;
        private static AdminTool adminTool = null;
        
        public static async void On()
        {
            logger.Debug("\n\n\n--------------------------------------------START PROGRAM--------------------------------------------");

            adminTool = AdminTool.GetInstance();
            adminTool.Start();
        }

        /// <summary>
        /// <para>Shutdown Chat server</para>
        /// <para></para>
        /// <para></para>
        /// </summary>
        public static void Off()
        {
            adminTool.Stop();
            appState = Constants.AppStop;
            // 종료 알림.

            logger.Debug("--------------------------------------------Exit Program-----------------------------------------------------");
            Environment.Exit(0);
        }
    }
}
