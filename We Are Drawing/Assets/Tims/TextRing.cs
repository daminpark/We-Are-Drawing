using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class RepeatedWordRing : MonoBehaviour
{
    // Public variables to customize in the Inspector
    public List<string> words = new List<string>(); // List of words to choose from
    public float radius = 10f; // Radius of the horizontal ring
    public float height = 1f; // Height of the text from the player's position
    public int repetitions = 10; // How many times to repeat the word around the circle

    // TextMeshPro settings exposed to the Inspector
    public TMP_FontAsset font; // Font for the word
    public int fontSize = 5; // Font size
    public Color fontColor = Color.white; // Font color
    public FontStyles fontStyle = FontStyles.Normal; // Font style (e.g. bold, italic)

    void Start()
    {
        // Initialize random seed with current time
        Random.InitState(System.DateTime.Now.Millisecond);

        CreateRepeatedWordRing();
    }

    void CreateRepeatedWordRing()
    {
        // Ensure that the words list is not empty
        if (words.Count == 0)
        {
            Debug.LogError("Words list is empty. Please add words to the list.");
            return;
        }

        // Clear existing children (if any)
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Randomly pick a word from the list
        string selectedWord = words[Random.Range(0, words.Count)];

        // Calculate the angle increment based on the number of repetitions
        float angleStep = 360f / repetitions;

        // Create each repeated word
        for (int i = 0; i < repetitions; i++)
        {
            // Create a new GameObject for each word
            GameObject wordObj = new GameObject("Word_" + i);
            wordObj.transform.SetParent(transform);

            TextMeshPro tmp = wordObj.AddComponent<TextMeshPro>();
            tmp.text = selectedWord; // Set the selected word
            tmp.fontSize = fontSize;
            tmp.color = fontColor; // Set font color
            tmp.font = font; // Set font
            tmp.fontStyle = fontStyle; // Set font style
            tmp.alignment = TextAlignmentOptions.Center;

            // Calculate the angle and position
            float angle = i * angleStep;
            Vector3 position = new Vector3(
                Mathf.Cos(Mathf.Deg2Rad * angle) * radius,
                height, // Keep it at a fixed height
                Mathf.Sin(Mathf.Deg2Rad * angle) * radius
            );
            wordObj.transform.localPosition = position;

            // Rotate the word to face the center of the circle
            wordObj.transform.rotation = Quaternion.LookRotation(position.normalized, Vector3.up);
        }
    }
}
