﻿using System;
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
            this.ip = new char[15];
            Array.Copy(ip, this.ip, ip.Length);

            this.port = port;
        }

        public ServerInfo(string ip, int port)
        {
            this.ip = new char[15];
            Array.Copy(ip.ToCharArray(), this.ip, ip.ToCharArray().Length);

            this.port = port;

        }

        public ServerInfo(string ip)
        {
            this.ip = new char[15];

            Array.Copy(ip.ToCharArray(), this.ip, ip.ToCharArray().Length);
            this.port = 0;
        }

        public string GetPureIp()
        {
            if (this.ip != null)
                return new string(ip).Split('\0')[0];
            else
                return null;
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
