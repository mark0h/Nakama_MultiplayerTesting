using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nakama;
using UnityEngine.EventSystems;
using System.Threading;
using MGR.Creations;
using System;
using System.Text;

[Serializable]
public class MatchRoomClass
{
    public string addRemove;
    public string matchName;
    public string userName;
    public string matchIDGUID;
}

public class MatchController : MonoBehaviour {

    public static MatchController Singleton;

    private INClient client;
    private INTopicId matchListTopic;
    private INMatch matchValues;
    private Dictionary<string, byte[]> matchNameByteList = new Dictionary<string, byte[]>();
    private Dictionary<string, string> matchNameUserList = new Dictionary<string, string>();

    //GameObjects Create Match Panel
    [Header("Create Match Panel")]
    public Transform createMatchPanel;
    public InputField createdMatchNameInput;
    private string createdMatchNameString;
    public Text createMatchPanelErrorText;

    //GameObject Match List Panel
    [Header("Match List Panel")]
    public Transform matchListPanel;
    public Transform matchListScrollContent;
    public InputField matchNameInput;
    public Text opponentName;

    public GameObject buttonPrefab;

    //GameMatch Variables
    private string matchName;
    private byte[] matchID;
    private bool updateMatchName = false;

    //TEMP DEBUGGIN
    private byte[] testByte = { 1, 2, 3 };

    // Use this for initialization
    void Start ()
    {
        Singleton = this;
        matchListPanel.gameObject.SetActive(false);
        createMatchPanel.gameObject.SetActive(false);
        opponentName.text = "";

    }
	
	// Update is called once per frame
	void Update ()
    {
        if (updateMatchName)
        {
            matchNameInput.text = matchName;
            updateMatchName = false;
        }           
    }

    //Create Match Button Pressed
    public void CreateandJoinMatch()
    {
        if(createdMatchNameInput.text.Length < 10 || createdMatchNameInput.text.Length > 30)
        {
            createMatchPanelErrorText.text = "Invalid match name! Must be between 10 and 30 characters. Currently only " + createdMatchNameInput.text.Length + " characters.";
            return;
        }

        Debug.Log("Creating a Match!!  Name: " + createdMatchNameInput.text);

        matchName = createdMatchNameInput.text;

        RegisterOnMatchData();

        client = NakamaData.Singleton.Client;
        ManualResetEvent createEvent = new ManualResetEvent(false);

        client.Send(NMatchCreateMessage.Default(), (INMatch match) =>
        {
            matchID = match.Id;
            matchValues = match;
            client.Send(NMatchJoinMessage.Default(match.Id), (INMatch match2) =>
            {
                createEvent.Set();
            }, (INError err) =>
            {
                Debug.Log("Failed to Join created match. Error: " + err);
                createEvent.Set();
            });
        }, (INError err) =>
        {
            Debug.Log("Failed to create a match. Error: " + err);
            createEvent.Set();
        });

        createEvent.WaitOne(5000, false);
        createMatchPanel.gameObject.SetActive(false);
        SendMatchInfoToMatchRoom("add");
        SendMessages.Singleton.LeaveRoom();
        SendMessages.Singleton.JoinRoom(matchName);
        GameManager.Singleton.StartNewGamePlay(createdMatchNameInput.text);
    }


