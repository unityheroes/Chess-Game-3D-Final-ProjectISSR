
using Unity.Networking.Transport;

public class NetRematch : NetMessages
{
    public int teamId;
    public byte WantRematch; // to check another player want Rematch or exit
    public NetRematch() // constractor <-- Making the box 
    {
        Code = OpCode.REMATCH;
    }
    public NetRematch(DataStreamReader reader) // constractor <-- Receiving the box 
    {
        Code = OpCode.REMATCH;
        DeSerialize(ref reader);
    }
    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(teamId);
        writer.WriteByte(WantRematch);

    }
    public override void DeSerialize(ref DataStreamReader reader)
    {
        teamId = reader.ReadInt();
        WantRematch = reader.ReadByte();
    }
    public override void ReceivedOnClient()
    {
        NetUtility.C_REMATCH?.Invoke(this);
    }
    public override void ReceivedOnServer(NetworkConnection cnn)
    {
        NetUtility.S_REMATCH?.Invoke(this, cnn);
    }
}
