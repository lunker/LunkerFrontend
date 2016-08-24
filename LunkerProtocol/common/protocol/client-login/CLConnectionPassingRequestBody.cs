using LunkerLibrary.common.protocol;

public struct CLConnectionPassingRequestBody
{
    ServerInfo serverInfo;
    public CLConnectionPassingRequestBody(ServerInfo serverInfo)
    {
        this.serverInfo = serverInfo;
    }


    public ServerInfo ServerInfo
    {
        get { return serverInfo; }
        set { this.serverInfo = value; }
    }

}