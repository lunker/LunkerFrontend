using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Config;

namespace LunkerLoginServer.src.utils
{
    public static class LoginLogger
    {
        private static ILog logger = null;

        public static ILog GetLoggerInstance()
        {
            if (logger == null)
            {
                log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo("D:\\workspace\\feature-async-without-beginxxxx\\LunkerFrontend\\LunkerLoginServer\\config\\DebugLogconfig.xml"));
                logger = LogManager.GetLogger("Logger");
            }
                
            return logger;
        }
    }
}
