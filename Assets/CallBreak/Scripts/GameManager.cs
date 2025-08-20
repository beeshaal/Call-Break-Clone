using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using TMPro;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [System.Serializable]
    class ResultRow
    {
        public TMP_Text playerName;
        public TMP_Text totalScore;
    }

    [Header("Game Settings")]
    public int totalRounds = 1;
    public int currentRound = 1;


    [Header("Players")]
    public Player[] players = new Player[4];
    public int currentPlayerIndex = 0;
    public int dealerIndex = 0;

    [Header("Game State")]
    public List<Card> deck = new List<Card>();
    public List<Card> currentTrick = new List<Card>();
    public List<Card> playerValidCards = new List<Card>();
    public List<Player> trickPlayers = new List<Player>();
    public Suit currentLeadSuit;
    public int tricksPlayed = 0;

    [SerializeField] private TMP_Text gameStatusText;
    [SerializeField] private List<TMP_Text> playerCallTexts;
    [SerializeField] private List<TMP_Text> playerWonTexts;

    public GameState currentState = GameState.Dealing;
    public GameObject callPanel;
    public int lastCardSortingOrder = 0;
    public Transform tempCardHolder;
    public GameObject yourTurnText;

    // Turn management variables
    private bool isTurnLocked = false;
    private bool isProcessingAction = false;
    private float actionDelay = 0.5f;

    // Game state tracking for AI
    private HashSet<Card> playedCards = new HashSet<Card>();
    private bool spadesIntroduced = false;

    [Header("Score Display")]
    [SerializeField] private Transform scoreTable; // Parent object containing all score rows
    [SerializeField] private GameObject ScoreRowPrefab;
    [SerializeField] private TMP_Text[] totalScoreTexts = new TMP_Text[4]; // Total score 
                                                                           // display for each player

    [Header("Result")]
    [SerializeField] private List<ResultRow> resultRows;


    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        totalRounds = GameDataManager.Instance.Selected_Round;
        StartNewRound();

    }

    void StartNewRound()
    {
        currentState = GameState.Dealing;
        tricksPlayed = 0;
        playedCards.Clear();
        spadesIntroduced = false;
        isTurnLocked = false;
        isProcessingAction = false;

        // Reset player round data
        foreach (Player player in players)
        {
            player.ClearHand();
            player.currentCall = 0;
            player.tricksWon = 0;
            // Don't reset totalScore here as it accumulates across rounds
        }

        if (currentRound != 1) EnableCards();
        ShuffleDeck();
        DealCards();
        DisplayCardForPlayer();
        EnableFakeCards();

        currentState = GameState.Calling;
        currentPlayerIndex = (dealerIndex + 1) % 4;
        UpdateUI();
        StartCallingPhase();
    }


    void EnableFakeCards()
    {
        foreach (Player pl in players)
        {
            if (!pl.isHuman)
            {
                pl.SetFakeCardRefs();
            }
        }
    }

    void DisplayCardForPlayer()
    {
        players[0].SpreadCards();
    }

    void EnableCards()
    {
        foreach (Card card in deck)
        {
            card.gameObject.SetActive(true);
            card.GetComponent<Collider>().enabled = true;
        }
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            Card temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    void DealCards()
    {
        int cardIndex = 0;

        // Deal 13 cards to each player
        for (int i = 0; i < 13; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                int playerIndex = (dealerIndex + 1 + j) % 4;
                players[playerIndex].AddCard(deck[cardIndex]);
                cardIndex++;
            }
        }

        // Sort hands
        foreach (Player player in players)
        {
            if (player.isHuman)
            {
                player.hand = player.hand.OrderByDescending(c => c.suit).ThenByDescending(c => c.rank).ToList();
            }
            player.AssignCards();
        }
    }

    void StartCallingPhase()
    {
        if (currentState != GameState.Calling || isProcessingAction) return;

        if (AllPlayersCalled())
        {
            currentState = GameState.Playing;
            currentPlayerIndex = (dealerIndex + 1) % 4;
            StartPlayingPhase();
            return;
        }

        Player currentPlayer = players[currentPlayerIndex];
        currentPlayer.MakeCall(currentRound);

        if (!currentPlayer.isHuman)
        {
            isProcessingAction = true;
            // AI made call, move to next player
            currentPlayerIndex = (currentPlayerIndex + 1) % 4;
            UpdateUI();
            Invoke("ContinueCallingPhase", actionDelay);
        }
    }

    void ContinueCallingPhase()
    {
        isProcessingAction = false;
        StartCallingPhase();
    }

    bool AllPlayersCalled()
    {
        foreach (Player player in players)
        {
            if (player.currentCall == 0) return false;
        }
        return true;
    }

    void StartPlayingPhase()
    {
        if (currentState != GameState.Playing || isProcessingAction) return;

        if (tricksPlayed >= 13)
        {
            EndRound();
            return;
        }

        if (currentTrick.Count == 0)
        {
            Player currentPlayer = players[currentPlayerIndex];
            Card playedCard = currentPlayer.PlayCard(currentTrick, currentLeadSuit, playedCards, spadesIntroduced);

            if (playedCard != null) // AI played
            {
                PlayCard(currentPlayer, playedCard);
            }
        }
        else if (currentTrick.Count == 4)
        {
            CompleteTrick();
        }
        else
        {
            // Continue trick
            Player currentPlayer = players[currentPlayerIndex];
            Card playedCard = currentPlayer.PlayCard(currentTrick, currentLeadSuit, playedCards, spadesIntroduced);
            if (playedCard != null) // AI played
            {
                PlayCard(currentPlayer, playedCard);
            }
        }
    }

    public void BringUpValidCards(string mode = "")
    {
        if (isTurnLocked || isProcessingAction) return;
        ShowYourTurn();

        if (mode == "initiate")
        {
            foreach (Card card in players[0].hand)
            {
                playerValidCards.Add(card);
                card.transform.DOMoveY(card.transform.position.y + 0.5f, 0.25f).SetEase(Ease.InQuad);
            }
            return;
        }

        Player human = players[0];
        List<Card> validCards = GetValidCards(human, currentTrick, currentLeadSuit);

        // Highlight ONLY valid cards according to Call Break rules
        foreach (Card card in validCards)
        {
            playerValidCards.Add(card);
            card.transform.DOMoveY(card.transform.position.y + 0.5f, 0.25f).SetEase(Ease.InQuad);
        }
    }


    public List<Card> GetValidCards(Player player, List<Card> trick, Suit leadSuit)
    {
        List<Card> validCards = new List<Card>();

        // If leading (first card of trick)
        if (trick.Count == 0)
        {
            validCards.AddRange(player.hand);
            return validCards;
        }

        // Get cards of lead suit
        List<Card> leadSuitCards = player.hand.Where(c => c.suit == leadSuit).ToList();

        if (leadSuitCards.Count > 0)
        {
            // Must follow suit - but must beat if possible
            Card highestInTrick = GetHighestCardOfSuit(trick, leadSuit);

            if (highestInTrick != null)
            {
                // Check if any spades were played (trump situation)
                bool trumpPlayed = trick.Any(c => c.IsSpade) && leadSuit != Suit.Spades;

                if (trumpPlayed)
                {
                    // If trumped and we have lead suit, we can play any lead suit card
                    validCards.AddRange(leadSuitCards);
                }
                else
                {
                    // Try to find cards that can beat the highest card of lead suit
                    List<Card> winningCards = leadSuitCards.Where(c => c.GetValue() > highestInTrick.GetValue()).ToList();

                    if (winningCards.Count > 0)
                    {
                        // Must play a winning card if possible (Call Break rule)
                        validCards.AddRange(winningCards);
                    }
                    else
                    {
                        // Can't win with lead suit, play any lead suit card
                        validCards.AddRange(leadSuitCards);
                    }
                }
            }
            else
            {
                // No lead suit cards in trick yet, can play any lead suit card
                validCards.AddRange(leadSuitCards);
            }
        }
        else
        {
            // Don't have lead suit - Check spade priority
            List<Card> spadeCards = player.hand.Where(c => c.IsSpade).ToList();

            if (spadeCards.Count > 0)
            {
                // Have spades - check if trick was already trumped
                List<Card> trumpsInTrick = trick.Where(c => c.IsSpade).ToList();

                if (trumpsInTrick.Count > 0)
                {
                    // Must play higher trump if possible
                    Card highestTrump = trumpsInTrick.OrderByDescending(c => c.GetValue()).First();
                    List<Card> higherTrumps = spadeCards.Where(c => c.GetValue() > highestTrump.GetValue()).ToList();

                    if (higherTrumps.Count > 0)
                    {
                        // Must play higher trump
                        validCards.AddRange(higherTrumps);
                    }
                    else
                    {
                        // Can't beat trump, play any card
                        validCards.AddRange(player.hand);
                    }
                }
                else
                {
                    // No trumps played yet - MUST play spade (Call Break rule)
                    validCards.AddRange(spadeCards);
                }
            }
            else
            {
                // No spades - can discard any card
                validCards.AddRange(player.hand);
            }
        }

        return validCards;
    }

    private Card GetHighestCardOfSuit(List<Card> cards, Suit suit)
    {
        return cards.Where(c => c.suit == suit).OrderByDescending(c => c.GetValue()).FirstOrDefault();
    }

    public void PlayCard(Player player, Card card)
    {
        if (currentState != GameState.Playing || isProcessingAction) return;
        if (player != players[currentPlayerIndex]) return;

        // Lock turns to prevent rapid clicking issues
        isTurnLocked = true;
        isProcessingAction = true;

        lastCardSortingOrder++;
        card.GetComponent<SpriteRenderer>().sortingOrder = lastCardSortingOrder;

        if (!player.isHuman)
        {
            card.gameObject.SetActive(true);
            card.OnTap(player.target, false);
            player.fakeCardRef.UpdateCardLayout("remove");
        }

        AudioManager.Instance.PlaySFX("PlaceCard");

        // Track played cards for AI strategy
        playedCards.Add(card);
        if (card.IsSpade && !spadesIntroduced)
        {
            spadesIntroduced = true;
        }

        // Set lead suit for first card
        if (currentTrick.Count == 0)
        {
            currentLeadSuit = card.suit;
        }

        // Add to trick
        currentTrick.Add(card);
        trickPlayers.Add(player);
        player.RemoveCard(card);

        // Move to next player
        currentPlayerIndex = (currentPlayerIndex + 1) % 4;
        UpdateUI();

        if (currentTrick.Count == 4)
        {
            Invoke("CompleteTrick", 0.5f);
        }
        else
        {
            if (!players[currentPlayerIndex].isHuman)
            {
                Invoke("ContinuePlayingPhase", actionDelay);
            }
            else
            {
                Invoke("UnlockAndShowValidCards", 0.3f);
            }
        }
    }

    void ContinuePlayingPhase()
    {
        isProcessingAction = false;
        isTurnLocked = false;
        StartPlayingPhase();
    }

    void UnlockAndShowValidCards()
    {
        isProcessingAction = false;
        isTurnLocked = false;
        BringUpValidCards("");
    }

    void CompleteTrick()
    {
        // Find winner
        Player winner = GetTrickWinner();

        foreach (Transform child in tempCardHolder.transform)
        {
            child.DOMove(winner.spawnCardPos.position, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
            {
                child.SetParent(null);
                child.gameObject.SetActive(false);
            });
        }

        AudioManager.Instance.PlaySFX("CollectCard");
        tempCardHolder.transform.position = Vector3.zero;
        winner.tricksWon++;

        // Clear trick
        currentTrick.Clear();
        trickPlayers.Clear();

        // Winner leads next trick
        currentPlayerIndex = System.Array.IndexOf(players, winner);
        tricksPlayed++;
        UpdateUI();

        if (tricksPlayed >= 13)
        {
            Invoke("EndRound", 1f);
        }
        else
        {
            if (!players[currentPlayerIndex].isHuman)
            {
                Invoke("ContinuePlayingPhase", actionDelay);
            }
            else
            {
                Invoke("UnlockAndInitiateHumanTurn", 0.3f);
            }
        }
    }

    void UnlockAndInitiateHumanTurn()
    {
        isProcessingAction = false;
        isTurnLocked = false;
        BringUpValidCards("initiate");
    }

    Player GetTrickWinner()
    {
        lastCardSortingOrder = 0;
        Card winningCard = currentTrick[0];
        Player winner = trickPlayers[0];

        for (int i = 1; i < currentTrick.Count; i++)
        {
            Card card = currentTrick[i];

            // Spade beats non-spade (trump logic)
            if (card.IsSpade && !winningCard.IsSpade)
            {
                winningCard = card;
                winner = trickPlayers[i];
            }
            else if (!card.IsSpade && winningCard.IsSpade)
            {
                continue; // Current winner stays
            }
            else if (card.suit == winningCard.suit)
            {
                // Same suit - higher rank wins
                if (card.GetValue() > winningCard.GetValue())
                {
                    winningCard = card;
                    winner = trickPlayers[i];
                }
            }
            else if (card.suit == currentLeadSuit && winningCard.suit != currentLeadSuit && !winningCard.IsSpade)
            {
                // Following lead suit beats off-suit
                winningCard = card;
                winner = trickPlayers[i];
            }
        }

        return winner;
    }

    [ContextMenu("Force Round End")]
    private void ForceEndRound()
    {
        EndRound();
    }

    void EndRound()
    {
        currentState = GameState.RoundEnd;
        isProcessingAction = false;
        isTurnLocked = false;

        // Calculate scores for this round
        float[] roundScores = new float[4];

        for (int i = 0; i < players.Length; i++)
        {
            Player player = players[i];

            if (player.tricksWon >= player.currentCall)
            {
                // Made call - get call points + bonus (0.1 per extra trick)
                float baseScore = player.currentCall;
                float bonusScore = (player.tricksWon - player.currentCall) * 0.1f;
                roundScores[i] = baseScore + bonusScore;
            }
            else
            {
                // Failed call - lose call points
                roundScores[i] = -player.currentCall;
            }

            // Add to total score
            player.totalScore += roundScores[i];
        }

        // Update score table
        UpdateScoreTable(roundScores);
        StartCoroutine(ShowScorePanel());
        currentRound++;
        dealerIndex = (dealerIndex + 1) % 4;
        UpdateUI();

        if (currentRound > totalRounds)
        {
            EndGame();
        }
        else
        {
            Invoke("StartNewRound", 3f);
        }
    }

    private IEnumerator ShowScorePanel()
    {
        if (currentRound == totalRounds) yield break;
        AudioManager.Instance.PlaySFX("RoundComplete");
        PanelManager.PM_Instance.ShowPanel("Score");
        yield return new WaitForSeconds(3f);
        PanelManager.PM_Instance.HidePanel("Score");

    }

    void UpdateScoreTable(float[] roundScores)
    {
        // Ensure we have enough rows for the current round
        int roundIndex = currentRound - 1; // Convert to 0-based index
        GameObject scoreRowObj = Instantiate(ScoreRowPrefab);
        scoreRowObj.transform.SetParent(scoreTable);
        scoreRowObj.transform.localScale = Vector3.one;
        List<TMP_Text> tempScoreTexts = new List<TMP_Text>(scoreRowObj.GetComponent<ScoreData>().allScoreText);
        scoreRowObj.GetComponent<ScoreData>().roundInfo.text = $"{currentRound}({players[dealerIndex].playerName})";


        for (int i = 0; i < 4; i++)
        {
            if (tempScoreTexts[i] != null)
            {
                // Format score to show one decimal place
                tempScoreTexts[i].text = roundScores[i].ToString("F1");
            }

            // Update total score display
            if (totalScoreTexts[i] != null)
            {
                totalScoreTexts[i].text = players[i].totalScore.ToString("F1");
            }
        }
    }


    void EndGame()
    {
        currentState = GameState.GameEnd;

        // Find winner
        Player winner = players[0];
        foreach (Player player in players)
        {
            if (player.totalScore > winner.totalScore)
                winner = player;
        }
        List<Player> playersByScore = new List<Player>();
        playersByScore = players.OrderByDescending(x => x.totalScore).ToList();
        for (int i = 0; i < 4; i++)
        {
            resultRows[i].playerName.text = playersByScore[i].playerName;
            resultRows[i].totalScore.text = playersByScore[i].totalScore.ToString("F1");
        }
        AudioManager.Instance.PlaySFX("GameComplete");
        PanelManager.PM_Instance.ShowPanel("Result");
    }

    void UpdateUI()
    {
        // Update game status
        string status = "";
        switch (currentState)
        {
            case GameState.Calling:
                status = $"Round {currentRound} - {players[currentPlayerIndex].playerName} calling...";
                break;
            case GameState.Playing:
                status = $"Round {currentRound} - Trick {tricksPlayed + 1} - {(currentPlayerIndex == 0 ? "Your" : players[currentPlayerIndex].playerName + "'s")} turn";
                break;
            case GameState.RoundEnd:
                status = $"Round {currentRound - 1} Complete";
                break;
        }

        if (gameStatusText != null)
            gameStatusText.text = status;

        // Update player info
        for (int i = 0; i < 4; i++)
        {
            if (playerCallTexts[i] != null)
                playerCallTexts[i].text = $"Call: {players[i].currentCall}";
            if (playerWonTexts[i] != null)
                playerWonTexts[i].text = $"Won: {players[i].tricksWon}";
        }
    }

    // Public methods for UI interaction
    public void HumanMakeCall(int call)
    {
        if (currentState != GameState.Calling || isProcessingAction) return;
        if (!players[currentPlayerIndex].isHuman) return;

        isProcessingAction = true;
        HideYourTurn();
        players[currentPlayerIndex].ShowBidInfo();
        players[currentPlayerIndex].currentCall = call;
        currentPlayerIndex = (currentPlayerIndex + 1) % 4;
        UpdateUI();

        if (AllPlayersCalled())
        {
            currentState = GameState.Playing;
            currentPlayerIndex = (dealerIndex + 1) % 4;
            Invoke("ContinuePlayingPhase", 0.5f);
        }
        else
        {
            Invoke("ContinueCallingPhase", 0.5f);
        }
    }

    public void ShowYourTurn()
    {
        AudioManager.Instance.PlaySFX("YourTurn");
        if (yourTurnText != null)
            yourTurnText.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
    }

    public void HideYourTurn()
    {
        if (yourTurnText != null)
            yourTurnText.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
    }

    public void HumanPlayCard(Card card)
    {
        if (currentState != GameState.Playing || isTurnLocked || isProcessingAction) return;
        if (!players[currentPlayerIndex].isHuman) return;

        // Validate card play using improved validation
        List<Card> validCards = GetValidCards(players[currentPlayerIndex], currentTrick, currentLeadSuit);

        if (validCards.Contains(card))
        {
            card.OnTap(players[currentPlayerIndex].target, true);
            PlayCard(players[currentPlayerIndex], card);
            playerValidCards.Remove(card);
            HideYourTurn();
            players[0].RearrangeCards();
            playerValidCards.Clear();
        }
        else
        {
            // Invalid card - shake animation
            Vector3 originalPos = card.transform.localPosition;
            card.transform.DOLocalMoveX(originalPos.x + 0.15f, 0.15f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    card.transform.localPosition = originalPos;
                });
        }
    }

    // Simplified validation for backward compatibility
    public bool IsValidPlay(Card card)
    {
        List<Card> validCards = GetValidCards(players[currentPlayerIndex], currentTrick, currentLeadSuit);
        return validCards.Contains(card);
    }
}