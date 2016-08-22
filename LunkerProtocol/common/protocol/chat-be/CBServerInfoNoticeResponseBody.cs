using LunkerLibrary.common.protocol;

public struct CBServerInfoNoticeResponseBody
{
    ServerInfo serverInfo;

    public CBServerInfoNoticeResponseBody(ServerInfo serverInfo)
    {
        this.serverInfo = serverInfo;
    }


    public ServerInfo ServerInfo
    {
        get { return serverInfo; }
    }

}