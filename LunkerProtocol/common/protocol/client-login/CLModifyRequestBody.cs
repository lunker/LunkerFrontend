using LunkerLibrary.common.protocol;
using System;
using System.Runtime.InteropServices;

public struct CLModifyRequestBody : Body
{
    UserInfo userInfo;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
    char[] npwd;

    public CLModifyRequestBody(UserInfo userInfo, char[] npwd)
    {
        this.userInfo = userInfo;
        this.npwd = new char[18];
        Array.Copy(npwd, this.npwd, npwd.Length);
    }

    public CLModifyRequestBody(UserInfo userInfo, string npwd)
    {
        this.userInfo = userInfo;
        this.npwd = new char[18];
        Array.Copy(npwd.ToCharArray(), this.npwd, npwd.ToCharArray().Length);
    }

}