    /// <summary>
    /// The following is used to create a Match List. Hopefully Nakama releases the version that does this for us! Yay!
    /// TODO: Fix this once that release comes out
    /// Methods::::
    /// SendMatchInfoToMatchRoom(), JoinMatchRoom(), 
    /// </summary>
    /// 
    public void JoinMatchRoom()
    {
        client = NakamaData.Singleton.Client;
        ManualResetEvent joinEvent = new ManualResetEvent(false);
        var message = new NTopicJoinMessage.Builder().TopicRoom(Encoding.UTF8.GetBytes("match-list")).Build();
        client.Send(message, (INTopic topic) =>
        {
            matchListTopic = topic.Topic;
            joinEvent.Set();
        }, (INError err) =>
        {
            Debug.Log("Failed to join room : " + err);
            joinEvent.Set();
        });

        joinEvent.WaitOne(1000, false);
        RegisterMatchListRoom();
    }
    private void SendMatchInfoToMatchRoom(string addRemove)
    {
        client = NakamaData.Singleton.Client;
        ManualResetEvent sendMessage = new ManualResetEvent(false);
        Guid matchID_Guid = new Guid(matchID);

        //Debug.LogWarning("Encoding.UTF8.GetString(matchID): " + Encoding.UTF8.GetString(matchID));
        //Guid test = new Guid(matchID);

        string chatMessage = "{\"addRemove\":\"" + addRemove + "\",\"matchName\":\"" + matchName + "\",\"userName\":\"" + NakamaData.Singleton.ClientUserName + "\",\"matchIDGUID\":\"" + matchID_Guid.ToString() + "\"}";
        //Debug.LogWarning("DEBUG:::: chatJson.matchIDGUID: " + test + " chatJson.matchIDGUID..ToByteArray(): " + test.ToByteArray() + " Encoding.UTF8.GetString(matchID)" + Encoding.UTF8.GetString(matchID));
        //Debug.LogWarning("DEBUG:::: Encoding.UTF8.GetString(test.ToByteArray()): " + Encoding.UTF8.GetString(test.ToByteArray()) + " Encoding.UTF8.GetString(matchID)" + Encoding.UTF8.GetString(matchID));
        NTopicMessageSendMessage msg = NTopicMessageSendMessage.Default(matchListTopic, Encoding.UTF8.GetBytes(chatMessage));
        client.Send(msg, (INTopicMessageAck ack) =>
        {
            Debug.Log("Match Data being sent: " + chatMessage);
            sendMessage.Set();
        }, (INError error) =>
        {
            Debug.LogErrorFormat("Player could not send message: '{0}'.", error.Message);
        });

        sendMessage.WaitOne(1000, false);
    }

    //Called when "Join Game" button on game menu is pressed
    public void SetupMatchListPanel()
    {
        //Add Buttons of Current matches
        foreach (var pair in matchNameUserList)
        {
            GameObject newButton = Instantiate(buttonPrefab);
            newButton.transform.SetParent(matchListScrollContent);
            newButton.transform.name = pair.Key;
            newButton.transform.GetComponentInChildren<Text>().text = pair.Key;
        }

        matchListPanel.gameObject.SetActive(true);

    }

    //Called when "Join Match" is clicked in the Match List Panel
    public void JoinMatch()
    {
        Debug.Log("Joining the match: " + matchName);

        ManualResetEvent joinEvent = new ManualResetEvent(false);
        matchID = matchNameByteList[matchName];
        string opponentName = matchNameUserList[matchName];
        client = NakamaData.Singleton.Client;
        client.Send(NMatchJoinMessage.Default(matchID), (INMatch match) =>
        {
            joinEvent.Set();
        }, (INError err) =>
        {
            Debug.Log("Failed to Join match. Error: " + err);
            joinEvent.Set();
        });

        joinEvent.WaitOne(1000, false);
        SendMatchInfoToMatchRoom("remove");
        SendMessages.Singleton.LeaveRoom();
        SendMessages.Singleton.JoinRoom(matchName);
        GameManager.Singleton.StartNewGamePlay(matchName, opponentName);
    }

    public void QuitMatch()
    {
        Debug.LogWarning("Quitting Match");
        ManualResetEvent quitEvent = new ManualResetEvent(false);
        client = NakamaData.Singleton.Client;
        client.Send(NMatchLeaveMessage.Default(matchID), (bool complete) =>
        {
            Debug.LogWarning("Successfully Quit Match");
        }, (INError err) =>
        {
            Debug.LogWarning("Could not quit match. Error: " + err);
            quitEvent.Set();
        });
        quitEvent.WaitOne(1000, false);
        SendMatchInfoToMatchRoom("remove");
        UnRegisterOnMatchData();
        SendMessages.Singleton.LeaveRoom();
        SendMessages.Singleton.JoinRoom("default-room");
        GameManager.Singleton.QuitCurrentGameMatch(NakamaData.Singleton.ClientUserName + " left the match");
    }

