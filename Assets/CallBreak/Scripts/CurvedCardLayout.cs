using UnityEngine;

public class Curved3DCardLayout : MonoBehaviour
{
    public Transform[] cards;
    public float radius = 2f;           // Radius of the circular arc (distance from pivot)
    public float maxAngle = 60f;        // Total fan arc in degrees

    void Start()
    {
        UpdateCardLayout();
    }

    void UpdateCardLayout()
    {
        int cardCount = cards.Length;
        float startAngle = -maxAngle / 2f;
        float angleStep = cardCount > 1 ? maxAngle / (cardCount - 1) : 0f;

        for (int i = 0; i < cardCount; i++)
        {
            float angle = startAngle + i * angleStep;
            float rad = angle * Mathf.Deg2Rad;

            // Card position in arc (pivoted from this GameObject's position)
            Vector3 pos = new Vector3(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius, 0f);
            cards[i].localPosition = pos;

            // Rotate around Z axis to fan out
            cards[i].localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
