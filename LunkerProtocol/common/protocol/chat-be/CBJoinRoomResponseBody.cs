using LunkerLibrary.common.protocol;

public struct CBJoinRoomResponseBody
{
    ServerInfo serverInfo;

    public CBJoinRoomResponseBody(ServerInfo serverInfo)
    {
        this.serverInfo = serverInfo;
    }

    public ServerInfo ServerInfo
    {
        get { return serverInfo; }
        set { serverInfo = value; }
    }

}