using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nakama;
using System.Text;
using System.Threading;
using MGR.Creations;

public class ConnectToServer : MonoBehaviour {

    #region
    public static ConnectToServer ServerInstance { get; private set; }

    //private NakamaData nakamaData;

    private INClient client;
    private INSession session;    

    //Server info
    private static readonly string ServerKey = "defaultkey";
    public string ServerIP = "34.210.109.19";
    public uint serverPort = 7350;
    public bool useSSL = false;

    private bool clientConnected = false;
    private bool clientRegistered = false;
    private byte[] clientID = null;
    private string deviceID;

    //Login Panel Objects
    public Text loginErrorText;
    private string loginErrorString;
    public InputField userNameInputField;
    private string userNameString;
    public InputField passwordInputField;
    public InputField serverIPInputField;


    //Debugging Stuff
    //private string outputText = "";
    //public Text OutputTextField;
    //public GameObject loginLabel;

    #endregion

    void Awake()
    {
        ServerInstance = this;
    }

    // Use this for initialization
    void Start ()
    {
        ServerInstance.client = client;

        serverIPInputField.text = ServerIP;
        Debug.Log("serverIPInputField.text: " + serverIPInputField.text);

        string lastUserName = PlayerPrefs.GetString("UserName");
        userNameInputField.text = lastUserName;

    }

    // Update is called once per frame
    void Update()
    {
        loginErrorText.text = loginErrorString;
    }

    void OnApplicationQuit()
    {
         client.Disconnect();
    }

    public void ClientDisconnect()
    {
        clientConnected = false;
        Debug.Log("Client has disconnected");
        SendMessages.Singleton.LeaveRoom();
        //SendMessages.Singleton.UnRegisterOnTopicMessagePresence();
        client.Disconnect();
    }

    private void ClientBuilder()
    {
        client = new NClient.Builder(ServerKey)
            .Host(ServerIP)
            .Port(serverPort)
            .SSL(useSSL)
            .Build();

        NakamaData.Singleton.Client = client;
    }

    public void ClientRegister()
    {
        if(userNameInputField.text.Length < 8)
        {
            loginErrorString = "Invalid UserName. Must be at least 8 characters!";
            return;
        }           
        if (serverIPInputField.text.Length < 11)
        {
            loginErrorString = "Invalid IP!";
            return;
        }            

        ServerIP = serverIPInputField.text;

        ClientBuilder();

        deviceID = userNameInputField.text;
        Debug.Log("Logging in as: " + deviceID + " to IP: " + ServerIP);
        PlayerPrefs.SetString("UserName", deviceID);


        ManualResetEvent registerEvent = new ManualResetEvent(false);

        //Register and connect client
        Debug.Log("Trying to register");
        var request = NAuthenticateMessage.Device(deviceID);
        client.Register(request, (INSession session) =>
        {
            this.session = session;
            ServerInstance.session = session;
            client.Connect(session);
            clientConnected = true;
            NakamaData.Singleton.Session = session;
            registerEvent.Set();
        }, (INError err) =>
        {
            if (err.Code == ErrorCode.AuthError)
                loginErrorString = "This UserName is already registered!";
            else
                loginErrorString = "ID register " + deviceID + " failed: " + err;

            registerEvent.Set();
        });

        registerEvent.WaitOne(1000, false);

        if (clientConnected)
        {
            FetchClientInfo();
            UpdateClientSession();
            UpdateUserInfo();
            JoinDefaultRoom();
            CompleteLogin();
        }

    }
    public void ClientLogin()
    {
        if (userNameInputField.text.Length < 10)
        {
            loginErrorString = "Invalid UserName. Must be at least 10 characters. UserName entered was only " + userNameInputField.text.Length  + " characters";
            return;
        }
        if (serverIPInputField.text.Length < 11)
        {
            loginErrorString = "Invalid IP!";
            return;
        }

        ServerIP = serverIPInputField.text;

        ClientBuilder();

        deviceID = userNameInputField.text;
        Debug.Log("Logging in as: " + deviceID + " to IP: " + ServerIP);
        PlayerPrefs.SetString("UserName", deviceID);

        //Allows us to wait until the register check is complete before moving onto login
        ManualResetEvent loginEvent = new ManualResetEvent(false);

        ////Try to Login
        var request = NAuthenticateMessage.Device(deviceID);
        client.Login(request, (INSession session) =>
        {
            if(ServerInstance.session == session)
            {
                loginErrorString = "This user is already logged in!";
                loginEvent.Set();
                return;
            }
            //outputText += "Player logged in successfully. ID: " + deviceID.ToString() + "\n";
            this.session = session;
            ServerInstance.session = session;
            client.Connect(session);
            clientConnected = true;
            NakamaData.Singleton.Session = session;
            loginEvent.Set();
        }, (INError err) =>
        {
            loginErrorString = "Player failed to login! ID: " + deviceID.ToString() + " Error: " + err.ToString() + "\n";
            loginEvent.Set();
        });

        loginEvent.WaitOne(1000, false);

        if (clientConnected)
        {
            FetchClientInfo();
            UpdateClientSession();
            UpdateUserInfo();
            JoinDefaultRoom();
            CompleteLogin();
        }
    }

    private void FetchClientInfo()
    {

        ManualResetEvent nakamaEvent = new ManualResetEvent(false);
        INError error = null;

        client.Send(NSelfFetchMessage.Default(), (INSelf result) => {
            clientID = result.Id;
            nakamaEvent.Set();
        }, (INError err) =>
        {
            error = err;
            Debug.LogErrorFormat("Could not retrieve client self: '{0}'.", error.Message);
            nakamaEvent.Set();
        });

        nakamaEvent.WaitOne(1000, false);
    }

    private void UpdateUserInfo()
    {

        //ManualResetEvent nakamaEvent = new ManualResetEvent(false);

        Debug.Log("Updating User Info");
        var message = new NSelfUpdateMessage.Builder()
                    .Fullname(deviceID)
                    //.Lang("en")
                    //.Location("San Francisco")
                    //.Timezone("Pacific Time")
                    .Build();
        client.Send(message, (bool completed) => {
            Debug.Log("Successfully updated user information");
        }, (INError error) =>
        {
            Debug.LogErrorFormat("Could not update self: '{0}'.", error.Message);
        });
    }

    public void JoinDefaultRoom()
    {
        SendMessages.Singleton.JoinRoom("default-room");
    }

    private void CompleteLogin()
    {
        GameManager.Singleton.HideLoginPanel();
        GameManager.Singleton.ShowChatAndUserList();
    }

    private void UpdateClientSession()
    {
        SendMessages.Singleton.SetClientSession(client, session, deviceID, clientID);
        //if (!clientRegistered)
        //{
        //    SendMessages.Singleton.RegisterOnTopicMessagePresence();
        //    clientRegistered = true;
        //}
            
    }
}
