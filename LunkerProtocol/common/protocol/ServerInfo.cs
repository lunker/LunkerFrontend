using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LunkerLibrary.common.protocol
{
    public struct ServerInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        char[] ip;//15
        int port;

        public ServerInfo(char[] ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        /*
        public char[] Domain
        {
            get { return this.domain; }
            set { domain = value; }
        }
        */

            public char[] Ip
        {
            get { return ip; }
            set { ip = value; }
        }

        

        public int Port
        {
            get { return this.port; }
            set { port = value; }
        }

    }
}
