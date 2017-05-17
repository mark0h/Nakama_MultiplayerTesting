using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//SHUFFLING CLASS
public static class Shuffling
{
    //SHUFFLER METHOD 
    private static System.Random rng = new System.Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}

public class Deck : MonoBehaviour {

    public List<CardValue> deckCards = new List<CardValue>();
    public List<CardValue> inPlayCards = new List<CardValue>();
    public List<CardValue> playedCards = new List<CardValue>();

	// Create a new deck adding all 52 cards from CardConstants in it
	void Start ()
    {
		foreach(var card in CardConstants.Cards)
        {
            foreach(var suite in CardConstants.Suites)
            {
                deckCards.Add(new CardValue(card, suite));
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ShuffleCards()
    {
        deckCards.Shuffle();
    }

    public CardValue DrawCard()
    {
        int cardSelect = deckCards.Count - 1;

        CardValue cardSelected = deckCards[cardSelect];
        deckCards.RemoveAt(cardSelect);
        inPlayCards.Add(cardSelected);

        return cardSelected;
    }

    public void DiscardPlayedCard(int cardIndex)
    {
        CardValue cardDiscarded = inPlayCards[cardIndex];
        inPlayCards.RemoveAt(cardIndex);
        playedCards.Add(cardDiscarded);
    }

    public void ReturnCardsToDeck()
    {
        //First Discard all cards in play
        foreach(var card in inPlayCards)
            playedCards.Add(card);
        inPlayCards.Clear();

        //Then return all discarded cards to the deck
        foreach (var card in playedCards)
            deckCards.Add(card);
        playedCards.Clear();

        Debug.Log("deckCards.Count: " + deckCards.Count + " inPlayCards.Count: " + inPlayCards.Count + " playedCards.Count: " + playedCards.Count);

    }

    
}
