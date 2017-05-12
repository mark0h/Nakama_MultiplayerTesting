using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MGR.Creations
{
    public class GameManager : MonoBehaviour
    {

        public static GameManager Singleton;

        public Transform LoginPanel;
        public Transform ChatPanel;
        public Transform UserListPanel;

        // Use this for initialization
        void Start()
        {
            Singleton = this;
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
        }
    }
}


