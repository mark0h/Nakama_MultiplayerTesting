using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GameState
{
    PlayerTurn,
    OpponentTurn,
    WaitingForPlayers,
}

namespace MGR.Creations
{
    public class GameManager : MonoBehaviour
    {

        public static GameManager Singleton;

        public Transform LoginPanel;
        public Transform ChatPanel;
        public Transform UserListPanel;
        public Transform MenuPanel;

        // Use this for initialization
        void Start()
        {
            Singleton = this;
            LoginPanel.gameObject.SetActive(true);
            ChatPanel.gameObject.SetActive(false);
            UserListPanel.gameObject.SetActive(false);
            MenuPanel.gameObject.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void HideLoginPanel()
        {
            LoginPanel.gameObject.SetActive(false);
        }

        public void ShowChatAndUserList()
        {
            ChatPanel.gameObject.SetActive(true);
            UserListPanel.gameObject.SetActive(true);
            MenuPanel.gameObject.SetActive(true);
        }
    }
}


