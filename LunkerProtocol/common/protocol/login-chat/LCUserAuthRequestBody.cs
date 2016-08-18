
using LunkerLibrary.common.protocol;

/// <summary>
/// <para>User Login Auth Information</para>
/// </summary>
public struct LCUserAuthRequestBody
{
    Cookie cookie;
    UserInfo userInfo; // use only id 

    /*
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)] 
    char[] id;
    */

    public LCUserAuthRequestBody(Cookie cookie, UserInfo userInfo)
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

