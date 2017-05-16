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
    public string matchName;
    public string userName;
    public string matchID;
    public Guid matchIDGUID;
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

    public void CreateandJoinMatch()
    {
        if(createdMatchNameInput.text.Length < 10 || createdMatchNameInput.text.Length > 30)
        {
            createMatchPanelErrorText.text = "Invalid match name! Must be between 10 and 30 characters. Currently only " + createdMatchNameInput.text.Length + " characters.";
            return;
        }

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
        SendMatchInfoToMatchRoom();
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
    private void SendMatchInfoToMatchRoom()
    {
        client = NakamaData.Singleton.Client;
        ManualResetEvent sendMessage = new ManualResetEvent(false);

        //    public string matchName;
        //public string userName;
        //public byte[] matchID;
        //{"level":1,"timeElapsed":47.5,"playerName":"Dr Charles Francis"}

        Debug.LogWarning("Encoding.UTF8.GetString(matchID): " + Encoding.UTF8.GetString(matchID));
        Guid test = new Guid(matchID);

        string chatMessage = "{\"matchName\":\"" + matchName + "\",\"userName\":\"" + NakamaData.Singleton.ClientUserName + "\",\"matchIDGUID\":\"" + test + "\"}";
        Debug.LogWarning("chatMessage for Match Info: " + chatMessage);
        Debug.LogWarning("DEBUG:::: chatJson.matchIDGUID: " + test + " chatJson.matchIDGUID..ToByteArray(): " + test.ToByteArray() + " Encoding.UTF8.GetString(matchID)" + Encoding.UTF8.GetString(matchID));
        Debug.LogWarning("DEBUG:::: Encoding.UTF8.GetString(test.ToByteArray()): " + Encoding.UTF8.GetString(test.ToByteArray()) + " Encoding.UTF8.GetString(matchID)" + Encoding.UTF8.GetString(matchID));
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

        //TEMP FOR DEBUGGING
        //matchNameUserList.Add("Join My Awesome Game Yea Yea!!", "ThisUserYea!");
        //matchNameUserList.Add("Fight me Buddy!", "Markoooo123");
        //matchNameUserList.Add("Another Match, come on!", "DemonKingYA!");

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


    ///<summary>
    ///The following is for registering an unregistering Matches
    ///</summary>
    ///
    void c_OnMatchData(object src, NMatchDataEventArgs args)
    {
        
    }

    void MatchList_OnTopicMessage(object sender, NTopicMessageEventArgs e)
    {        
        var bytesAsString = Encoding.ASCII.GetString(e.Message.Data);
        var chatJson = JsonUtility.FromJson<MatchRoomClass>(bytesAsString);
        matchNameByteList.Add(chatJson.matchName, Encoding.UTF8.GetBytes(chatJson.matchID));
        matchNameUserList.Add(chatJson.matchName, chatJson.userName);

        Debug.LogWarning("DEBUG:::: chatJson.matchIDGUID: " + chatJson.matchIDGUID + "chatJson.matchIDGUID.ToString: " + chatJson.matchIDGUID.ToString() + " Encoding.UTF8.GetString(matchID)" + Encoding.UTF8.GetString(matchID));
    }

    public void RegisterOnMatchData()
    {
        client = NakamaData.Singleton.Client;
        client.OnMatchData += c_OnMatchData;
    }

    public void UnRegisterOnMatchData()
    {
        client = NakamaData.Singleton.Client;
        client.OnMatchData -= c_OnMatchData;
    }

    public void RegisterMatchListRoom()
    {
        client = NakamaData.Singleton.Client;
        client.OnTopicMessage += MatchList_OnTopicMessage;
    }
}
