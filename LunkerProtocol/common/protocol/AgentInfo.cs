using LunkerLibrary.common.protocol;

public struct AgentInfo
{
    ServerInfo serverInfo;
    ServerState serverState;
    ServerType serverType;

    public AgentInfo(ServerInfo serverInfo, ServerState serverState, ServerType serverType)
    {
        this.serverInfo = serverInfo;
        this.serverState = serverState;
        this.serverType = serverType;
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

    public ServerType ServerType
    {
        get { return serverType; }
        set {
            serverType = value;
        }
    }

}