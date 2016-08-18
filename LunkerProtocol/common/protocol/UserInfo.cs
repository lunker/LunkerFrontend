using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LunkerLibrary.common.protocol
{
    public struct UserInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        char[] id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        char[] pwd;

        public UserInfo(char[] id, char[] pwd)
        {
            this.id = id;
            this.pwd = pwd;
        }

        public UserInfo(string id)
        {
            this.id = id.ToCharArray();
            this.pwd = new char[18];
        }

        public char[] Id
        {
            get { return id; }
            set { id = value; }

        }

        public char[] Pwd
        {
            get { return pwd; }
            set { pwd = value; }
        }
    }
}
