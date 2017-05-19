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
    public string matchMaxHealth;
}

[Serializable]
public class MatchSettings
{
    public string matchCreator;
    public string matchName;
    public string maxHealth;
    public string matchStatus;  //carry value of "open" or "closed"
}

public class MatchController : MonoBehaviour {

    public static MatchController Singleton;

    private INClient client;
    private INTopicId matchListTopic;
    private INMatch matchValues;
    private Dictionary<string, Guid> matchNameMatchGuid = new Dictionary<string, Guid>();
    private Dictionary<Guid, MatchSettings> matchGuidMatchSettings = new Dictionary<Guid, MatchSettings>();

    //GameObjects Create Match Panel
    [Header("Create Match Panel")]
    public Transform createMatchPanel;
    public InputField createdMatchNameInput;
    private string createdMatchNameString;
    public Text createMatchPanelErrorText;
    public Slider maxHealthSlider;
    public Text maxHealthText;

    //GameObject Match List Panel
    [Header("Match List Panel")]
    public Transform matchListPanel;
    public Transform matchListScrollContent;
    public InputField matchNameInput;
    public Text opponentName;
    public Text maxHealth;

    public GameObject buttonPrefab;

    //GameMatch Variables
    private string matchName;
    private byte[] matchID;
    private bool updateMatchName = false;
    private bool opponentQuit = false;

    private int opCode;
    private string dataValue;
    private bool dataRecieved = false;    //To get out of the Nakama OnMatchData thread

    //TEMP DEBUGGIN
    //private byte[] testByte = { 1, 2, 3 };

