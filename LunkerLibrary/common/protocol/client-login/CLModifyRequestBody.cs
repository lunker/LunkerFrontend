using LunkerLibrary.common.protocol;
using System.Runtime.InteropServices;

public struct CLModifyRequestBody : Body
{
    UserInfo info;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
    char[] npwd;
}