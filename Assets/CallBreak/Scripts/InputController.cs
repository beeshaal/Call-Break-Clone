using UnityEngine;

public class InputController : MonoBehaviour
{
    private bool isInputLocked = false;
    private float lastClickTime = 0f;
    private float clickCooldown = 0.3f; // Prevent rapid clicking

    void Update()
    {
        // Only process input during playing state
        if (GameManager.instance.currentState != GameState.Playing) return;

        // Only allow input for human player's turn
        if (GameManager.instance.currentPlayerIndex != 0) return;

        // Prevent input during animations or processing
        if (isInputLocked) return;

        if (Input.GetMouseButtonDown(0))
        {
            // Prevent rapid clicking
            if (Time.time - lastClickTime < clickCooldown) return;

            ProcessCardClick();
            lastClickTime = Time.time;
        }
    }

    private void ProcessCardClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log($"Hit: {hit.collider.gameObject.name}");

            Card targetCard = hit.collider.GetComponent<Card>();
            if (targetCard != null)
            {
                // Check if the clicked card is in the valid cards list
                if (GameManager.instance.playerValidCards.Contains(targetCard))
                {
                    // Lock input to prevent multiple rapid clicks
                    LockInput();

                    // Play the card
                    GameManager.instance.HumanPlayCard(targetCard);

                    // Unlock after a delay
                    Invoke("UnlockInput", clickCooldown);
                }
                else
                {
                    Debug.Log("Invalid card selection");
                    // Optional: Play error sound or show visual feedback
                }
            }
        }
    }

    public void LockInput()
    {
        isInputLocked = true;
    }

    public void UnlockInput()
    {
        isInputLocked = false;
    }

    // Method to be called by GameManager when it's safe to accept input
    public void EnableInput()
    {
        isInputLocked = false;
    }

    // Method to be called by GameManager when input should be disabled
    public void DisableInput()
    {
        isInputLocked = true;
    }
}