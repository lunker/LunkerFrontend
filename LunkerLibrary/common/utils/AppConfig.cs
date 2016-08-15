﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using log4net;



namespace LunkerLibrary.common.Utils
{
    public class AppConfig
    {
        //private int port = 0;
        private int frontport = 0;
        private int backport = 0;
        private int backlog = 0;

        private static ILog logger = Logger.GetLoggerInstance();
        private static AppConfig appConfig = null;

        private AppConfig()
        {
            // read config xml 
            //StringBuilder sb = new StringBuilder();

            XmlTextReader reader = new XmlTextReader("D:\\workspace\\LunkerFrontend\\LunkerChatServer\\config\\AppConfig.xml");
            while (reader.Read())
            {

                if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("frontport"))
                {
                    reader.Read();
                    if(Int32.TryParse( reader.Value, out frontport))
                    {
                        logger.Debug("");
                    }
                    else
                    {
                        logger.Debug("");
                    }
                    reader.Read(); // delete close element
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("backport"))
                {
                    reader.Read();
                    if (Int32.TryParse(reader.Value, out backport))
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
    }
}
