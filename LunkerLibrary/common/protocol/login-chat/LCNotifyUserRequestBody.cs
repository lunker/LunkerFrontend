using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunkerLibrary.common.protocol.login_chat
{
    public struct LCNotifyUserRequestBody
    {
        Cookie cookie;
        UserInfo userInfo; // use only id 
        /*
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)] 
        char[] id;
        */

        public Cookie Cookie
        {
            get { return cookie; }
            set { this.cookie = value; }
        }

        public UserInfo UserInfo
        {
            get { return userInfo; }
            set { userInfo = value; }
        }
    }
}
