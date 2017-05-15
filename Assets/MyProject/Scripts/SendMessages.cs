using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nakama;
using System;
using System.Text;
using System.Threading;
using System.Linq;
using System.Text.RegularExpressions;

public class SendMessages : MonoBehaviour
{

    public static SendMessages Singleton;

    private NakamaData nakamaData;

    private INClient client;
    private byte[] clientID = null;
    private INSession session;
    private INTopicId currentTopic;
    private IList<INUserPresence> userList = new List<INUserPresence>();

    //For Chat Text
    public Text chatTextBox;
    public Text chatRoomName;
    private List<string> chatText = new List<string>();
    private string clientUserName;
    public InputField chatInputBox;
    private string chatInputText;
    private string currentRoom;
    bool allowSend;


    //Our UserList View
    public Text userListScrollView;
    private List<byte[]> chatUsersJoinedID = new List<byte[]>();
    private List<byte[]> chatUsersLeftID = new List<byte[]>();
    private HashSet<string> currentChatUsers = new HashSet<string>();
    private bool UserListChange = false;
    private Func<object, NTopicMessageEventArgs, object> registeredOnTopicMessage;

    // Use this for initialization
    void Start()
    {
        Singleton = this;       
    }

    void Update()
    {
        //Update UserList for Chat Room
        if (UserListChange) //(currentUserCount != chatUsersJoinedID.Count)
        {
            foreach (var UserId in chatUsersJoinedID)
            {
                currentChatUsers.Add(FetchUserFullName(UserId));
            }

            foreach (var UserId in chatUsersLeftID)
            {
                currentChatUsers.Remove(FetchUserFullName(UserId));
            }
            UserListChange = false;
            chatUsersJoinedID.Clear();
            chatUsersLeftID.Clear();
        }

        //Update Chat text and UserList
        chatTextBox.text = string.Join("\n", chatText.ToArray());
        userListScrollView.text = string.Join("\n", currentChatUsers.ToArray());
        chatRoomName.text = currentRoom;

        //Send Message if Enter is hit
        if (allowSend && chatInputBox.text != "" && Input.GetKey(KeyCode.Return))
        {
            allowSend = false;
            SendChatMessage();
        }
        else
        {
            allowSend = chatInputBox.isFocused;
        }
    }

    public void SetClientSession(INClient newClient, INSession newSession, string clientUserValue, byte[] clientIDValue)
    {
        //client = newClient;
        //session = newSession;
        clientUserName = clientUserValue;
        clientID = clientIDValue;
    }

    //Will join a room for the client, and update that room's userlist for all clients
    public void JoinRoom(string roomName)
    {
        client = NakamaData.Singleton.Client;
        ManualResetEvent joinEvent = new ManualResetEvent(false);
        currentRoom = roomName;
        var message = new NTopicJoinMessage.Builder().TopicRoom(Encoding.UTF8.GetBytes(roomName)).Build();
        client.Send(message, (INTopic topic) =>
        {
            chatText.Add("Successfully joined the Default Room." + " There are currently " + topic.Presences.Count + " Users");
            userList = topic.Presences;
            currentTopic = topic.Topic;
            joinEvent.Set();
        }, (INError err) =>
        {
            Debug.Log("Failed to join room : " + err);
            joinEvent.Set();
        });

        joinEvent.WaitOne(1000, false);
        foreach (var userInList in userList)
        {
            if (!chatUsersJoinedID.Contains(userInList.UserId))
                chatUsersJoinedID.Add(userInList.UserId);
        }
        Debug.Log("JoinRoom::  ::chatUsersJoinedID.count: " + chatUsersJoinedID.Count);

        UserListChange = true;
        RegisterOnTopicMessagePresence();
    }

    public void LeaveRoom()
    {
        client = NakamaData.Singleton.Client;
        ManualResetEvent leaveEvent = new ManualResetEvent(false);

        var message = new NTopicJoinMessage.Builder().TopicRoom(Encoding.UTF8.GetBytes(currentRoom)).Build();
        client.Send(message, (INTopic topic) =>
        {
            client.Send(NTopicLeaveMessage.Default(topic.Topic), (bool complete) =>
            {
                leaveEvent.Set();
            }, (INError err) => {
                Debug.Log("Failed to complete leaving of room : " + err);
                leaveEvent.Set();
            });
        }, (INError err) =>
        {
            Debug.Log("Failed to leave room : " + err);
            leaveEvent.Set();
        });

        leaveEvent.WaitOne(1000, false);
        chatText.Clear();
        currentChatUsers.Clear();
        //chatUsersJoinedID.Clear();
        currentRoom = "";

        UnRegisterOnTopicMessagePresence();
    }    

