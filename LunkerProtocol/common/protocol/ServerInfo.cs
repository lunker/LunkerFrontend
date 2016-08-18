using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LunkerLibrary.common.protocol
{
    public struct ServerInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        char[] domain;//15
        int port;

        public ServerInfo(char[] domain, int port)
        {
            this.domain = domain;
            this.port = port;
        }

        public char[] Domain
        {
            get { return this.domain; }
            set { domain = value; }
        }

        public int Port
        {
            get { return this.port; }
            set { port = value; }
        }

    }
}
