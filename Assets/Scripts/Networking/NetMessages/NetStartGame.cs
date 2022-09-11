using Unity.Networking.Transport;
using UnityEngine;

public class NetStartGame : NetMessages
{

    
    public NetStartGame()
    {
        Code = OpCode.START_GAME;
    }
    public NetStartGame(DataStreamReader reader)
    {
        Code = OpCode.START_GAME;
        DeSerialize(ref reader);

    }
    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);        
    }
    public override void DeSerialize(ref DataStreamReader reader)
    {
        
    }
    public override void ReceivedOnClient()
    {
        NetUtility.C_START_GAME?.Invoke(this);
    }
    public override void ReceivedOnServer(NetworkConnection cnn)
    {
        NetUtility.S_START_GAME?.Invoke(this, cnn);
    }
}
