public struct AAHeader
{
    MessageType type;
    MessageState state;

    public AAHeader(MessageType type, MessageState state)
    {
        this.type = type;
        this.state = state;
    }

    public MessageType Type
    {
        get { return type; }
    }

    public MessageState State
    {
        get { return state; }
    }
}