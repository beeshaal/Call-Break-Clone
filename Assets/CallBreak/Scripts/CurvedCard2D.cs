// using UnityEngine;
// using UnityEngine.UI;
// using System.Linq;

// [ExecuteAlways]
// public class CurvedCard2D : MonoBehaviour
// {
//     public RectTransform[] cards;
//     public float radius = 300f;
//     public float baseMaxAngle = 30f;

//     private RectTransform[] previousCards;

//     private void OnValidate()
//     {
//         CleanUpNulls();
//         UpdateCardLayout();
//     }

//     private void Start()
//     {
//         CleanUpNulls();
//         UpdateCardLayout();
//     }

//     private void Update()
//     {
//         // Check for card changes every frame (Editor or Runtime)
//         if (!cards.SequenceEqual(previousCards))
//         {
//             CleanUpNulls();
//             UpdateCardLayout();
//         }
//     }

//     private void CleanUpNulls()
//     {
//         cards = cards.Where(c => c != null).ToArray();
//         previousCards = cards.ToArray(); // cache snapshot for comparison
//     }

//     private void UpdateCardLayout()
//     {
//         if (cards == null || cards.Length == 0) return;

//         int cardCount = cards.Length;

//         // Adapt maxAngle based on card count (you can change this logic if needed)
//         float maxAngle = (cardCount > 1) ? baseMaxAngle * (cardCount - 1) / 4f : 0f;
//         maxAngle = Mathf.Min(maxAngle, baseMaxAngle); // Clamp it if needed

//         float startAngle = -maxAngle / 2f;
//         float angleStep = (cardCount > 1) ? maxAngle / (cardCount - 1) : 0f;

//         for (int i = 0; i < cardCount; i++)
//         {
//             float angle = startAngle + i * angleStep;
//             float rad = angle * Mathf.Deg2Rad;

//             float x = Mathf.Sin(rad) * radius;
//             float y = Mathf.Cos(rad) * radius;

//             cards[i].anchoredPosition = new Vector2(x, -y + radius);
//             cards[i].localRotation = Quaternion.Euler(0, 0, angle);
//         }
//     }
// }

using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

[ExecuteAlways]
public class CurvedCard2D : MonoBehaviour
{
    public List<RectTransform> cards = new List<RectTransform>();
    public float radius = 300f;
    public float baseMaxAngle = 30f;

    private void Start()
    {
        DistributeUsingTween();
        UpdateCardLayout();
    }

    public void UpdateCardLayout(string def = "")
    {
        if (def == "remove" && cards.Count > 0)
        {
            cards[0].gameObject.SetActive(false);
            cards.RemoveAt(0);
        }

        if (cards == null || cards.Count == 0) return;

        int cardCount = cards.Count;

        float maxAngle = (cardCount > 1) ? baseMaxAngle * (cardCount - 1) / 4f : 0f;
        maxAngle = Mathf.Min(maxAngle, baseMaxAngle);

        float startAngle = -maxAngle / 2f;
        float angleStep = (cardCount > 1) ? maxAngle / (cardCount - 1) : 0f;

        for (int i = 0; i < cardCount; i++)
        {
            float angle = startAngle + i * angleStep;
            float rad = angle * Mathf.Deg2Rad;

            float x = Mathf.Sin(rad) * radius;
            float y = Mathf.Cos(rad) * radius;

            cards[i].anchoredPosition = new Vector2(x, -y + radius);
            cards[i].localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void DistributeUsingTween()
    {
        // Animate stuff later with DOTween here
    }
}
