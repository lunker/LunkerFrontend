public struct Packet
{
    public Header header;
    public Body body;

    public Packet(Header header, Body body)
    {
        this.header = header;
        this.body = body;
    }
    
}