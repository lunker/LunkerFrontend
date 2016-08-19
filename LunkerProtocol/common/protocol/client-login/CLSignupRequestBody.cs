using LunkerLibrary.common.protocol;

public struct CLSignupRequestBody : Body
{
    UserInfo userInfo;

    public CLSignupRequestBody(UserInfo userInfo)
    {
        this.userInfo = userInfo;
    }

    public UserInfo UserInfo
    {
        get { return userInfo; }
    }

}