using Unity.Networking.Transport;
using UnityEngine;

public class NetWelcome : NetMessages
{

    public int AssignedTeam { set; get; }
    public NetWelcome()
    {
        Code = OpCode.WELCOME;
    }
    public NetWelcome(DataStreamReader reader)
    {
        Code = OpCode.WELCOME;
        DeSerialize(ref reader);

    }
    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(AssignedTeam);

    }
    public override void DeSerialize(ref DataStreamReader reader)
    {
        // we already read the byte in the NetUtility :: Data 
        AssignedTeam = reader.ReadInt();
    }
    public override void ReceivedOnClient()
    {
        NetUtility.C_WELCOME?.Invoke(this);
    }
    public override void ReceivedOnServer(NetworkConnection cnn)
    {
        NetUtility.S_WELCOME?.Invoke(this, cnn);
    }
}
