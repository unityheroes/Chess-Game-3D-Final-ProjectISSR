using Unity.Networking.Transport;

public class NetMessages
{
  public OpCode Code { set; get; }

    public virtual void Serialize(ref DataStreamWriter writer) // to write bits data
    {
        writer.WriteByte((byte)Code);
    }
    public virtual void DeSerialize(ref DataStreamReader reader) // to read bits data 
    {

    }
    public virtual void ReceivedOnClient() // Only address server 
    {
        NetUtility.C_KEEP_ALIVE?.Invoke(this);
    }
    public virtual void ReceivedOnServer(NetworkConnection cnn) // to Receive Msg and Attach who send to us 
    {
        NetUtility.S_KEEP_ALIVE?.Invoke(this,cnn);

    }

}
