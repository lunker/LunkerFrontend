
using LunkerLibrary.common.protocol;

public struct LCNotifyUserRequestBody
{
    Cookie cookie;
    UserInfo userInfo; // use only id 

    /*
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)] 
    char[] id;
    */

    public LCNotifyUserRequestBody(Cookie cookie, UserInfo userInfo)
    {
        this.cookie = cookie;
        this.userInfo = userInfo;
    }

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

