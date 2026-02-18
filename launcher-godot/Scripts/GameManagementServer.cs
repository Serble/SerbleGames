using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Godot;
using LauncherGodot.Scripts.ManagementPackets;

namespace LauncherGodot.Scripts;

public class GameManagementServer {
    public const int Port = 46599;  // magic number for the port the game management server listens on
    
    private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

    public async Task Start() {
        TcpListener listener = new(IPAddress.Loopback, Port);
        listener.Start();
        
        while (true) {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = HandleClient(client);
            GD.Print("Accepted game client connection");
        }
    }

    private async Task HandleClient(TcpClient client) {
        try {
            await using NetworkStream stream = client.GetStream();
            
            bool isAuthenticated = false;
            HandshakePacket handshake;
            while (client.Connected) {
                byte[] packetLengthBytes = _pool.Rent(4);
                await ReadBytes(stream, packetLengthBytes, 4);
                int packetLength = new DataReader(packetLengthBytes).ReadInteger();
                _pool.Return(packetLengthBytes);
                GD.Print("Received packet of length " + packetLength);
            
                byte[] packetBytes = _pool.Rent(packetLength);
                await ReadBytes(stream, packetBytes, packetLength);
                DataReader reader = new(packetBytes);
                
                ManagementPacket packet = ManagementPacket.Deserialize(reader);
                if (!isAuthenticated && packet is not HandshakePacket) {
                    _pool.Return(packetBytes);
                    throw new Exception("Expected handshake packet");
                }

                switch (packet) {
                    case HandshakePacket handshakePacket: {
                        isAuthenticated = true;
                        handshake = handshakePacket;

                        HandshakeResponsePacket response = new() {
                            IsLoggedIn = AuthManager.LoggedIn,
                            Username = AuthManager.LoggedIn ? (await AuthManager.GetAccountInfo()).Username : null
                        };
                        response.Serialise(stream);
                        GD.Print("Handshake from game client. Game ID: " + handshake.GameId);
                        break;
                    }

                    case GrantAchievementPacket grantAchievement: {
                        await Global.GrantAchievement(grantAchievement.AchievementId);
                        new AckPacket().Serialise(stream);
                        break;
                    }
                }
                
                _pool.Return(packetBytes);
            }
        }
        catch (Exception e) {
            GD.Print("Game client connection error: " + e.Message);
        }
        GD.Print("Game client disconnected");
    }
    
    private static async Task ReadBytes(NetworkStream stream, byte[] buffer, int count) {
        int offset = 0;
        while (offset < count) {
            int read = await stream.ReadAsync(buffer, offset, count - offset);
            if (read == 0) {
                throw new System.IO.EndOfStreamException("Client disconnected");
            }
            offset += read;
        }
    }
}
