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

    public ServerInfo ServerInfo
    {
        get
        {
            return serverInfo;
        }
        set
        {
            this.serverInfo = value;
        }
    }

    public LBSigninResponseBody(Cookie cookie, ServerInfo serverInfo)
    {
        this.cookie = cookie;
        this.serverInfo = serverInfo;
    }
}
