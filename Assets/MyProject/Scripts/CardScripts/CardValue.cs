using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CardValue  {

    public CardConstants.Suite suite;
    public CardConstants.Card card;

    public CardValue(CardConstants.Card cardValue, CardConstants.Suite suiteValue)
    {
        suite = suiteValue;
        card = cardValue;
    }

	
}
