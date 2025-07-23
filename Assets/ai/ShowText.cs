using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;

public class ShowText : MonoBehaviour
{
    public string textValue;
    public TextMeshProUGUI textElement;
    string filePath = "D:/gradai/grad/Assets/ai/gbt/gbt_output.txt";

    void Update()
    {
        /*textValue = ReadTextFromFile(filePath);
        textElement.text = textValue;
        textElement.text = ArabicSupport.Fix(textElement.text, true, false);*/
    }

    // Method to read a text file from Resources folder
    string ReadTextFromFile(string fileName)
    {

        if (File.Exists(fileName))
        {
            return File.ReadAllText(filePath);
        }
        else
        {
            Debug.LogWarning("File not found: " + filePath);
            return "File not found";
        }
    }
}
