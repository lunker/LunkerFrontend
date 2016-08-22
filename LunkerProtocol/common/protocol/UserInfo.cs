using LunkerLibrary.common.Utils;
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

        bool isDummy;

        
        public UserInfo(char[] id, char[] pwd, bool isDummy)
        {
            this.id = new char[Constants.IdLength];
            Array.Copy(id, this.id, id.Length);
            this.pwd = new char[Constants.PwdLength];
            Array.Copy(pwd, this.pwd, pwd.Length);
            this.isDummy = isDummy;
        }

        public UserInfo(string id, string pwd, bool isDummy)
        {
            this.id = new char[18];
            Array.Copy(id.ToCharArray(),this.id, id.ToCharArray().Length);
            this.pwd = new char[18];
            Array.Copy(pwd.ToCharArray(), this.pwd, pwd.ToCharArray().Length);

            this.isDummy = isDummy;
        }

        
        public UserInfo(string id)
        {
            this.id = id.ToCharArray();
            this.pwd = new char[18];
            this.isDummy = false;
        }
        public string GetPureId()
        {
            if (this.id != null)
                return new string(id).Split('\0')[0];
            else
                return null;
        }

        public string GetPurePwd()
        {
            if (this.id != null)
                return new string(pwd).Split('\0')[0];
            else
                return null;
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

        public bool IsDummy
        {
            get { return isDummy; }
            set { isDummy = value; }
        }

    }
}
