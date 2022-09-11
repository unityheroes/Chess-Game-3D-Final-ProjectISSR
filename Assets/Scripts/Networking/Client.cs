using System;
using Unity.Networking.Transport;
using UnityEngine;

public class Client : MonoBehaviour
{
    //Rules
    public NetworkDriver driver;
    private bool isActive = false;
    private NetworkConnection connection;

    public Action connectionDropped;
    
    #region Singleton implementation 
    public static Client Instance { set; get; }

    private void Awake()
    {
        Instance = this;
    }

    #endregion

    
    //Methods
    public void Init(string ip, ushort port) // initalize port
    {
        driver = NetworkDriver.Create();
        NetworkEndPoint endPoint = NetworkEndPoint.Parse(ip,port);
        connection = driver.Connect(endPoint);
        Debug.Log("Attemping to connect to server on " + endPoint.Address);
        isActive = true;
        RegisterToEvent();
    }
  
  
    public void Shutdown() // close off the server
    {
        if (isActive)
        {
            UnRegisterToEvent();
            driver.Dispose();           
            isActive = false;
            connection = default(NetworkConnection);
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
        driver.ScheduleUpdate().Complete(); // to make job system queue of msgs
        CheckAlive();
        UpdateMessagePump();  // is are they sending us message and if so  do we have to reply
    }
    private void CheckAlive()
    {
        if (!connection.IsCreated && isActive)
        {
            Debug.Log("SomeThing went worng, we lost connection to server");
            connectionDropped?.Invoke();
            Shutdown();
        }
    }


    private void UpdateMessagePump()
    {
        DataStreamReader stream;
        
            NetworkEvent.Type cmd;
            while ((cmd = connection.PopEvent(driver,out stream)) != NetworkEvent.Type.Empty)
            {

                if (cmd == NetworkEvent.Type.Connect)
                {
                
                 SendToServer(new NetWelcome());
                Debug.Log("we are connected!");
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    NetUtility.OnData(stream, default(NetworkConnection));
                }

                else if (cmd == NetworkEvent.Type.Disconnect)           
                {
                     Debug.Log("Client Disconnect from server ");
                     connection = default(NetworkConnection);
                     connectionDropped?.Invoke();
                     Shutdown();

                    
                }
            }
        
    }
    public  void SendToServer(NetMessages msg)
    {
        DataStreamWriter writer;
        driver.BeginSend(connection, out writer);
        msg.Serialize(ref writer);
        driver.EndSend(writer);
    }

    //Event parsing
    private void RegisterToEvent()
    {
         NetUtility.C_KEEP_ALIVE += OnKeepAlive;
    }
    private void UnRegisterToEvent()
    {
        NetUtility.C_KEEP_ALIVE -= OnKeepAlive;
    }
    private void OnKeepAlive(NetMessages nm)
    {
        //Sent it Back , to Keep Both side alive
        SendToServer(nm);
    }


  

}
