public struct AACommandRequestBody
{
    ServerType serverType;
    public AACommandRequestBody(ServerType serverType)
    {
        this.serverType = serverType;
    }

    public ServerType ServerType
    {
        get { return serverType; }
        set { serverType = value; }
    }
}