using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;

public class Player : MonoBehaviour
{
    public string playerName;
    public bool isHuman;
    public List<Card> hand = new List<Card>();
    public List<Transform> cardTransforms = new List<Transform>();
    public int currentCall = 0;
    public int tricksWon = 0;
    public float totalScore;
    public Transform spawnCardPos;
    public Transform target;
    public CurvedCard2D fakeCardRef;
    public GameObject bidInfo;
    public Vector3 offset;

    public void ShowBidInfo()
    {
        bidInfo.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
    }

    public void SetFakeCardRefs()
    {
        fakeCardRef.cards.Clear();

        foreach (RectTransform child in fakeCardRef.transform)
        {
            child.gameObject.SetActive(true);
            fakeCardRef.cards.Add(child);
        }
        fakeCardRef.UpdateCardLayout();
    }

    public virtual void MakeCall(int roundNumber)
    {
        if (isHuman)
        {
            GameManager.instance.ShowYourTurn();
            GameManager.instance.callPanel.SetActive(true);
            return;
        }
        ShowBidInfo();
        currentCall = CalculateSmartAICall(roundNumber);
    }

    public virtual Card PlayCard(List<Card> currentTrick, Suit leadSuit, HashSet<Card> playedCards, bool spadesIntroduced)
    {
        if (isHuman)
        {
            GameManager.instance.BringUpValidCards();
            return null;
        }

        return SelectSmartAICard(currentTrick, leadSuit, playedCards, spadesIntroduced);
    }

    public void UpdateCall(int call)
    {
        currentCall = call;
    }

    // Improved AI calling logic
    private int CalculateSmartAICall(int roundNumber)
    {
        int call = 0;
        int spadeCount = 0;
        int sureWinners = 0;
        int probableWinners = 0;

        // Count spades by strength
        int aceSpades = 0;
        int highSpades = 0; // K, Q, J
        int mediumSpades = 0; // 9, 10

        // Count non-spade aces and kings
        int nonSpadeAces = 0;
        int nonSpadeKings = 0;

        // Analyze hand composition
        Dictionary<Suit, int> suitCounts = new Dictionary<Suit, int>();
        Dictionary<Suit, List<Card>> suitCards = new Dictionary<Suit, List<Card>>();

        foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
        {
            suitCounts[suit] = 0;
            suitCards[suit] = new List<Card>();
        }

        foreach (Card card in hand)
        {
            suitCounts[card.suit]++;
            suitCards[card.suit].Add(card);

            if (card.IsSpade)
            {
                spadeCount++;
                if (card.GetValue() == 14) aceSpades++;
                else if (card.GetValue() >= 11) highSpades++;
                else if (card.GetValue() >= 9) mediumSpades++;
            }
            else
            {
                if (card.GetValue() == 14) nonSpadeAces++;
                else if (card.GetValue() == 13) nonSpadeKings++;
            }
        }

        // Calculate sure winners (Ace of Spades, non-spade Aces)
        sureWinners = aceSpades + nonSpadeAces;

        // Calculate probable winners
        // High spades (K, Q, J of spades)
        probableWinners += highSpades;

        // Non-spade Kings (50% chance to win)
        probableWinners += nonSpadeKings;

        // Spade distribution bonus
        int spadeDistributionBonus = 0;
        if (spadeCount >= 6) spadeDistributionBonus = 2; // Very strong spade suit
        else if (spadeCount >= 4) spadeDistributionBonus = 1; // Good spade suit
        else if (spadeCount <= 2) spadeDistributionBonus = -1; // Weak spade suit

        // Long suit potential (7+ cards in a suit can generate extra tricks)
        int longSuitBonus = 0;
        foreach (var suit in suitCounts)
        {
            if (suit.Key != Suit.Spades && suit.Value >= 7)
            {
                longSuitBonus += 1;
            }
        }

        // Base call calculation
        call = sureWinners + (probableWinners / 2) + spadeDistributionBonus + longSuitBonus;

        // Adjust for round number and risk tolerance
        if (roundNumber <= 3)
        {
            // Early rounds - be slightly conservative
            call = Mathf.Max(1, call - 1);
        }
        else if (roundNumber >= 8)
        {
            // Later rounds - be more aggressive
            call += 1;
        }

        // Ensure minimum bid of 1 and reasonable maximum
        call = Mathf.Clamp(call, 1, Mathf.Min(10, roundNumber + 2));

        return call;
    }


