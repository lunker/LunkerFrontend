using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunkerLibrary.common.protocol
{
    public struct CCHeader : Header
    {
        MessageType type;
        MessageState state;
        int bodyLength;
        Cookie cookie;

        public CCHeader(MessageType type, MessageState state, int bodyLength, Cookie cookie)
        {
            this.type = type;
            this.state = state;
            this.bodyLength = bodyLength;
            this.cookie = cookie;

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

    }
}
