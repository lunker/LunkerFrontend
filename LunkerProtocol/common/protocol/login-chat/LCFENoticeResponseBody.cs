using LunkerLibrary.common.protocol;

public struct LCFENoticeResponseBody
{
    ServerInfo serverInfo;

    public LCFENoticeResponseBody(ServerInfo serverInfo)
    {
        this.serverInfo = serverInfo;
    }

    public ServerInfo ServerInfo
    {
        get { return serverInfo; }
        set { serverInfo = value; }
    }

}