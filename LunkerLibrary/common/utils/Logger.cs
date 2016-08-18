﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Config;

namespace LunkerLibrary.common.Utils
{
    public static class Logger
    {
        private static ILog logger = null;

        public static ILog GetLoggerInstance()
        {
            if (logger == null)
            {
                log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo("D:\\workspace\\feature-async-without-beginxxxx\\LunkerFrontend\\LunkerChatWebServer\\config\\DebugLogconfig.xml"));
                logger = LogManager.GetLogger("Logger");
            }
                
            return logger;
        }
    }
}
