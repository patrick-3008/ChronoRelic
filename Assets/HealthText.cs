using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class HealthText : MonoBehaviour
{
    public Object Traveller;
    public TextMeshProUGUI hudText;
    // Start is called before the first frame update
    void Start()
    {
        hudText = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        hudText.text = "Health: " + Traveller.GetComponent<SubZeroMove>().health.ToString() + "\n Hit Time: " + Traveller.GetComponent<SubZeroMove>().hitTimer.ToString();
    }
}
