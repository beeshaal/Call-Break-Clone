using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public enum Suit
{
    Spades = 3,
    Diamonds = 2,
    Clubs = 1,
    Hearts = 0
}
public enum Rank
{
    Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7, Eight = 8,
    Nine = 9, Ten = 10, Jack = 11, Queen = 12, King = 13, Ace = 14
}

public class Card : MonoBehaviour
{
    public Suit suit;
    public Rank rank;
    public Sprite frontSprite;

    void Start()
    {
        GetComponent<SpriteRenderer>().sprite = frontSprite;
    }

    public bool IsSpade => suit == Suit.Spades;

    public string GetCardName()
    {
        return $"{rank} of {suit}";
    }

    public int GetValue()
    {
        return (int)rank;
    }

    public void OnTap(Transform target, bool human)
    {
        GetComponent<Collider>().enabled = false;
        transform.SetParent(GameManager.instance.tempCardHolder);
        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOMove(target.position, 0.5f).SetEase(Ease.OutBack));
        sequence.Join(transform.DORotate(new Vector3(0, 0, 180), 0.5f, RotateMode.FastBeyond360)
                            .SetRelative());
        sequence.Join(transform.DOScale(new Vector3(0.4f, 0.4f, 0.4f), 0.5f));
        sequence.OnComplete(() =>
        {
            if (human)
            {
                GameManager.instance.HumanPlayCard(this);
            }
            //Do Post actions
        });
    }
}