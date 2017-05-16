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

        [Header("Gameplay Panel")]
        public Transform GamePlayPanel;
        public Transform InfoFlashPanel;
        private CanvasGroup InfoFlashCG;

        [Header("Opponent Objects")]
        public Text opponentNameText;
        public Slider opponentHealthSlider;
        public Text opponentHealthText;

        [Header("Player Objects")]
        public Text playerNameText;
        public Slider playerHealthSlider;
        public Text playerHealthText;

        private string _playerName;
        private string _opponentName;

        private int _playerHealth;
        public int PlayerHealth
        {
            set   { _playerHealth = value; }
            get { return _playerHealth; }
        }

        private int _opponentHealth;
        public int OpponentHealth
        {
            set
            { _opponentHealth = value; }
            get { return _opponentHealth; }
        }

        // Use this for initialization
        void Start()
        {
            Singleton = this;

            InfoFlashCG = InfoFlashPanel.GetComponentInChildren<CanvasGroup>();

            GamePlayPanel.gameObject.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {

            playerHealthSlider.value = _playerHealth;
            playerHealthText.text = _playerHealth.ToString();


            opponentHealthSlider.value = _opponentHealth;
            opponentHealthText.text = _opponentHealth.ToString();

        }

        public void UpdateOpponent(string opponent)
        {
            _opponentName = opponent;
            opponentNameText.text = _opponentName;
        }

        //public void ToggleGamePlayPanel(bool showPanel)
        //{
        //    GamePlayPanel.gameObject.SetActive(showPanel);
        //}

        //When a new game is created or joined
        public void StartNewGamePlay(string gameName, string opponent = "")
        {
            GamePlayPanel.gameObject.SetActive(true);
            _playerName = NakamaData.Singleton.ClientUserName;

            playerNameText.text = _playerName;
            _playerHealth = 15;
            _opponentHealth = 15;

            if (opponent == "")
            {
                opponentNameText.text = "";
                InfoFlashPanel.GetComponentInChildren<Text>().text = "You Have created a new game: " + gameName;
            } else
            {
                _opponentName = opponent;
                opponentNameText.text = _opponentName;
                InfoFlashPanel.GetComponentInChildren<Text>().text = "You Have joined a new game: " + gameName;
            }

            
            StartCoroutine(ShowInfoFlashPanel());
        }

        private IEnumerator ShowInfoFlashPanel()
        {
            InfoFlashCG.alpha = 2f;

            yield return new WaitForSeconds(1f);

            while(InfoFlashCG.alpha > 0)
            {
                InfoFlashCG.alpha -= 0.05f;
                yield return new WaitForSeconds(0.05f);
            }
        }

    }
}


