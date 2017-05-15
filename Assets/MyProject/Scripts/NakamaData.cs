using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;

public class NakamaData : MonoBehaviour {

    public static NakamaData Singleton;

    private INClient _client;
    public INClient Client
    {
        set
        {
            _client = value;
        }
        get { return _client; }
    }

    private INSession _session;
    public INSession Session
    {
        set
        {
            _session = value;
        }
        get { return _session; }
    }

    private byte[] _clientID = null;
    public byte[] ClientID
    {
        set
        {
            _clientID = value;
        }
        get { return _clientID; }
    }

    private string _deviceID;
    public string DeviceID
    {
        set
        {
            _deviceID = value;
        }
        get { return _deviceID; }
    }

    private string _clientUserName;
    public string ClientUserName
    {
        set
        {
            _clientUserName = value;
        }
        get { return _clientUserName; }
    }

    void Start()
    {
        Singleton = this;
    }
}
