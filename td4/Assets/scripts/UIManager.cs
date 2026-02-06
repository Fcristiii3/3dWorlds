using UnityEngine;
using TMPro; // Essential for TextMeshPro

public class UIManager : MonoBehaviour
{
    // The reference to the actual UI element on your screen
    public TextMeshProUGUI lapText;

    // This method will be called by other scripts
    public void UpdateLapText(string message)
    {
        lapText.text = message;
    }
}