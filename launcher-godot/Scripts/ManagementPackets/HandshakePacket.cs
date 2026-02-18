namespace LauncherGodot.Scripts.ManagementPackets;

public class HandshakePacket : ManagementPacket {
    public override int Type => 0;
    
    public string GameId { get; set; }
    
    public override DataWriter SerializeData(DataWriter writer) {
        return writer.WriteString(GameId);
    }

    public override ManagementPacket DeserializeData(DataReader data) {
        GameId = data.ReadString();
        return this;
    }
}
