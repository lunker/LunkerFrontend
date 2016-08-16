using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LunkerLibrary.common.protocol
{
    public struct ServerInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        char[] ip;//15
        int port;
    }
}
