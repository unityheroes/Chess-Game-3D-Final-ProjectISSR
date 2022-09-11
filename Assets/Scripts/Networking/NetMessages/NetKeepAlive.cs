
using Unity.Networking.Transport;

public class NetKeepAlive : NetMessages
{
   public NetKeepAlive() // constractor <-- Making the box 
    {
        Code = OpCode.KEEP_ALIVE;
    }
    public NetKeepAlive(DataStreamReader reader) // constractor <-- Receiving the box 
    {
        Code = OpCode.KEEP_ALIVE;
        DeSerialize(ref reader);
    }
    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);

    }
    public override void DeSerialize(ref DataStreamReader reader)
    {
       // defualt reader in class NetUtility
    }
    public override void ReceivedOnClient()
    {
        NetUtility.C_KEEP_ALIVE?.Invoke(this);
    }
    public override void ReceivedOnServer(NetworkConnection cnn)
    {
        NetUtility.S_KEEP_ALIVE?.Invoke(this,cnn);
    }
}
