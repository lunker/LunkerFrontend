using LunkerLibrary.common.protocol;

public struct CLSigninRequestBody
{
    UserInfo userInfo;

    public UserInfo UserInfo
    {
        get { return userInfo; }
        set { userInfo = value; }
    }
}