    // Much improved AI card selection
    private Card SelectSmartAICard(List<Card> currentTrick, Suit leadSuit, HashSet<Card> playedCards, bool spadesIntroduced)
    {
        List<Card> validCards = GameManager.instance.GetValidCards(this, currentTrick, leadSuit);

        if (validCards.Count == 0) return hand[0]; // Shouldn't happen

        if (currentTrick.Count == 0)
        {
            // Leading the trick - strategic lead
            return SelectLeadCard(playedCards, spadesIntroduced);
        }
        else
        {
            // Following - try to win or play strategically
            return SelectFollowCard(validCards, currentTrick, leadSuit, playedCards);
        }
    }

    private Card SelectLeadCard(HashSet<Card> playedCards, bool spadesIntroduced)
    {
        // Strategy for leading a trick - prioritize medium cards to conserve high cards

        // Categorize cards by strength
        List<Card> aces = hand.Where(c => c.GetValue() == 14 && !c.IsSpade).ToList();
        List<Card> kings = hand.Where(c => c.GetValue() == 13 && !c.IsSpade).ToList();
        List<Card> mediumCards = hand.Where(c => c.GetValue() >= 6 && c.GetValue() <= 11 && !c.IsSpade).ToList();
        List<Card> lowCards = hand.Where(c => c.GetValue() <= 5 && !c.IsSpade).ToList();
        List<Card> spades = hand.Where(c => c.IsSpade).ToList();

        int tricksNeeded = currentCall - tricksWon;
        int tricksRemaining = 13 - GameManager.instance.tricksPlayed;
        bool desperate = tricksNeeded >= tricksRemaining - 1;

        // If desperate for tricks, lead with strongest available card
        if (desperate)
        {
            if (aces.Count > 0) return aces[0];
            if (spades.Count > 0) return spades.OrderByDescending(c => c.GetValue()).First();
            if (kings.Count > 0) return kings[0];
            if (mediumCards.Count > 0) return mediumCards.OrderByDescending(c => c.GetValue()).First();
        }

        // Normal strategy: lead with medium cards (6-Jack) to conserve high cards
        if (mediumCards.Count > 0)
        {
            // Prefer Jacks and 10s as they're strong but not the strongest
            List<Card> preferredMedium = mediumCards.Where(c => c.GetValue() >= 10).ToList();
            if (preferredMedium.Count > 0)
            {
                return preferredMedium[Random.Range(0, preferredMedium.Count)];
            }
            return mediumCards[Random.Range(0, mediumCards.Count)];
        }

        // If no medium cards, check if we should lead with an ace/king strategically
        if (aces.Count > 0)
        {
            // Only lead with ace if we're confident it will win
            Card ace = aces[0];
            // Check if this is a short suit where ace is likely to hold
            List<Card> sameSuitCards = hand.Where(c => c.suit == ace.suit).ToList();
            if (sameSuitCards.Count <= 3) // Short suit - ace more likely to win
            {
                return ace;
            }
        }

        if (kings.Count > 0)
        {
            // Only lead with king if ace of same suit has been played
            foreach (Card king in kings)
            {
                bool acePlayedInSuit = playedCards.Any(c => c.suit == king.suit && c.GetValue() == 14);
                if (acePlayedInSuit)
                {
                    return king;
                }
            }
        }

        // Lead with low cards if that's all we have in non-spades
        if (lowCards.Count > 0)
        {
            return lowCards.OrderByDescending(c => c.GetValue()).First(); // Highest of low cards
        }

        // If only spades remain, lead strategically
        if (spades.Count > 0)
        {
            // If we need tricks, lead high spade
            if (tricksNeeded > tricksRemaining / 2)
            {
                return spades.OrderByDescending(c => c.GetValue()).First();
            }
            else
            {
                // Lead medium spade to establish trump
                List<Card> mediumSpades = spades.Where(c => c.GetValue() >= 8 && c.GetValue() <= 11).ToList();
                if (mediumSpades.Count > 0)
                {
                    return mediumSpades[Random.Range(0, mediumSpades.Count)];
                }
                return spades.OrderBy(c => c.GetValue()).First(); // Lowest spade
            }
        }

        // Fallback - should rarely reach here
        return hand[Random.Range(0, hand.Count)];
    }


