
public struct AAAgentInfoRequestBody
{
    AgentInfo agentInfo;

    public AAAgentInfoRequestBody(AgentInfo agentInfo)
    {
        this.agentInfo = agentInfo;
    }

    public AgentInfo AgentInfo
    {
        get { return agentInfo; }
        set { agentInfo = value; }
    }
}

