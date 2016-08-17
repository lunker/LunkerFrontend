using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LunkerAgent.src.utils
{
    class AppConfig
    {
        private int port = default(int);
        private string ip = default(string);


        private static AppConfig appConfig = null;



        private AppConfig()
        {
            // read config xml 
            //StringBuilder sb = new StringBuilder();
            //chatServerInfo.Select();

            XmlTextReader reader = new XmlTextReader("D:\\workspace\\LunkerFrontend\\LunkerChatServer\\config\\AppConfig.xml");
            while (reader.Read())
            {

                if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("ip"))
                {
                    reader.Read();
                    ip = reader.Value;
                    reader.Read(); // delete close element
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("port"))
                {
                    reader.Read();
                    if (Int32.TryParse(reader.Value, out port))
                    {
                        // successs
                    }
                    reader.Read(); // delete close element
                }

            }
        }// set configs 

        public static AppConfig GetInstance()
        {
            if (appConfig == null)
            {
                appConfig = new AppConfig();
            }
            return appConfig;
        }

        public string Ip
        {
            get { return ip; }
        }

        public int Port
        {
            get { return port; }
        }

    }
}
