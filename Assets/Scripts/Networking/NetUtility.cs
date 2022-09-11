using System;
using Unity.Networking.Transport;
using UnityEngine;
public enum OpCode
{
    KEEP_ALIVE = 1,
    WELCOME = 2,
    START_GAME = 3,
    MAKE_MOVE = 4,
    REMATCH = 5
}
public static class NetUtility 
{   
    // Net messages
    public static Action<NetMessages> C_KEEP_ALIVE; // Receive massages on client
    public static Action<NetMessages> C_WELCOME;    // Receive massages on client
    public static Action<NetMessages> C_START_GAME; // Receive massages on client
    public static Action<NetMessages> C_MAKE_MOVE;  // Receive massages on client
    public static Action<NetMessages> C_REMATCH;    // Receive massages on client
    public static Action<NetMessages,NetworkConnection> S_KEEP_ALIVE;  // Receive massages on Server
    public static Action<NetMessages, NetworkConnection> S_WELCOME;    // Receive massages on Server
    public static Action<NetMessages, NetworkConnection> S_START_GAME; // Receive massages on Server
    public static Action<NetMessages, NetworkConnection> S_MAKE_MOVE;  // Receive massages on Server
    public static Action<NetMessages, NetworkConnection> S_REMATCH;   // Receive massages on Server
    //Methods
    public static void OnData(DataStreamReader stream ,NetworkConnection cnn, Server server = null)
    {
        NetMessages msg = null;
        var opCode = (OpCode)stream.ReadByte(); 
        switch (opCode) 
        {
            
            case OpCode.KEEP_ALIVE: msg = new NetKeepAlive(stream);break;   
            case OpCode.WELCOME: msg = new NetWelcome(stream); break;                
            case OpCode.START_GAME: msg = new NetStartGame(stream); break;           
            case OpCode.MAKE_MOVE : msg = new NetMakeMove(stream); break;
            case OpCode.REMATCH: msg = new NetRematch(stream); break; 
          // add next into server and client to write and Read data Stream in  socket 
            default: Debug.LogError("Message reseived has no Opcode "); break;            
        }
        if(server != null){ msg.ReceivedOnServer(cnn); }
        else { msg.ReceivedOnClient(); }
    }

}

