using LunkerLibrary.common.protocol;

public struct CLDeleteRequestBody : Body
{
    UserInfo userInfo;

    public CLDeleteRequestBody(UserInfo userInfo)
    {
        this.userInfo = userInfo;
    }

    public UserInfo UserInfo
    {
        get { return userInfo; }
    }
}