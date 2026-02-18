namespace LauncherGodot.Scripts.ManagementPackets;

public class HandshakeResponsePacket : ManagementPacket {
    public override int Type => 1;
    
    public bool IsLoggedIn { get; set; }
    public string Username { get; set; }
    
    public override DataWriter SerializeData(DataWriter writer) {
        return writer
            .WriteBoolean(IsLoggedIn)
            .WritePrefixedOptional(Username, (s, w) => w.WriteString(s));
    }

    public override ManagementPacket DeserializeData(DataReader data) {
        IsLoggedIn = data.ReadBoolean();
        Username = data.ReadPrefixedOptional(r => r.ReadString());
        return this;
    }
}
