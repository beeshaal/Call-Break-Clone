using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RoundSelector : MonoBehaviour
{
    [SerializeField] private Button round3Btn;
    [SerializeField] private Button round5Btn;

    private void Start()
    {
        round3Btn.onClick.AddListener(() => OnRoundSelected(3));
        round5Btn.onClick.AddListener(() => OnRoundSelected(5));
    }

    public void OnRoundSelected(int num)
    {
        // Kill any ongoing tweens to avoid conflicts
        round3Btn.transform.DOKill();
        round5Btn.transform.DOKill();

        switch (num)
        {
            case 3:
                round3Btn.transform.DOScale(2.2f, 0.15f).SetEase(Ease.OutBack);
                round5Btn.transform.DOScale(2f, 0.15f).SetEase(Ease.InBack);
                GameDataManager.Instance.Selected_Round = 3;
                break;
            case 5:
                round5Btn.transform.DOScale(2.2f, 0.15f).SetEase(Ease.OutBack);
                round3Btn.transform.DOScale(2f, 0.15f).SetEase(Ease.InBack);
                GameDataManager.Instance.Selected_Round = 5;
                break;
        }
    }
}
