public struct Packet
{
    public Header header;
    public Body body;

    public Packet(Header header, Body body)
    {
        this.header = header;
        this.body = body;
    }

    public Header Header
    {
        get { return this.header; }
        set { header = value; }
    }

    public Body Body
    {
        get { return this.body; }
        set { body = value; }
    }
    
}