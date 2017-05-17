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
        public Transform BlockActionPanel;
        private CanvasGroup InfoFlashCG;

        [Header("GameOVer Panel")]
        public Transform EndGameResultsPanel;
        public Transform EndGamePlayerPanel;
        public Transform EndGameOppNamePanel;

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
            EndGameResultsPanel.gameObject.SetActive(false);
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
            InfoFlashPanel.gameObject.SetActive(true);
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
            BlockActionPanel.gameObject.SetActive(false);
        }

        public void QuitCurrentGameMatch(string endGameText)
        {
            EndGameResultsPanel.gameObject.SetActive(true);
            BlockActionPanel.gameObject.SetActive(true);

            Text endGameInfo = EndGameResultsPanel.FindChild("EndGameInfoText").GetComponent<Text>();
            Text playerNameText = EndGamePlayerPanel.FindChild("PlayerNameText").GetComponent<Text>();
            Text playerScore = EndGamePlayerPanel.FindChild("PlayerScore").GetComponent<Text>();
            Text playerDamage = EndGamePlayerPanel.FindChild("PlayerDamage").GetComponent<Text>();
            Text playerHealth = EndGamePlayerPanel.FindChild("PlayerHealth").GetComponent<Text>();

            endGameInfo.text = endGameText;
            playerNameText.text = NakamaData.Singleton.ClientUserName;
            playerScore.text = "144";
        }

        public void UpdateInfoFlashPanelMessage(string message)
        {
            InfoFlashPanel.GetComponentInChildren<Text>().text = message;
            StartCoroutine(ShowInfoFlashPanel());
        }

        private IEnumerator ShowInfoFlashPanel()
        {
            InfoFlashCG.alpha = 1f;

            yield return new WaitForSeconds(2f);

            while(InfoFlashCG.alpha > 0)
            {
                InfoFlashCG.alpha -= 0.05f;
                yield return new WaitForSeconds(0.05f);
            }
        }

        public void CloseEndGameResultsPanel()
        {
            EndGameResultsPanel.gameObject.SetActive(false);
            GamePlayPanel.gameObject.SetActive(false);
        }

    }
}


