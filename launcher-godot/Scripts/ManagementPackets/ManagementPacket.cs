using System;
using System.IO;

namespace LauncherGodot.Scripts.ManagementPackets;

public abstract class ManagementPacket {
    public abstract int Type { get; }

    // format: lengthofdata(4) + type(4) + data
    public void Serialise(Stream stream) {
        DataWriter writer = new(stream);
        DataWriter sub = new();
        SerializeData(sub);
        writer
            .WriteInteger((int)sub.Length + 4)
            .WriteInteger(Type);
        sub.Write(writer);
    }
    
    public static ManagementPacket Deserialize(DataReader reader) {
        int type = reader.ReadInteger();
        
        ManagementPacket packet = type switch {
            0 => new HandshakePacket(),
            1 => new HandshakeResponsePacket(),
            2 => new AckPacket(),
            3 => new GrantAchievementPacket(),
            _ => throw new Exception($"Unknown packet type: {type}")
        };
        packet.DeserializeData(reader);
        return packet;
    }
    
    public abstract DataWriter SerializeData(DataWriter writer);
    public abstract ManagementPacket DeserializeData(DataReader data);
}
