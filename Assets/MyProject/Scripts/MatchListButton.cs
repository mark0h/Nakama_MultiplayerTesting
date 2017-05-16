using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple Script when the generated button for match lists is pushed. 
/// Had to do it this way, as the GameManager object assigned to the "OnCLick" of the button, was not carrying over when starting a new game(dif objects)
/// </summary>
public class MatchListButton : MonoBehaviour {

	public void OnMatchSelect()
    {
        MatchController.Singleton.MatchListButtonPressed();
    }
}
