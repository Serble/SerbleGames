namespace LauncherGodot.Scripts.ManagementPackets;

public class GrantAchievementPacket : ManagementPacket {
    public override int Type => 3;
    
    public string AchievementId { get; set; }
    
    public override DataWriter SerializeData(DataWriter writer) {
        return writer.WriteString(AchievementId);
    }

    public override ManagementPacket DeserializeData(DataReader data) {
        AchievementId = data.ReadString();
        return this;
    }
}
