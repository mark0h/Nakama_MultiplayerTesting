using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimizeWindow : MonoBehaviour {

    public Transform mainPanel;
    private bool isVisible = true;

	public void HideorShowPanel()
    {
        if (isVisible)
        {
            mainPanel.gameObject.SetActive(false);
            isVisible = false;
        }
        else
        {
            mainPanel.gameObject.SetActive(true);
            isVisible = true;
        }            
    }
}
