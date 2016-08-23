﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using log4net;
using LunkerLibrary.common.Utils;

namespace LunkerChatServer.src.utils
{
    public class AppConfig
    {
        //private int port = 0;
        private int backlog = 0;
        private int clientListenPort = 0;

        private string backendServerIp = "";
        private int backendServerPort = 0;

        private string loginServerIp = "";
        private int loginServerPort = 0;

        private static ILog logger = Logger.GetLoggerInstance();
        private static AppConfig appConfig = null;

        private AppConfig()
        {
            // read config xml 
#if DEBUG
            XmlTextReader reader = new XmlTextReader("config\\AppConfig.xml");
#else
            XmlTextReader reader = new XmlTextReader("config\\AppConfig.xml");
#endif

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("clientListenPort"))
                {
                    reader.Read();
                    if(Int32.TryParse( reader.Value, out clientListenPort))
                    {
                        logger.Debug("");
                    }
                    else
                    {
                        logger.Debug("");
                    }
                    reader.Read(); // delete close element
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("backlog"))
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
                    backendServerIp = reader.Value;
                    
                    reader.Read(); // delete close element
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("backendServerPort"))
                {
                    reader.Read();
                    if (Int32.TryParse(reader.Value, out backendServerPort))
                    {
                        logger.Debug("");
                    }
                    else
                    {
                        logger.Debug("");
                    }
                    reader.Read(); // delete close element
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("loginServerIp"))
                {
                    reader.Read();
                    loginServerIp = reader.Value;

                    reader.Read(); // delete close element
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("loginServerPort"))
                {
                    reader.Read();
                    if (Int32.TryParse(reader.Value, out loginServerPort))
                    {
                        logger.Debug("");
                    }
                    else
                    {
                        logger.Debug("");
                    }
                    reader.Read(); // delete close element
                }
            }
        }// set configs 

        public static AppConfig GetInstance()
        {
            if(appConfig == null)
            {
                appConfig = new AppConfig();
            }
            return appConfig;
        }


        public int ClientListenPort
        {
            get { return this.clientListenPort; }
        }
        
        public int Backlog
        {
            get { return this.backlog; }
        }

        public string BackendServerIp
        {
            get { return backendServerIp; }
        }

        public int BackendServerPort
        {
            get { return backendServerPort; }
        }

        public string LoginServerIp
        {
            get { return loginServerIp; }

        }
        public int LoginServerPort
        {
            get { return loginServerPort; }
        }
    }
}
