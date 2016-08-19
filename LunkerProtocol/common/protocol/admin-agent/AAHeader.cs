public struct AAHeader
{
    MessageType type;
    MessageState state;
    int bodyLength;

    public AAHeader(MessageType type, MessageState state, int bodyLength)
    {
        this.type = type;
        this.state = state;
        this.bodyLength = bodyLength;
    }

    public MessageType Type
    {
        get { return type; }
    }

    public MessageState State
    {
        get { return state; }
    }

    public int BodyLength
    {
        get { return bodyLength; }
        set { bodyLength = value; }
    }
}