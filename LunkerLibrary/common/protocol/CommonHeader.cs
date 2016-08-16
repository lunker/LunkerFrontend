using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunkerLibrary.common.protocol
{
    // 48byte 
    public struct CommonHeader : Header
    {
        MessageType type; // 2
        MessageState state; // 2
        int bodyLength; // 4 
        Cookie cookie; // 4 
        UserInfo userInfo; // 36

        public CommonHeader(MessageType type, MessageState state, int bodyLength, Cookie cookie, UserInfo userInfo)
        {
            this.type = type;
            this.state = state;
            this.bodyLength = bodyLength;
            this.cookie = cookie;
            this.userInfo = userInfo;

        }
        

        public MessageState State
        {
            get { return this.state; }
            set { state = value; }
        }

        public MessageType Type
        {
            get { return type; }
            set { type = value; }
        }

        public int BodyLength
        {
            get { return bodyLength; }
            set { bodyLength = value; }
        }
        
        public Cookie Cookie
        {
            get { return cookie; }
            set { cookie = value; }
        }
        
        public UserInfo UserInfo
        {
            get { return userInfo; }
            set { userInfo = value; }
        }
    }

}
