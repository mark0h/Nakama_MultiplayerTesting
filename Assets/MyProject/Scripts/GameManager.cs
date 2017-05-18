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
        public Transform defendPanelCards;

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

        [Header("Misc Objects")]
        public Text whoseTurnText;
        public Text currentRoundText;


        //Gameplay variables
        private int currentRound = 1;
        private int playersPlayedCount = 0;
        private string opponentName;
        private int playerNumber;              //1 for match creator, 2 for match joiner(for turn rotation)
        private int oppNumber;                 //1 for match creator, 2 for match joiner(for turn rotation)
        private int firstTurn;
        //private int cardsDrawn = 0;
        //private int oppCardsDrawn = 0;

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
            firstTurn = Random.Range(1, 3);
        }

        // Update is called once per frame
        void Update()
        {

            playerHealthSlider.value = GameData.Singleton.PlayerHealth;
            playerHealthText.text = GameData.Singleton.PlayerHealth.ToString();


            opponentHealthSlider.value = GameData.Singleton.OpponentHealth;
            opponentHealthText.text = GameData.Singleton.OpponentHealth.ToString();

        }

        private void ToggleBlockPanel(bool blockOn)
        {
            Debug.Log("ToggleGamePlay():: :: Setting BlockPanel to : " + blockOn);
            BlockActionPanel.gameObject.SetActive(blockOn);
        }

        //When a new game is created or joined
        public void StartNewGamePlay(string gameName, int startingHealth, string opponent = "")
        {
            GamePlayPanel.gameObject.SetActive(true);
            quitGameButton.interactable = true;
            ToggleBlockPanel(true);

            if(gameTesting)
                ToggleBlockPanel(false);

            Debug.Log("Starting a NEW GAME PLAY.");

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
                oppNumber = 2;
                whoseTurnText.text = "Waiting for player...";
            }
            else
            {
                InfoFlashPanel.GetComponentInChildren<Text>().text = "You Have joined a new game: " + gameName;
                playerNumber = 2;   //Match joiner always player 2
                oppNumber = 1;
                opponentName = opponent;
                MatchController.Singleton.SendMatchData(0, NakamaData.Singleton.ClientUserName);
            }

            Debug.Log("StartNewGamePlay():: :: You are player: " + playerNumber);

             //StartCoroutine(ShowInfoFlashPanel());

            //NEW GAME SETUP
            //Remove all previous cards
            foreach (Transform child in playerCardsPanel)
                GameObject.Destroy(child.gameObject);
            foreach (Transform child in oppCardsPanel)
                GameObject.Destroy(child.gameObject);

            Deck.Singleton.ReturnCardsToDeck();
            Deck.Singleton.ShuffleCards();
            
        }

        public void QuitCurrentGameMatch(string endGameText)
        {
            EndGameResultsPanel.gameObject.SetActive(true);
            ToggleBlockPanel(true);
            quitGameButton.interactable = false;

            Text endGameInfo = EndGameResultsPanel.Find("EndGameInfoText").GetComponent<Text>();
            Text playerNameText = EndGamePlayerPanel.Find("PlayerNameText").GetComponent<Text>();
            Text playerScore = EndGamePlayerPanel.Find("PlayerScore").GetComponent<Text>();
            Text playerDamage = EndGamePlayerPanel.Find("PlayerDamage").GetComponent<Text>();
            Text playerDamageTaken = EndGamePlayerPanel.Find("PlayerHealth").GetComponent<Text>();

            Text opponentNameText = EndGameOppPanel.Find("OpponentNameText").GetComponent<Text>();
            Text opponentScore = EndGameOppPanel.Find("OpponentScore").GetComponent<Text>();
            Text opponentDamage = EndGameOppPanel.Find("OpponentDamage").GetComponent<Text>();
            Text opponentDamageTaken = EndGameOppPanel.Find("OpponentHealth").GetComponent<Text>();

            endGameInfo.text = endGameText;
            playerNameText.text = NakamaData.Singleton.ClientUserName;
            playerScore.text = GameData.Singleton.PlayerScore.ToString();
            playerDamage.text = GameData.Singleton.PlayerDamage.ToString();
            playerDamageTaken.text = GameData.Singleton.PlayerDamageTaken.ToString();

            opponentNameText.text = opponentName;
            opponentScore.text = GameData.Singleton.OpponentScore.ToString();
            opponentDamage.text = GameData.Singleton.OpponentDamage.ToString();
            opponentDamageTaken.text = GameData.Singleton.OpponentDamageTaken.ToString();
        }

        public void UpdateInfoFlashPanelMessage(string message)
        {
            InfoFlashPanel.GetComponentInChildren<Text>().text = message;
            //StartCoroutine(ShowInfoFlashPanel());
        }

        public void CloseEndGameResultsPanel()
        {
            EndGameResultsPanel.gameObject.SetActive(false);
            ToggleBlockPanel(false);
            GamePlayPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// GAMEPLAY METHODS!
        /// These Send Match Data values
        /// </summary>
        /// 
        public void OpponentJoined(string opponent)
        {
            opponentName = opponent;
            opponentNameText.text = opponentName;

            //Determine who goes first
            if (firstTurn == 1)
            {
                //This player will start
                MatchController.Singleton.SendMatchData(1, "");
                Debug.Log("Player 1 will start");
                NewTurn(1);
            }
            else
            {
                //Opponent Starts first turn
                MatchController.Singleton.SendMatchData(2, "");
                Debug.Log("Player 2 will start");
                NewTurn(2);
            }
        }

        public void NewTurn(int whoseTurn)
        {
            if(playersPlayedCount == 3)
            {
                currentRound += 1;
                playersPlayedCount = 0;
            }
            Debug.Log("NewTurn():: :: WhoseTurn: " + whoseTurn + " You are player: " + playerNumber);
            //Is it this player's turn?
            if (playerNumber == whoseTurn)
            {
                //Allow this player to play by turning off blockpanel
                Debug.Log("NewTurn():: :: It's your turn!");
                ToggleBlockPanel(false);
                whoseTurnText.text = NakamaData.Singleton.ClientUserName;
                currentRoundText.text = currentRound.ToString();
                playersPlayedCount += playerNumber;
            }
            else
            {
                //Allow this player to play by turning off blockpanel
                ToggleBlockPanel(true);
                whoseTurnText.text = opponentName;
                currentRoundText.text = currentRound.ToString();
                playersPlayedCount += oppNumber;
            }
        }

        public void DrawCard()
        {
            if(Deck.Singleton.inPlayCards.Count > 4)
            {
                return;
            }

            GameData.Singleton.PlayerScore -= 10;

            CardValue drawnCard = Deck.Singleton.DrawCard();
            Transform cardTransform = Instantiate(cardSlotPrefab);
            Image cardImageTransform = cardTransform.Find("CardImage").GetComponent<Image>();

            int cardValue = (int)drawnCard.card;
            string cardSuite = drawnCard.suite.ToString().ToLower();
            string cardSpriteName = "PlayingCards/" + cardValue + "_of_" + cardSuite;
            
            cardImageTransform.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(cardSpriteName);
            cardTransform.SetParent(playerCardsPanel);

            //Send Match message that a card was drawn
            MatchController.Singleton.SendMatchData(3, "");
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

            //Send Match message that an attack was made <cardNum>:<suiteName>
            if (!gameTesting)
                MatchController.Singleton.SendMatchData(4, cardNumber + ":" + suiteNumber);

            StartCoroutine(ShowDamage(oppDamageCG));
            GameData.Singleton.OpponentHealth -= cardNumber;
            GameData.Singleton.PlayerScore += (cardNumber * 10) / currentRound;
            GameData.Singleton.PlayerDamage += cardNumber;
            GameData.Singleton.OpponentDamageTaken += cardNumber;
            if (GameData.Singleton.OpponentHealth < 1)
            {
                OpponentDied();
            }

            NewTurn(oppNumber);
            MatchController.Singleton.SendMatchData(oppNumber, "");
        }

        public void OpponentDrawCard()
        {
            Debug.Log("OpponentDrawCard()");
            GameData.Singleton.OpponentScore -= 10;

            Transform cardTransform = Instantiate(cardSlotPrefab);
            Image cardImageTransform = cardTransform.Find("CardImage").GetComponent<Image>();
            cardImageTransform.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>("PlayingCards/cardBack_blue");
            cardTransform.SetParent(oppCardsPanel);

        }

        public void OpponentAttack(int attackCard, int suiteNumber)
        {
            //Remove One Card From Opponent In Play cards
            foreach (Transform child in oppCardsPanel)
            {
                GameObject.Destroy(child.gameObject);
                break; //Only want to delete one. This seems Janky!
            }

            //Remove last card Opponent attacked with, if there is one
            foreach (Transform child in defendPanelCards)
            {
                GameObject.Destroy(child.gameObject);
            }

            CardConstants.Card card = (CardConstants.Card)attackCard;
            CardConstants.Suite suite = (CardConstants.Suite)suiteNumber;
            CardValue cardValue = new CardValue(card, suite);

            Transform cardTransform = Instantiate(cardSlotPrefab);
            Image cardImageTransform = cardTransform.Find("CardImage").GetComponent<Image>();
            
            string cardSuite = cardValue.suite.ToString().ToLower();
            string cardSpriteName = "PlayingCards/" + attackCard + "_of_" + cardSuite;

            cardImageTransform.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>(cardSpriteName);
            cardTransform.SetParent(defendPanelCards);

            StartCoroutine(ShowDamage(playerDamageCG));
            GameData.Singleton.PlayerHealth -= attackCard;
            GameData.Singleton.OpponentScore += (attackCard * 10) / currentRound;
            GameData.Singleton.PlayerDamageTaken += attackCard;
            GameData.Singleton.OpponentDamage += attackCard;
            if (GameData.Singleton.PlayerHealth < 1)
            {
                PlayerDied();
            }
        }

        public void PlayerDied()
        {
            GameData.Singleton.OpponentScore += 100;
            QuitCurrentGameMatch(opponentName + " has won the match!");
        }

        public void OpponentDied()
        {
            GameData.Singleton.PlayerScore += 100;
            QuitCurrentGameMatch(NakamaData.Singleton.ClientUserName + " has won the match!");
        }

        public void OpponentQuit()
        {
            GameData.Singleton.PlayerScore += 100;
            GameData.Singleton.OpponentScore = 0;
            QuitCurrentGameMatch(opponentName + " quit the match!");
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


