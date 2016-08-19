using LunkerLibrary.common.protocol;

public struct AgentInfo
{
    ServerInfo serverInfo;
    ServerState serverState;

    public AgentInfo(ServerInfo serverInfo, ServerState serverState)
    {
        this.serverInfo = serverInfo;
        this.serverState = serverState;
    }

    public ServerState ServerState
    {
        get { return serverState; }
        set { serverState = value; }
    }

    public ServerInfo ServerInfo
    {
        get { return serverInfo; }
        set { serverInfo = value; }
    }


}