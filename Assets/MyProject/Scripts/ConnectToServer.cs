using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nakama;
using System.Text;
using System.Threading;

public class ConnectToServer : MonoBehaviour {

    #region
    public static ConnectToServer ServerInstance { get; private set; }

    private INClient client;
    private INSession session;    

    //Server info
    private static readonly string ServerKey = "defaultkey";
    public string ServerIP = "35.166.239.83";
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
    public InputField passwordInputField;


    //Debugging Stuff
    //private string outputText = "";
    //public Text OutputTextField;
    //public GameObject loginLabel;

    #endregion

    void Awake()
    {
        // Save a reference to the AudioHandler component as our singleton instance
        ServerInstance = this;
    }

    // Use this for initialization
    void Start ()
    {
        client = new NClient.Builder(ServerKey)
            .Host(ServerIP)
            .Port(serverPort)
            .SSL(useSSL)
            .Build();        


        ServerInstance.client = client;

    }

    // Update is called once per frame
    void Update()
    {
        //OutputTextField.text = outputText;
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
        SendMessages.Singleton.UnRegisterOnTopicMessagePresence();
        client.Disconnect();
    }

    //When using clicks "Connect" button
    //Does 3 things
    //1. Tries to register the client(this is so messy!)
    //2. Tries to log in
    //3. Tries to connect
    public void ClientConnect()
    {

        loginErrorString = "";
        if (clientConnected)
        {
            loginErrorString = "Already connected!\n";
            return;
        }

        //Allows us to wait until the register check is complete before moving onto login
        ManualResetEvent nakamaEvent = new ManualResetEvent(false);
        INError error = null;

        //Get Device Unique ID. If not already in PlayerPrefs, get it from device, and then set PlayerPrefs
        deviceID = PlayerPrefs.GetString("ID");
        if (string.IsNullOrEmpty(deviceID))
        {
            deviceID = SystemInfo.deviceUniqueIdentifier;
            PlayerPrefs.SetString("ID", deviceID);
        }

        //TEMP DEBUGGING!
        //Text loginText = loginLabel.GetComponent<Text>();
        deviceID = userNameInputField.text;
        Debug.Log("Logging in as: " + deviceID + " to IP: " + ServerIP);

        ////Check if registered
        Debug.Log("Trying to register");
        var request = NAuthenticateMessage.Device(deviceID);
        client.Register(request, (INSession session) =>
        {
            //clientRegistered = true;
        }, (INError err) =>
        {
            error = err;
            if (error.Code == ErrorCode.AuthError)
                Debug.Log("Already registered");
            //clientRegistered = true;
            else
                Debug.LogErrorFormat("ID register '{0}' failed: {1}", deviceID, error);            
        });

        nakamaEvent.WaitOne(1000, false); //Waiting until Register is complete, pass or fail
        nakamaEvent.Reset();

        ////Try to Login
        client.Login(request, (INSession session) =>
        {
            //outputText += "Player logged in successfully. ID: " + deviceID.ToString() + "\n";
            this.session = session;
            ServerInstance.session = session;
            client.Connect(session);
            clientConnected = true;
            nakamaEvent.Set();
        }, (INError err) =>
        {
            error = err;
            loginErrorString = "Player failed to login! ID: " + deviceID.ToString() + " Error: " + error.ToString() + "\n";
            nakamaEvent.Set();
        });

        nakamaEvent.WaitOne(1000, false);

        if (clientConnected)
        {
            FetchClientInfo();
            UpdateClientSession();
            UpdateUserInfo();
            JoinDefaultRoom();
        }
        else
        {
            loginErrorString = "There was a connection issue: " + error.ToString();
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

    private void UpdateClientSession()
    {
        SendMessages.Singleton.SetClientSession(client, session, deviceID, clientID);
        if (!clientRegistered)
        {
            SendMessages.Singleton.RegisterOnTopicMessagePresence();
            clientRegistered = true;
        }
            
    }
}
