using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

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
        public Transform cardSlotPrefab;
        public Button quitGameButton;

        [Header("GameOver Panel")]
        public Transform EndGameResultsPanel;
        public Transform EndGamePlayerPanel;
        public Transform EndGameOppPanel;

        [Header("Opponent Objects")]
        public Text opponentNameText;
        public Slider opponentHealthSlider;
        public Text opponentHealthText;
        public Transform oppCardsPanel;
        public Transform oppDamageIcon;
        private CanvasGroup oppDamageCG;

        [Header("Player Objects")]
        public Text playerNameText;
        public Slider playerHealthSlider;
        public Text playerHealthText;
        public Transform playerCardsPanel;
        public Transform playerDamageIcon;
        private CanvasGroup playerDamageCG;


        //Gameplay variables
        private int currentRound = 1;
        private string opponentName;
        private int playerNumber;              //1 for match creator, 2 for match joiner(for turn rotation)

        private bool gameTesting = false;

        

        // Use this for initialization
        void Start()
        {
            Singleton = this;


            InfoFlashCG = InfoFlashPanel.GetComponentInChildren<CanvasGroup>();
            oppDamageCG = oppDamageIcon.GetComponentInChildren<CanvasGroup>();
            playerDamageCG = playerDamageIcon.GetComponentInChildren<CanvasGroup>();

            GamePlayPanel.gameObject.SetActive(false);
            EndGameResultsPanel.gameObject.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {

            playerHealthSlider.value = GameData.Singleton.PlayerHealth;
            playerHealthText.text = GameData.Singleton.PlayerHealth.ToString();


            opponentHealthSlider.value = GameData.Singleton.OpponentHealth;
            opponentHealthText.text = GameData.Singleton.OpponentHealth.ToString();

        }

        public void OpponentJoined(string opponent)
        {
            opponentName = opponent;
            opponentNameText.text = opponentName;

            //Determine who goes first
            int firstTurn = Random.Range(1, 3);
            if(firstTurn == 1)
            {
                //This player will start
            } else
            {
                //Opponent Starts first turn
            }

        }

        public void NewTurn(int whoseTurn)
        {
            //Is it this player's turn?
            if(playerNumber == whoseTurn)
            {

            } else
            {

            }
        }

        private void ToggleGamePlay(bool playable)
        {
            BlockActionPanel.gameObject.SetActive(playable);
        }

        //When a new game is created or joined
        public void StartNewGamePlay(string gameName, int startingHealth, string opponent = "")
        {
            GamePlayPanel.gameObject.SetActive(true);
            quitGameButton.interactable = true;

            Debug.Log("Starting a NEW GAME PLAY");

            //SETUP Everything in the GameStatusPanel
            opponentHealthSlider.maxValue = startingHealth;
            playerHealthSlider.maxValue = startingHealth;
            playerNameText.text = NakamaData.Singleton.ClientUserName;
            GameData.Singleton.PlayerHealth = startingHealth;
            GameData.Singleton.OpponentHealth = startingHealth;
            opponentNameText.text = opponent;

            if (opponent == "")
            {
                InfoFlashPanel.GetComponentInChildren<Text>().text = "You Have created a new game: " + gameName;
                playerNumber = 1;   //Match creator always player 1
            }
            else
            {
                InfoFlashPanel.GetComponentInChildren<Text>().text = "You Have joined a new game: " + gameName;
                playerNumber = 2;   //Match joiner always player 2
            }

             StartCoroutine(ShowInfoFlashPanel());

            //NEW GAME SETUP
            //Remove all previous cards
            foreach (Transform child in playerCardsPanel)
                GameObject.Destroy(child.gameObject);
            foreach (Transform child in oppCardsPanel)
                GameObject.Destroy(child.gameObject);

            Deck.Singleton.ReturnCardsToDeck();
            Deck.Singleton.ShuffleCards();


            ToggleGamePlay(false);
        }

        public void QuitCurrentGameMatch(string endGameText)
        {
            EndGameResultsPanel.gameObject.SetActive(true);
            ToggleGamePlay(true);
            quitGameButton.interactable = false;

            Text endGameInfo = EndGameResultsPanel.FindChild("EndGameInfoText").GetComponent<Text>();
            Text playerNameText = EndGamePlayerPanel.FindChild("PlayerNameText").GetComponent<Text>();
            Text playerScore = EndGamePlayerPanel.FindChild("PlayerScore").GetComponent<Text>();
            Text playerDamage = EndGamePlayerPanel.FindChild("PlayerDamage").GetComponent<Text>();
            Text playerHealth = EndGamePlayerPanel.FindChild("PlayerHealth").GetComponent<Text>();

            Text opponentNameText = EndGameOppPanel.FindChild("OpponentNameText").GetComponent<Text>();
            Text opponentScore = EndGameOppPanel.FindChild("OpponentScore").GetComponent<Text>();
            Text opponentDamage = EndGameOppPanel.FindChild("OpponentDamage").GetComponent<Text>();
            Text opponentHealth = EndGameOppPanel.FindChild("OpponentHealth").GetComponent<Text>();

            endGameInfo.text = endGameText;
            playerNameText.text = NakamaData.Singleton.ClientUserName;
            playerScore.text = GameData.Singleton.PlayerScore.ToString();
            opponentScore.text = GameData.Singleton.OpponentScore.ToString();
        }

        public void UpdateInfoFlashPanelMessage(string message)
        {
            InfoFlashPanel.GetComponentInChildren<Text>().text = message;
            StartCoroutine(ShowInfoFlashPanel());
        }

        public void CloseEndGameResultsPanel()
        {
            EndGameResultsPanel.gameObject.SetActive(false);
            ToggleGamePlay(false);
            GamePlayPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// GAMEPLAY METHODS!
        /// </summary>
        public void DrawCard()
        {
            if(Deck.Singleton.inPlayCards.Count > 4)
            {
                return;
            }

            CardValue drawnCard = Deck.Singleton.DrawCard();
            Transform cardTransform = Instantiate(cardSlotPrefab);
            Image cardImageTransform = cardTransform.FindChild("CardImage").GetComponent<Image>();

            int cardValue = (int)drawnCard.card;
            string cardSuite = drawnCard.suite.ToString().ToLower();
            string cardSpriteName = "PlayingCards/" + cardValue + "_of_" + cardSuite;
            
            cardImageTransform.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(cardSpriteName);
            cardTransform.SetParent(playerCardsPanel);
        }
        public void AttackwithCard(DragAndDropCell cardSlot, DragAndDropItem draggedCardImage)
        {
            //Not sure why I have to re-apply the image??
            Image cardImage = draggedCardImage.GetComponentInChildren<Image>();
            string spriteName = cardImage.sprite.name;
            draggedCardImage.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>("PlayingCards/" + spriteName);

            int cardNumber = Convert.ToInt32(Regex.Replace(spriteName, "_.*", ""));
            int suiteNumber = GetSuiteNumber(Regex.Replace(spriteName, ".*_", ""));
            CardConstants.Card card = (CardConstants.Card)cardNumber;
            CardConstants.Suite suite = (CardConstants.Suite)suiteNumber;
            CardValue cardValue = new CardValue(card, suite);

            Debug.Log("CardNumber: " + cardNumber + " SuiteNumber: " + suiteNumber);

            GameObject.Destroy(cardSlot.gameObject);
            Deck.Singleton.DiscardPlayedCard(cardValue);

            StartCoroutine(ShowDamage(oppDamageCG));
            GameData.Singleton.OpponentHealth -= cardNumber;
            GameData.Singleton.PlayerScore += (cardNumber * 10) / currentRound;
            if(GameData.Singleton.OpponentHealth < 1)
            {
                OpponentDied();
            }
        }

        public void OpponentDrawCard()
        {
            Transform cardTransform = Instantiate(cardSlotPrefab);
            Image cardImageTransform = cardTransform.FindChild("CardImage").GetComponent<Image>();
            cardImageTransform.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>("PlayingCards/cardBack_blue");
            cardTransform.SetParent(oppCardsPanel);
        }

        public void OpponentAttack(int attackCard, string attackSuite)
        {
            int suiteNumber = GetSuiteNumber(attackSuite);
            CardConstants.Card card = (CardConstants.Card)attackCard;
            CardConstants.Suite suite = (CardConstants.Suite)suiteNumber;
            CardValue cardValue = new CardValue(card, suite);

            StartCoroutine(ShowDamage(playerDamageCG));
            GameData.Singleton.PlayerHealth -= attackCard;
            GameData.Singleton.OpponentScore += (attackCard * 10) / currentRound;
            if (GameData.Singleton.PlayerHealth < 1)
            {
                PlayerDied();
            }

            //After attack, you can now play
            ToggleGamePlay(true);
        }

        public void PlayerDied()
        {
            GameData.Singleton.OpponentScore += 100;
            QuitCurrentGameMatch(NakamaData.Singleton.ClientUserName + " has won the match!");
            //THIS WILL ALSO SEND A MESSAGE TO OPPONENET ABOUT THIS
        }

        public void OpponentDied()
        {
            GameData.Singleton.PlayerScore += 100;
            QuitCurrentGameMatch(NakamaData.Singleton.ClientUserName + " has won the match!");
        }

        public void GamePlayTesting()
        {
            GamePlayPanel.gameObject.SetActive(true);
            gameTesting = true;
            NakamaData.Singleton.ClientUserName = "TESTING";
            StartNewGamePlay("TESTING GAME",45, "");
        }

        private IEnumerator ShowInfoFlashPanel()
        {
            InfoFlashPanel.gameObject.SetActive(true);
            InfoFlashCG.alpha = 1f;

            yield return new WaitForSeconds(2f);

            while (InfoFlashCG.alpha > 0)
            {
                InfoFlashCG.alpha -= 0.05f;
                yield return new WaitForSeconds(0.05f);
            }
            InfoFlashPanel.gameObject.SetActive(false);
        }

        private IEnumerator ShowDamage(CanvasGroup playerDamaged)
        {
            playerDamaged.alpha = 1f;

            yield return new WaitForSeconds(2f);

            while (playerDamaged.alpha > 0)
            {
                playerDamaged.alpha -= 0.05f;
                yield return new WaitForSeconds(0.05f);
            }
        }

        private int GetSuiteNumber(string suite)
        {
            if (suite == "clubs")
                return 1;
            else if (suite == "spades")
                return 2;
            else if (suite == "hearts")
                return 3;
            else
                return 4;
        }

    }
}


