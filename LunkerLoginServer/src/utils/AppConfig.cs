using log4net;
using LunkerLibrary.common.protocol;
using LunkerLibrary.common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LunkerLoginServer.src.utils
{
    public class AppConfig
    {
        //private int port = 0;
        private int frontport = 0;
        private int backport = 0;
        private int backlog = 0;

        private string backendserverip = "";
        private int backendserverport = 0;

        private static ILog logger = Logger.GetLoggerInstance();
        private static AppConfig appConfig = null;

        private Dictionary<string, ServerInfo> chatServerInfo = null;
        private int clientListenEndPoint = 0;
        private int frontendListenEndPoint = 0;

        private AppConfig()
        {
            // read config xml 
            //StringBuilder sb = new StringBuilder();
            //chatServerInfo.Select();

            XmlTextReader reader = new XmlTextReader("config\\AppConfig.xml");
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("backlog"))
                {
                    reader.Read();
                    if (Int32.TryParse(reader.Value, out backlog))
                    {
                        logger.Debug("");
                    }
                    else
                    {
                        logger.Debug("");
                    }
                    reader.Read(); // delete close element
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("backendServerIp"))
                {
                    reader.Read();
                    backendserverip = reader.Value;

                    reader.Read(); // delete close element
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("backendServerPort"))
                {
                    reader.Read();
                    if (Int32.TryParse(reader.Value, out backendserverport))
                    {
                        logger.Debug("");
                    }
                    else
                    {
                        logger.Debug("");
                    }
                    reader.Read(); // delete close element
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("clientListenEndPoint"))
                {
                    reader.Read();
                    if (Int32.TryParse(reader.Value, out clientListenEndPoint))
                    {
                        logger.Debug("");
                    }
                    else
                    {
                        logger.Debug("");
                    }
                    
                    reader.Read(); // delete close element
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("frontendListenEndPoint"))
                {
                    reader.Read();
                    if (Int32.TryParse(reader.Value, out frontendListenEndPoint))
                    {
                        logger.Debug("");
                    }
                    else
                    {
                        logger.Debug("");
                    }
                    
                    reader.Read(); // delete close element
                }



            }//end loop
        }// set configs 

        public static AppConfig GetInstance()
        {
            if (appConfig == null)
            {
                appConfig = new AppConfig();
            }
            return appConfig;
        }

        public int Backport
        {
            get { return this.Backport; }
        }

        public int FrontPort
        {
            get { return this.frontport; }
        }

        public int Backlog
        {
            get { return this.backlog; }
        }

        public string Backendserverip
        {
            get { return backendserverip; }
        }

        public int Backendserverport
        {
            get { return backendserverport; }
        }

        public int ClientListenEndPoint
        {
            get { return clientListenEndPoint; }
        }

        public int FrontListenEndPoint
        {
            get { return frontendListenEndPoint; }
        }
    }
}
