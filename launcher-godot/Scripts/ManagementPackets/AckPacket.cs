namespace LauncherGodot.Scripts.ManagementPackets;

public class AckPacket : ManagementPacket {
    public override int Type => 2;
    
    public override DataWriter SerializeData(DataWriter writer) {
        return writer;
    }

    public override ManagementPacket DeserializeData(DataReader data) {
        return this;
    }
}
