using UnityEngine;
using TMPro; // Import TextMeshPro namespace
using System.Collections;

public class TextDisplay : MonoBehaviour
{
    public TextMeshPro uiText; // Use TextMeshProUGUI instead of Text
    public string[] lines; // Populate this array with the lines you want to display
    public float displayTime = 2f; // Time each line will be displayed

    private void Start()
    {
        StartCoroutine(DisplayLines());
    }

    private IEnumerator DisplayLines()
    {
        for (int i = 0; i < lines.Length; i++)
        {
            uiText.text = lines[i].Replace("\\n", "\n"); // Replace placeholder with actual newline character
            yield return new WaitForSeconds(displayTime);
            if (i < lines.Length - 1)
            {
                uiText.text = string.Empty;
                yield return new WaitForSeconds(0.5f); // Small pause between lines
            }
        }
    }
}
