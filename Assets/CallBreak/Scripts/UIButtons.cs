using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIButtons : MonoBehaviour
{
    [SerializeField] private List<Button> allUIButtons;

    void Start()
    {
        foreach (Button btn in allUIButtons)
        {
            if (btn != null)
            {
                btn.onClick.AddListener(OnUIBtnClicked); // lowercase onClick
            }
        }
    }

    void OnUIBtnClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("UITap");
        }
        else
        {
            Debug.LogWarning("AudioManager instance not found!");
        }
    }
}