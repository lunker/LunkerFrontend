public struct AAAgentInfoResponseBody
{
    AgentInfo agentInfo;

    public AAAgentInfoResponseBody(AgentInfo agentInfo)
    {
        this.agentInfo = agentInfo;
    }

    public AgentInfo AgentInfo
    {
        get { return AgentInfo; }
        set { AgentInfo = value; }
    }
}