    private Card SelectFollowCard(List<Card> validCards, List<Card> currentTrick, Suit leadSuit, HashSet<Card> playedCards)
    {
        // Determine current winning card
        Card currentWinner = GetTrickWinner(currentTrick, leadSuit);
        Player currentWinnerPlayer = GameManager.instance.trickPlayers[currentTrick.IndexOf(currentWinner)];

        // Check if we have lead suit cards
        List<Card> leadSuitCards = validCards.Where(c => c.suit == leadSuit).ToList();

        if (leadSuitCards.Count > 0)
        {
            // We have lead suit - must play it
            List<Card> winningLeadCards = leadSuitCards.Where(c => CanCardWin(c, currentWinner, leadSuit)).ToList();

            if (winningLeadCards.Count > 0 && ShouldTryToWin(currentWinnerPlayer))
            {
                return winningLeadCards.OrderBy(c => c.GetValue()).First();
            }
            else
            {
                return leadSuitCards.OrderBy(c => c.GetValue()).First();
            }
        }
        else
        {
            // Don't have lead suit - MUST play spade if we have one
            List<Card> spades = validCards.Where(c => c.IsSpade).ToList();
            List<Card> nonSpades = validCards.Where(c => !c.IsSpade).ToList();

            if (spades.Count > 0)
            {
                // We have spades - check if we should try to win
                List<Card> winningSpades = spades.Where(c => CanCardWin(c, currentWinner, leadSuit)).ToList();

                if (winningSpades.Count > 0 && ShouldTrump(currentWinnerPlayer))
                {
                    // Play lowest winning spade
                    return winningSpades.OrderBy(c => c.GetValue()).First();
                }
                else if (winningSpades.Count == 0 && currentWinner.IsSpade)
                {
                    // Can't beat existing trump, play lowest spade
                    return spades.OrderBy(c => c.GetValue()).First();
                }
                else
                {
                    // Decide whether to trump or not
                    bool shouldTrump = ShouldTrump(currentWinnerPlayer);
                    if (shouldTrump)
                    {
                        return spades.OrderBy(c => c.GetValue()).First();
                    }
                    else
                    {
                        // Only play non-spade if we have no strategic reason to trump
                        if (nonSpades.Count > 0)
                        {
                            return nonSpades.OrderBy(c => c.GetValue()).First();
                        }
                        else
                        {
                            return spades.OrderBy(c => c.GetValue()).First();
                        }
                    }
                }
            }
            else
            {
                // No spades - discard lowest card
                return validCards.OrderBy(c => c.GetValue()).First();
            }
        }
    }

    private bool ShouldTryToWin(Player currentWinningPlayer)
    {
        // Always try to win if we're behind on our call
        int tricksNeeded = currentCall - tricksWon;
        int tricksRemaining = 13 - GameManager.instance.tricksPlayed;

        if (tricksNeeded >= tricksRemaining)
        {
            return true; // Desperate - must win
        }

        if (tricksNeeded > tricksRemaining / 2)
        {
            return true; // Aggressive play needed
        }

        // Don't help opponents who are behind on their calls
        int opponentTricksNeeded = currentWinningPlayer.currentCall - currentWinningPlayer.tricksWon;
        if (opponentTricksNeeded > tricksRemaining / 2)
        {
            return true; // Prevent opponent from catching up
        }

        return false; // Conservative play
    }

