using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameData : MonoBehaviour {

    public static GameData Singleton;

    private int _playerHealth;
    public int PlayerHealth
    {
        set { _playerHealth = value; }
        get { return _playerHealth; }
    }

    private int _opponentHealth;
    public int OpponentHealth
    {
        set { _opponentHealth = value; }
        get { return _opponentHealth; }
    }


    private int _playerScore;
    public int PlayerScore
    {
        set { _playerScore = value; }
        get { return _playerScore; }
    }
    private int _oppScore;
    public int OpponentScore
    {
        set { _oppScore = value; }
        get { return _oppScore; }
    }

    private int _playerDamage;
    public int PlayerDamage
    {
        set { _playerDamage = value; }
        get { return _playerDamage; }
    }
    private int _oppDamage;
    public int OpponentDamage
    {
        set { _oppDamage = value; }
        get { return _oppDamage; }
    }

    private int _playerDamageTaken;
    public int PlayerDamageTaken
    {
        set { _playerDamageTaken = value; }
        get { return _playerDamageTaken; }
    }
    private int _oppDamageTaken;
    public int OpponentDamageTaken
    {
        set { _oppDamageTaken = value; }
        get { return _oppDamageTaken; }
    }


    // Use this for initialization
    void Start ()
    {
        Singleton = this;
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
