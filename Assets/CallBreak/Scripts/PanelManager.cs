using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class OverlayPanel
{
    public string panelName;
    public GameObject panelGameObject;
}

public class PanelManager : MonoBehaviour
{
    public static PanelManager PM_Instance;

    [SerializeField] private InputController inputController;
    public List<OverlayPanel> gameOverlayPanels;

    private OverlayPanel currentActivePanel = null;

    [Header("Animation Settings")]
    public float animDuration = 0.4f;
    public Ease showEase = Ease.OutBack;
    public Ease hideEase = Ease.InBack;
    public float fadedAlpha = 0.85f; // if you want semi-transparent backgrounds

    void Awake()
    {
        if (PM_Instance == null)
            PM_Instance = this;
    }

    public void ShowPanel(string panelName)
    {
        foreach (OverlayPanel op in gameOverlayPanels)
        {
            bool shouldShow = op.panelName == panelName;

            if (shouldShow)
            {
                inputController?.LockInput();
                op.panelGameObject.SetActive(true);
                currentActivePanel = op;

                // Reset state before animating
                CanvasGroup cg = GetOrAddCanvasGroup(op.panelGameObject);
                cg.alpha = 0f;
                op.panelGameObject.transform.localScale = Vector3.one * 0.8f;

                // Animate in
                cg.DOFade(1f, animDuration * 0.8f).SetEase(Ease.OutQuad);
                op.panelGameObject.transform.DOScale(1f, animDuration).SetEase(showEase);
            }
            else
            {
                op.panelGameObject.SetActive(false);
            }
        }
    }

    public void HidePanel(string panelName)
    {
        foreach (OverlayPanel op in gameOverlayPanels)
        {
            if (op.panelName == panelName)
            {
                AnimateHide(op);

                if (currentActivePanel == op)
                    currentActivePanel = null;
            }
        }
    }

    public void HideCurrentPanel()
    {
        if (currentActivePanel != null)
        {
            AnimateHide(currentActivePanel);
            currentActivePanel = null;
        }
    }

    private void AnimateHide(OverlayPanel op)
    {
        CanvasGroup cg = GetOrAddCanvasGroup(op.panelGameObject);

        // Animate out
        Sequence hideSeq = DOTween.Sequence();
        hideSeq.Append(op.panelGameObject.transform.DOScale(0.8f, animDuration * 0.8f).SetEase(hideEase));
        hideSeq.Join(cg.DOFade(0f, animDuration * 0.8f).SetEase(Ease.InQuad));
        hideSeq.OnComplete(() =>
        {
            inputController?.UnlockInput();
            op.panelGameObject.SetActive(false);
        });
    }

    public OverlayPanel GetCurrentPanel()
    {
        return currentActivePanel;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = go.AddComponent<CanvasGroup>();
        return cg;
    }
}