    private bool ShouldTrump(Player currentWinningPlayer)
    {
        // Similar logic to ShouldTryToWin but for trumping decisions
        int tricksNeeded = currentCall - tricksWon;
        int tricksRemaining = 13 - GameManager.instance.tricksPlayed;

        // Always trump if desperate for tricks
        if (tricksNeeded >= tricksRemaining - 1)
        {
            return true;
        }

        // Trump if opponent is close to making their call
        int opponentProgress = (int)currentWinningPlayer.tricksWon / currentWinningPlayer.currentCall;
        if (opponentProgress > 0.7f)
        {
            return true;
        }

        return false;
    }

    private bool CanCardWin(Card cardToPlay, Card currentWinner, Suit leadSuit)
    {
        // Spade beats non-spade (trump logic)
        if (cardToPlay.IsSpade && !currentWinner.IsSpade)
        {
            return true;
        }
        else if (!cardToPlay.IsSpade && currentWinner.IsSpade)
        {
            return false;
        }
        else if (cardToPlay.suit == currentWinner.suit)
        {
            // Same suit - higher rank wins
            return cardToPlay.GetValue() > currentWinner.GetValue();
        }
        else if (cardToPlay.suit == leadSuit && currentWinner.suit != leadSuit && !currentWinner.IsSpade)
        {
            // Following lead suit beats off-suit
            return true;
        }

        return false;
    }

    private Card GetTrickWinner(List<Card> trick, Suit leadSuit)
    {
        if (trick.Count == 0) return null;

        Card winner = trick[0];
        foreach (Card card in trick)
        {
            if (CanCardWin(card, winner, leadSuit))
                winner = card;
        }
        return winner;
    }

    // Helper method to evaluate hand strength for calling
    private float EvaluateHandStrength()
    {
        float strength = 0f;

        foreach (Card card in hand)
        {
            if (card.GetValue() == 14) strength += 1.0f; // Ace
            else if (card.GetValue() == 13) strength += 0.8f; // King  
            else if (card.GetValue() == 12) strength += 0.6f; // Queen
            else if (card.GetValue() == 11) strength += 0.4f; // Jack
            else if (card.GetValue() >= 9) strength += 0.2f; // 9, 10

            // Bonus for spades (trump)
            if (card.IsSpade)
            {
                strength += 0.3f;
            }
        }

        return strength;
    }

    // Existing methods remain the same...
    public void AddCard(Card card)
    {
        hand.Add(card);
    }

    public void RemoveCard(Card card)
    {
        hand.Remove(card);
    }

    public void ClearHand()
    {
        hand.Clear();
    }

    public void AssignCards()
    {
        for (int i = 0; i < cardTransforms.Count && i < hand.Count; i++)
        {
            hand[i].transform.position = spawnCardPos.position;
            hand[i].transform.rotation = spawnCardPos.rotation;
            hand[i].transform.localScale = spawnCardPos.localScale;
            hand[i].transform.SetParent(transform);
            hand[i].gameObject.SetActive(false);
        }
    }

    public void SpreadCards(float duration = 0.75f, Ease easeType = Ease.OutBack)
    {
        for (int i = 0; i < cardTransforms.Count && i < hand.Count; i++)
        {
            Card card = hand[i];
            Transform target = cardTransforms[i];

            card.GetComponent<SpriteRenderer>().sortingOrder = i;
            card.transform.SetParent(transform);
            card.gameObject.SetActive(true);

            // Tweening position, rotation, and scale
            card.transform.DOMove(target.position, duration).SetEase(easeType);
            card.transform.DORotateQuaternion(target.rotation, duration).SetEase(easeType);
            card.transform.DOScale(target.localScale, duration).SetEase(easeType);
        }
    }

    public void RearrangeCards()
    {
        if (!isHuman) return;

        float spacing = 1.4f;
        int n = hand.Count;
        float totalWidth = (n - 1) * spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < n; i++)
        {
            Vector3 targetPos = new Vector3(startX + i * spacing, 0, 0) + offset;
            hand[i].transform.DOMove(targetPos, 0.4f).SetEase(Ease.InBack);
        }
    }
}