    //Called when a Match listed in the MatchListScroll is pressed
    public void MatchListButtonPressed()
    {
        matchName = EventSystem.current.currentSelectedGameObject.name;
        opponentName.text = matchNameUserList[matchName];
        updateMatchName = true;
        Debug.Log("Button clicked! " + matchName);
    }

    //Called when "Create Match" button on game menu is pressed
    public void CreateGameMenuButtonPressed()
    {
        createMatchPanel.gameObject.SetActive(true);
    }

    //Called when X button on panel is pressed
    public void CloseCreatematchPanel()
    {
        createMatchPanel.gameObject.SetActive(false);
    }

    //Called when X button on panel is pressed
    public void CloseMatchListPanel()
    {
        foreach (Transform child in matchListScrollContent)
        {
            GameObject.Destroy(child.gameObject);
        }
        matchListPanel.gameObject.SetActive(false);
    }


    ///<summary>
    ///The following is for registering an unregistering Matches
    ///</summary>
    ///
    void c_OnMatchData(object src, NMatchDataEventArgs args)
    {
        
    }

    void c_OnMatchPresence(object source, NMatchPresenceEventArgs args)
    {
        if(args.MatchPresence.Leave.Count > 0)
        {
            GameManager.Singleton.UpdateInfoFlashPanelMessage("Opponenet has left. You win!!!");
        }
    }

    void MatchList_OnTopicMessage(object sender, NTopicMessageEventArgs e)
    {
        ManualResetEvent updateEvent = new ManualResetEvent(false);

        var bytesAsString = Encoding.ASCII.GetString(e.Message.Data);
        var chatJson = JsonUtility.FromJson<MatchRoomClass>(bytesAsString);
        Guid tempMatchID = new Guid(chatJson.matchIDGUID);

        if (chatJson.addRemove == "add")
        {
            Debug.Log("Adding match");
            matchNameByteList.Add(chatJson.matchName, tempMatchID.ToByteArray());
            matchNameUserList.Add(chatJson.matchName, chatJson.userName);
            Debug.Log("Added match matchNameByteList.Count: " + matchNameByteList.Count + " matchNameUserList.Count: " + matchNameUserList.Count);
            updateEvent.Set();
        } else
        {
            Debug.Log("Removing match matchNameByteList.Count: " + matchNameByteList.Count + " matchNameUserList.Count: " + matchNameUserList.Count);
            matchNameByteList.Remove(chatJson.matchName);
            matchNameUserList.Remove(chatJson.matchName);
            Debug.Log("Removed match matchNameByteList.Count: " + matchNameByteList.Count + " matchNameUserList.Count: " + matchNameUserList.Count);
            updateEvent.Set();
        }
        updateEvent.WaitOne(1000, false);
    }

    public void RegisterOnMatchData()
    {
        client = NakamaData.Singleton.Client;
        client.OnMatchData += c_OnMatchData;
        client.OnMatchPresence += c_OnMatchPresence;
    }

    public void UnRegisterOnMatchData()
    {
        client = NakamaData.Singleton.Client;
        client.OnMatchData -= c_OnMatchData;
        client.OnMatchPresence -= c_OnMatchPresence;
    }

    public void RegisterMatchListRoom()
    {
        client = NakamaData.Singleton.Client;
        client.OnTopicMessage += MatchList_OnTopicMessage;
    }

    public void UnRegisterMatchListRoom()
    {
        client = NakamaData.Singleton.Client;
        client.OnTopicMessage -= MatchList_OnTopicMessage;
    }

    public void SendMatchData(int opCode, byte[] data)
    {
        var message = NMatchDataSendMessage.Default(matchID, opCode, data);
        client.Send(message, (bool complete) =>
        {
            Debug.Log("Successfully sent data to match.");
        }, (INError error) => {
            Debug.LogErrorFormat("Could not send data to match: '{0}'.", error.Message);
        });
    }
}
