using LunkerLibrary.common.protocol;

public struct CLSigninResponseBody
{
    Cookie cookie;
    ServerInfo serverInfo;

    public CLSigninResponseBody(Cookie cookie, ServerInfo serverInfo)
    {
        this.cookie = cookie;
        this.serverInfo = serverInfo;
    }

    public Cookie Cookie
    {
        get { return cookie; }
        set { cookie = value; }
    }

    public ServerInfo ServerInfo
    {
        get { return serverInfo; }
        set { serverInfo = value; }
    }

}