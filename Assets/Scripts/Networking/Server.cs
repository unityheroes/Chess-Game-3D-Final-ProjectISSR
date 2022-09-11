using System;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class Server : MonoBehaviour
{
    // to make treads for request 
    #region Singleton implementation 
    public static Server Instance { set; get; }

    private void Awake()
    {
        Instance = this;
    }

    #endregion

    //Rules
    public NetworkDriver driver;
    private NativeList<NetworkConnection> connections;

    private bool isActive = false;
    private const float keepAliveTickRate = 20.0f; // to keep send message every 20 second
    private float lastKeepAlive;

    public Action connectionDropped;


    //Methods
    public void Init(ushort port) // initalize port
    {
        driver = NetworkDriver.Create();
        NetworkEndPoint endPoint = NetworkEndPoint.AnyIpv4;
        endPoint.Port = port;

        if (driver.Bind(endPoint) != 0)  // 0 is success !=0 not success
        {
            Debug.Log("Unable to Bind on port " + endPoint.Port); // test
            return;
        }
        else
        {
            driver.Listen();
            Debug.Log("Currently listening on port" + endPoint.Port); // test
        }
        connections = new NativeList<NetworkConnection>(2, Allocator.Persistent); // max player 2
        isActive = true;
    }
    public void Shutdown() // close off the server
    {
        if (isActive)
        {
            driver.Dispose();
            connections.Dispose();
            isActive = false;
        }
    }
    public void OnDestroy()
    {
        Shutdown();

    }
    private void Update()
    {
        if (!isActive)
        {
            return;
        }
        KeepAlive(); // send msg every 20 sec to sure connection between server and client

        driver.ScheduleUpdate().Complete(); // to make job system queue of msgs
        CleanupConnections(); // any body is not connected to us but we still have reference 
        AcceptNewConnections(); // is there somebody knocking on the door to enter our server 
        UpdateMessagePump();  // is are they sending us message and if so  do we have to reply
    }
   private void KeepAlive()
    {
        if(Time.time - lastKeepAlive > keepAliveTickRate) // every 20 sec 
        {
            lastKeepAlive = Time.time;
            Broadcast(new NetKeepAlive());
        }
    }
    private void CleanupConnections()
    {
        for (int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                --i; // for don't break loop
            }
        }
    }
    private void AcceptNewConnections()
    {
        //Accept new Connections
        NetworkConnection c;
        while ((c = driver.Accept()) != default(NetworkConnection))
        {
            connections.Add(c); // add connection to list
        }

    }

    private void UpdateMessagePump()
    {
        DataStreamReader stream;
        for (int i = 0; i < connections.Length; i++)
        {
            NetworkEvent.Type cmd;
            while((cmd = driver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                
                if (cmd==NetworkEvent.Type.Data)
                {
                    NetUtility.OnData(stream, connections[i], this);
                }
                else if(cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client Disconnect from server ");
                    connections[i] = default(NetworkConnection);
                    connectionDropped?.Invoke();
                    Shutdown(); // this does not happen usually , its just because we are in a two player  game 
                }
            }
        }
    }

    // Server specific 
    public void SendToClient(NetworkConnection connection , NetMessages msg)
    {
        DataStreamWriter writer;
        driver.BeginSend(connection, out writer);
        msg.Serialize(ref writer);
        driver.EndSend(writer);
    }

    public void Broadcast(NetMessages msg)
    {
        for (int i = 0; i < connections.Length; i++)
        {
            if (connections[i].IsCreated)
            {
              //  Debug.Log($"Sending {msg.Code} To : {connections[i].InternalId}");
                SendToClient(connections[i], msg);
            }
        }
    }
}