    public void SendChatMessage()
    {
        client = NakamaData.Singleton.Client;

        if (chatInputBox.text == "")
            return;
        Debug.LogWarning("Sending a message");
        ManualResetEvent sendMessage = new ManualResetEvent(false);
        chatInputText = chatInputBox.text;
        chatInputBox.text = "";
        string chatMessage = "{\"Data\": \"[" + clientUserName + " " + DateTime.Now.ToString("HH:mm:ss") + "] " + chatInputText + "\"}";
        NTopicMessageSendMessage msg = NTopicMessageSendMessage.Default(currentTopic, Encoding.UTF8.GetBytes(chatMessage));
        client.Send(msg, (INTopicMessageAck ack) =>
        {
            Debug.Log("Message being sent");
            sendMessage.Set();
        }, (INError error) =>
        {
            Debug.LogErrorFormat("Player could not send message: '{0}'.", error.Message);
        });

        sendMessage.WaitOne(1000, false);      

    }

    /// <summary>
    /// OnTopMessage and OnTopPresence Registering
    /// </summary>
    /// 
    void c_OnTopicMessage(object sender, NTopicMessageEventArgs e)
    {
        string chatValue = Encoding.UTF8.GetString(e.Message.Data).Substring(10);
        chatValue = chatValue.Substring(0, chatValue.Length - 2);
        chatText.Add(chatValue);
    }

    void c_OnTopicPresence(object source, NTopicPresenceEventArgs args)
    {
        ManualResetEvent updateEvent = new ManualResetEvent(false);
        if (args.TopicPresence.Join.Count > 0)
        {
            UserListChange = true;
            for (int i = 0; i < args.TopicPresence.Join.Count; i++)
            {

                if (clientID.SequenceEqual(args.TopicPresence.Join[i].UserId))
                    Debug.LogWarning("Will not add this client to chatUsersJoinedID. Already added when joined room");
                else
                    chatUsersJoinedID.Add(args.TopicPresence.Join[i].UserId);

                Debug.LogWarning("Added client to chatUsersJoinedID");
            }
            updateEvent.Set();
        }
        if (args.TopicPresence.Leave.Count > 0)
        {
            UserListChange = true;
            for (int i = 0; i < args.TopicPresence.Leave.Count; i++)
            {
                chatUsersLeftID.Add(args.TopicPresence.Leave[i].UserId);
            }
            Debug.Log("USER LEFT:: ::chatUsersLeftID.Count: " + chatUsersLeftID.Count);
            updateEvent.Set();
        }

        updateEvent.WaitOne(2000, false);
    }

    public void RegisterOnTopicMessagePresence()
    {
        client.OnTopicMessage += c_OnTopicMessage;
        client.OnTopicPresence += c_OnTopicPresence;
    }

    public void UnRegisterOnTopicMessagePresence()
    {
        client.OnTopicMessage -= c_OnTopicMessage;
        client.OnTopicPresence -= c_OnTopicPresence;
    }

    //Used to get the username from userID
    private string FetchUserFullName(byte[] userID)
    {

        ManualResetEvent fetchEvent = new ManualResetEvent(false);

        string fullNameValue = "";
        var message = NUsersFetchMessage.Default(userID);
        client.Send(message, (INResultSet<INUser> results) => {
            //Debug.LogFormat("Fetched {0} users.", results.Results.Count);
            foreach (INUser user in results.Results)
            {
                fullNameValue = user.Fullname;
            }
            fetchEvent.Set();
        }, (INError error) =>
        {
            Debug.LogErrorFormat("Could not retrieve users: '{0}'.", error.Message);
            fetchEvent.Set();
        });
        fetchEvent.WaitOne(1000, false);
        return fullNameValue;
    }   
}
