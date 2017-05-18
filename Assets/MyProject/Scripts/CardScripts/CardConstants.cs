using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardConstants : MonoBehaviour {

    //(int)Card.Ace   will return 11
    //int value = 11;
    //Card card = (Card)value;  will return Card.Ace
    public enum Card
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13
    };

    public enum Suite
    {
        Clubs = 1,
        Spades = 2,
        Hearts = 3,
        Diamonds = 4
    }

    public static readonly Card[] Cards =
    {
        Card.Ace,
        Card.Two,
        Card.Three,
        Card.Four,
        Card.Five,
        Card.Six,
        Card.Seven,
        Card.Eight,
        Card.Nine,
        Card.Ten,
        Card.Jack,
        Card.Queen,
        Card.King
    };

    public static readonly Suite[] Suites =
    {
        Suite.Clubs,
        Suite.Spades,
        Suite.Hearts,
        Suite.Diamonds
    };

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