    // Use this for initialization
    void Start ()
    {
        Singleton = this;
        matchListPanel.gameObject.SetActive(false);
        createMatchPanel.gameObject.SetActive(false);
        opponentName.text = "";

        //Fix when Match System comes out. For now, remove previous games this player created and may have disconnected
        DestroyLastMatchCreated();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (updateMatchName)
        {
            matchNameInput.text = matchName;
            updateMatchName = false;
        }

        if (opponentQuit)
        {
            GameManager.Singleton.OpponentQuit();
            opponentQuit = false;
        }
            

        if (dataRecieved)
        {
            ReadSentData(opCode, dataValue);
            dataRecieved = false;
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
        SendMatchInfoToMatchRoom("add", Convert.ToInt32(maxHealthSlider.value));
        PlayerPrefs.SetString("MatchCreated", new Guid(matchID).ToString());
        SendMessages.Singleton.LeaveRoom();
        SendMessages.Singleton.JoinRoom(matchName);
        GameManager.Singleton.StartNewGamePlay(createdMatchNameInput.text, Convert.ToInt32(maxHealthSlider.value));
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

    private void GetPreviousMatchesFromRoom()
    {
        ManualResetEvent historyEvent = new ManualResetEvent(false);

        client = NakamaData.Singleton.Client;
        byte[] roomByte = Encoding.UTF8.GetBytes("match-list");
        IList<INTopicMessage> msgsReturned = null;

        var message = new NTopicMessagesListMessage.Builder()
            .TopicRoom(roomByte)
            .Forward(false)
            .Limit(100)
            .Build();

        client.Send(message, (INResultSet<INTopicMessage> msgs) =>
        {
            // Each message in the result set is a `INTopicMessage` identical
            // to messages received through `OnTopicMessage` realtime delivery.
            msgsReturned = msgs.Results;
            Debug.Log("Successfully listed messages from topic. msgs.Results.Count: " + msgs.Results.Count + " msgsReturned.Count: " + msgsReturned.Count);
            historyEvent.Set();
        }, (INError error) => {
            Debug.LogErrorFormat("Could not list messages from topic: '{0}'.", error.Message);
            historyEvent.Set();
        });
        historyEvent.WaitOne(1000, false);

        if (msgsReturned.Count < 1)
            return;

        //GO through the messages as if they were just recieved
        foreach (INTopicMessage topicMessage in msgsReturned)
        {

            Debug.Log("topicMessage.Data.ToString(): " + topicMessage.Data.ToString());
            var bytesAsString = Encoding.ASCII.GetString(topicMessage.Data);
            var chatJson = JsonUtility.FromJson<MatchRoomClass>(bytesAsString);
            Guid tempMatchID = new Guid(chatJson.matchIDGUID);

            MatchSettings newSettings = new MatchSettings();
            newSettings.matchCreator = chatJson.userName;
            newSettings.maxHealth = chatJson.matchMaxHealth;
            newSettings.matchName = chatJson.matchName;

            if (chatJson.addRemove == "add")
            {
                Debug.Log("Adding previous match: " + chatJson.matchName);
                newSettings.matchStatus = "open";
                //matchNameMatchGuid.Add(chatJson.matchName, tempMatchID.ToByteArray());
                if (matchGuidMatchSettings.ContainsKey(tempMatchID))
                    return;
                matchGuidMatchSettings.Add(tempMatchID, newSettings);
                Debug.Log("Added matchGuidMatchSettings.Count: " + matchGuidMatchSettings.Count); //match matchNameMatchGuid.Count: " + matchNameMatchGuid.Count + " matchGuidMatchSettings.Count: " + matchGuidMatchSettings.Count);
            }
            else
            {
                Debug.Log("Removing matchGuidMatchSettings.Count: " + matchGuidMatchSettings.Count); //match matchNameMatchGuid.Count: " + matchNameMatchGuid.Count + " matchGuidMatchSettings.Count: " + matchGuidMatchSettings.Count);
                //matchNameMatchGuid.Remove(chatJson.matchName);
                //matchGuidMatchSettings.Remove(chatJson.matchName);
                if (matchGuidMatchSettings.ContainsKey(tempMatchID))
                {
                    matchGuidMatchSettings[tempMatchID].matchStatus = "closed";
                }
                else
                {
                    newSettings.matchStatus = "closed";
                    matchGuidMatchSettings.Add(tempMatchID, newSettings);  //Only reason we add, is so if it comes up again in message
                }
                Debug.Log("Removed matchGuidMatchSettings.Count: " + matchGuidMatchSettings.Count); //match matchNameMatchGuid.Count: " + matchNameMatchGuid.Count + " matchGuidMatchSettings.Count: " + matchGuidMatchSettings.Count);
            }
            
        }
        
    }

    private void SendMatchInfoToMatchRoom(string addRemove, int maxHealth)
    {
        client = NakamaData.Singleton.Client;
        ManualResetEvent sendMessage = new ManualResetEvent(false);
        Guid matchID_Guid = new Guid(matchID);

        //Debug.LogWarning("Encoding.UTF8.GetString(matchID): " + Encoding.UTF8.GetString(matchID));
        //Guid test = new Guid(matchID);

        string chatMessage = "{\"addRemove\":\"" + addRemove + "\",\"matchName\":\"" + matchName + "\",\"userName\":\"" + NakamaData.Singleton.ClientUserName + "\",\"matchIDGUID\":\"" + matchID_Guid.ToString() + "\",\"matchMaxHealth\":\"" + maxHealth + "\"}";
        //Debug.LogWarning("DEBUG:::: chatJson.matchIDGUID: " + test + " chatJson.matchIDGUID..ToByteArray(): " + test.ToByteArray() + " Encoding.UTF8.GetString(matchID)" + Encoding.UTF8.GetString(matchID));
        //Debug.LogWarning("DEBUG:::: Encoding.UTF8.GetString(test.ToByteArray()): " + Encoding.UTF8.GetString(test.ToByteArray()) + " Encoding.UTF8.GetString(matchID)" + Encoding.UTF8.GetString(matchID));
        NTopicMessageSendMessage msg = NTopicMessageSendMessage.Default(matchListTopic, Encoding.UTF8.GetBytes(chatMessage));
        client.Send(msg, (INTopicMessageAck ack) =>
        {
            Debug.Log("Match Room Data being sent: " + chatMessage);
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
        //First clear any matches listed, in case this fool clicks the "Join Match" when window is already open!! 
        foreach (Transform child in matchListScrollContent)
        {
            GameObject.Destroy(child.gameObject);
        }

        matchNameMatchGuid.Clear();
        GetPreviousMatchesFromRoom();  //Try to fetch any previous matches from before joined
        //Add Buttons of Current matches
        foreach (var pair in matchGuidMatchSettings)
        {            
            if (pair.Value.matchStatus == "open")
            {
                matchNameMatchGuid.Add(pair.Value.matchName, pair.Key);
                GameObject newButton = Instantiate(buttonPrefab);
                newButton.transform.SetParent(matchListScrollContent);
                newButton.transform.name = pair.Value.matchName;
                newButton.transform.GetComponentInChildren<Text>().text = pair.Value.matchName;
            }            
        }

        matchListPanel.gameObject.SetActive(true);

    }

    //Called when "Join Match" is clicked in the Match List Panel
    public void JoinMatch()
    {
        Debug.Log("Joining the match: " + matchName);
        RegisterOnMatchData();

        ManualResetEvent joinEvent = new ManualResetEvent(false);
        matchID = matchNameMatchGuid[matchName].ToByteArray();
        string opponentName = matchGuidMatchSettings[matchNameMatchGuid[matchName]].matchCreator;
        string maxHealth = matchGuidMatchSettings[matchNameMatchGuid[matchName]].maxHealth;
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
        SendMatchInfoToMatchRoom("remove", 0);
        SendMessages.Singleton.LeaveRoom();
        SendMessages.Singleton.JoinRoom(matchName);
        GameManager.Singleton.StartNewGamePlay(matchName, Convert.ToInt32(maxHealth), opponentName);
        matchListPanel.gameObject.SetActive(false);
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
        SendMatchInfoToMatchRoom("remove", 0);
        UnRegisterOnMatchData();
        SendMessages.Singleton.LeaveRoom();
        SendMessages.Singleton.JoinRoom("default-room");
        GameManager.Singleton.QuitCurrentGameMatch(NakamaData.Singleton.ClientUserName + " left the match");
    }

    //Called when a Match listed in the MatchListScroll is pressed
    public void MatchListButtonPressed()
    {
        matchName = EventSystem.current.currentSelectedGameObject.name;
        matchID = matchNameMatchGuid[matchName].ToByteArray();
        opponentName.text = matchGuidMatchSettings[matchNameMatchGuid[matchName]].matchCreator;
        maxHealth.text = matchGuidMatchSettings[matchNameMatchGuid[matchName]].maxHealth;
        updateMatchName = true;
        Debug.Log("Button clicked! " + matchName);
    }

    private void DestroyLastMatchCreated()
    {
        Debug.Log("DestroyLastMatchCreated():: PlayerPrefs.GetString(\"MatchCreated\"): " + PlayerPrefs.GetString("MatchCreated"));
        if(PlayerPrefs.GetString("MatchCreated").Length > 0)
        {
            Guid tempMatchID = new Guid(PlayerPrefs.GetString("MatchCreated"));
            matchID = tempMatchID.ToByteArray();
            SendMatchInfoToMatchRoom("remove", 0);
            PlayerPrefs.SetString("MatchCreated", "");
        }
        

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
        opCode = Convert.ToInt32(args.Data.OpCode);
        dataValue = Encoding.ASCII.GetString(args.Data.Data);
        Debug.Log("Retrieved Match Data:: opCode: " + opCode + " data: " + dataValue);

        dataRecieved = true;
    }

    void c_OnMatchPresence(object source, NMatchPresenceEventArgs args)
    {
        if(args.MatchPresence.Leave.Count > 0)
        {
            Debug.Log("Opponent Quit the match!");
            opponentQuit = true;
        }
    }

    void MatchList_OnTopicMessage(object sender, NTopicMessageEventArgs e)
    {
        ManualResetEvent updateEvent = new ManualResetEvent(false);

        var bytesAsString = Encoding.ASCII.GetString(e.Message.Data);
        var chatJson = JsonUtility.FromJson<MatchRoomClass>(bytesAsString);
        Guid tempMatchID = new Guid(chatJson.matchIDGUID);

        MatchSettings newSettings = new MatchSettings();
        newSettings.matchCreator = chatJson.userName;
        newSettings.maxHealth = chatJson.matchMaxHealth;
        //newSettings.matchGuid = new Guid(chatJson.matchIDGUID);


        if (chatJson.addRemove == "add")
        {
            Debug.Log("Adding match");
            newSettings.matchStatus = "open";
            //matchNameMatchGuid.Add(chatJson.matchName, tempMatchID.ToByteArray());
            matchGuidMatchSettings.Add(tempMatchID, newSettings);
            Debug.Log("Added matchGuidMatchSettings.Count: " + matchGuidMatchSettings.Count); //match matchNameMatchGuid.Count: " + matchNameMatchGuid.Count + " matchGuidMatchSettings.Count: " + matchGuidMatchSettings.Count);
            updateEvent.Set();
        } else
        {
            Debug.Log("Removing matchGuidMatchSettings.Count: " + matchGuidMatchSettings.Count); //match matchNameMatchGuid.Count: " + matchNameMatchGuid.Count + " matchGuidMatchSettings.Count: " + matchGuidMatchSettings.Count);
            //matchNameMatchGuid.Remove(chatJson.matchName);
            matchGuidMatchSettings[tempMatchID].matchStatus = "closed";
            //matchGuidMatchSettings.Remove(chatJson.matchName);
            Debug.Log("Removed matchGuidMatchSettings.Count: " + matchGuidMatchSettings.Count); //match matchNameMatchGuid.Count: " + matchNameMatchGuid.Count + " matchGuidMatchSettings.Count: " + matchGuidMatchSettings.Count);
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
        //client.OnTopicMessage += MatchList_OnTopicMessage;
    }

    public void UnRegisterMatchListRoom()
    {
        client = NakamaData.Singleton.Client;
        //client.OnTopicMessage -= MatchList_OnTopicMessage;
    }

    /// <summary>
    /// opCodes:
    /// 0 Joined
    /// 1 Player 1 turn
    /// 2 Player 2 turn
    /// 3 Card Drawn(by whoever sent this)
    /// 4 Attack(by whoever sent this)
    /// 5 current turn
    /// </summary>
    public void SendMatchData(int opCode, string dataString)
    {
        Debug.Log("Sending Match Data:: opCode: " + opCode + " datastring: " + dataString);
        byte[] data = Encoding.ASCII.GetBytes(dataString);
        var message = NMatchDataSendMessage.Default(matchID, opCode, data);
        client.Send(message, (bool complete) =>
        {
            Debug.Log("Successfully sent data to match.");
        }, (INError error) => {
            Debug.LogErrorFormat("Could not send data to match: '{0}'.", error.Message);
        });
    }
    private void ReadSentData(int opCodeRetrieved, string dataRetrieved)
    {
        Debug.Log("Reading Recieved Match Data:: :: opCodeRetrieved: " + opCodeRetrieved + " dataRetrieved: " + dataRetrieved);
        switch (opCodeRetrieved)
        {
            case 0:
                GameManager.Singleton.OpponentJoined(dataRetrieved);
                break;
            case 1:
                GameManager.Singleton.NewTurn(1);
                break;
            case 2:
                GameManager.Singleton.NewTurn(2);
                break;
            case 3:
                int totalDrawn = Convert.ToInt32(dataRecieved);
                GameManager.Singleton.OpponentDrawCard();
                break;
            case 4:
                string[] values = dataRetrieved.Split(':'); //Split data which will be "<cardNum>:<suiteName>"
                GameManager.Singleton.OpponentAttack(Convert.ToInt32(values[0]), Convert.ToInt32(values[1]));
                break;
            default:
                Debug.Log("OpCode out of range: " + opCodeRetrieved);
                break;
        }
    }

    public void ChangeMaxHealthText()
    {
        maxHealthText.text = maxHealthSlider.value.ToString();
    }
}
