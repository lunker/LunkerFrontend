using LunkerLibrary.common.protocol;
using System;
using System.Runtime.InteropServices;

public struct LBModifyRequestBody
{
    // --> MySQL
    UserInfo userInfo;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
    char[] npwd;

    public LBModifyRequestBody(UserInfo userInfo, char[] npwd)
    {
        this.userInfo = userInfo;
        this.npwd = new char[18];

        Array.Copy(npwd, this.npwd, npwd.Length);
    }

    public UserInfo UserInfo
    {
        get { return userInfo; }
        set { userInfo = value; }
    }

    public char[] Npwd
    {
        get { return npwd; }
        set { this.npwd = value; }
    }
}