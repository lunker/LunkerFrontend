using LunkerLibrary.common.protocol;

public struct LBSigninResponseBody
{
    Cookie cookie;
    ServerInfo serverInfo;

    public Cookie Cookie
    {
        get { return this.cookie; }
        set { this.cookie = value; }
    }
}
