using LunkerLibrary.common.protocol;

public struct CCJoinResponseBody : Body
{
    // If Success
    // none

    // If Fail
    ServerInfo serverInfo;

    public CCJoinResponseBody(ServerInfo serverInfo)
    {
        this.serverInfo = serverInfo;
    }

    public ServerInfo ServerInfo
    {
        get { return serverInfo; }
        set { serverInfo = value; }
    }

}
