using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public enum cameraAngle
{
    menu = 0,
    whiteTeam = 1,
    blackTeam =2
}
public class GameUI : MonoBehaviour
{
    
    // Start is called before the first frame update
    public static GameUI Instance { set; get; }
    public Server server;
    public Client client;
    [SerializeField] private Animator menuAnimator;
    [SerializeField] private TMP_InputField addressInput; // text mesh pro inputfield who in GUI 
    [SerializeField] private GameObject[] cameraAngles;
    // Camera 
    public Action<bool> SetlocalGame;
    public void ChangeCamera(cameraAngle index)
    {
        for (int i = 0; i < cameraAngles.Length; i++)
        {
            cameraAngles[i].SetActive(false);
            // to off all cameras 
        }
        // TO ENABLE CAMERA INDEX
        cameraAngles[(int)index].SetActive(true);
        
    }

    private void Awake()
    {
        Instance = this;
        RegisterEvents();

    }
    public void OnlocalGameButton()
    {
        menuAnimator.SetTrigger("InGame");
        SetlocalGame?.Invoke(true);
        server.Init(8007); // server initialize at port 8007 
        client.Init("127.0.0.1", 8007); // local host client
    }
    public void OnOnlineGameButton()
    {
        menuAnimator.SetTrigger("OnlineMenu");
    }
    public void OnOnlineHostButton()
    {
        SetlocalGame?.Invoke(false);
        server.Init(8007); // server initialize at port 8007 
        client.Init("127.0.0.1",8007); // local host client
        menuAnimator.SetTrigger("HostMenu");
    }
    public void OnOnlineConnectButton()
    {
        SetlocalGame?.Invoke(false);
        client.Init(addressInput.text, 8007);  // input address form InputField
    }
    public void OnOnlineBackButton()
    {
        menuAnimator.SetTrigger("StartMenu");
    }
    public void OnHostBackButton()
    {
        server.Shutdown();
        client.Shutdown();
        menuAnimator.SetTrigger("OnlineMenu");
    }

    public void OnLeaveFromGameMenu()
    {
        ChangeCamera(cameraAngle.menu);
        menuAnimator.SetTrigger("StartMenu");
       
    }


    #region
    private void RegisterEvents()
    {
        
        NetUtility.C_START_GAME += OnStartGameClient;
    }


    private void UnRegisterEvents()
    {
        NetUtility.C_START_GAME -= OnStartGameClient;
    }
    private void OnStartGameClient(NetMessages obj)
    {
        menuAnimator.SetTrigger("InGame");
    }
    
   
    
    #endregion
}
