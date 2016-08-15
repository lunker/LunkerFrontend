using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LunkerLibrary.common.protocol
{
    public struct UserInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        char[] id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        char[] pwd;

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
