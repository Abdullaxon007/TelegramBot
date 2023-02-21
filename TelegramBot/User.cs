class User
{
    public long ChatId;
    public string Name;
    public ENextMessage NextMessage = ENextMessage.Created;
    public Outlay? CurrentAddingOutlay;

    public Room CurrentRoom;
}