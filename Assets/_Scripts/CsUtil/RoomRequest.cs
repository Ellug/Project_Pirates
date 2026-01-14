public readonly struct RoomSnapshot
{
    public readonly string Name;
    public readonly int PlayerCount;
    public readonly int MaxPlayers;
    public readonly bool HasPassword;
    public readonly bool IsOpen;

    public RoomSnapshot(string name, int playerCount, int maxPlayers, bool hasPassword, bool isOpen)
    {
        Name = name;
        PlayerCount = playerCount;
        MaxPlayers = maxPlayers;
        HasPassword = hasPassword;
        IsOpen = isOpen;
    }

    public bool IsValid => !string.IsNullOrEmpty(Name);
}

public readonly struct CreateRoomRequest
{
    public readonly string Name;
    public readonly string Password;
    public readonly int MaxPlayers;

    public CreateRoomRequest(string name, string password, int maxPlayers)
    {
        Name = name;
        Password = password;
        MaxPlayers = maxPlayers;
    }
}
