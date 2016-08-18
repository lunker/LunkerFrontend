public struct ChattingRoom
{ 
    int roomNo;

    public ChattingRoom(int roomNo)
    {
        this.roomNo = roomNo;
    }

    public int RoomNo
    {
        get { return roomNo; }
        set { roomNo = value; }
    }
}