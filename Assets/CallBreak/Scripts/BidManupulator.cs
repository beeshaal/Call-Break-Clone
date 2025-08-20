using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BidManipulator : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text bidCountText;
    [SerializeField] private Button incrementButton;
    [SerializeField] private Button decrementButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Slider bidSlider;

    [Header("Bid Settings")]
    [SerializeField] private int minBid = 1;
    [SerializeField] private int maxBid = 13;

    [Header("References")]
    [SerializeField] private Player player;

    private int bidCount;

    void Awake()
    {
        // Optional: Initialize slider settings
        if (bidSlider != null)
        {
            bidSlider.minValue = minBid;
            bidSlider.maxValue = maxBid;
            bidSlider.wholeNumbers = true;
            bidSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        // Button event listeners
        if (incrementButton != null)
            incrementButton.onClick.AddListener(() => ChangeBid(1));

        if (decrementButton != null)
            decrementButton.onClick.AddListener(() => ChangeBid(-1));
        if (confirmButton != null)
            confirmButton.onClick.AddListener(() => SubmitCall());

        // Start with min bid
        bidCount = minBid;
        UpdateBidUI();
    }

    private void ChangeBid(int delta)
    {
        bidCount = Mathf.Clamp(bidCount + delta, minBid, maxBid);

        if (bidSlider != null)
            bidSlider.value = bidCount;

        UpdateBidUI();
    }

    private void OnSliderChanged(float value)
    {
        bidCount = Mathf.RoundToInt(value);
        UpdateBidUI();
    }

    private void UpdateBidUI()
    {
        if (bidCountText != null)
            bidCountText.text = bidCount.ToString();
    }

    private void SubmitCall()
    {
        GameManager.instance.HumanMakeCall(bidCount);
        player.UpdateCall(bidCount);
        gameObject.SetActive(false);
    }
}
