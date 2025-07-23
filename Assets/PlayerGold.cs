using TMPro;
using UnityEngine;

public class PlayerGold : MonoBehaviour
{
    public int gold = 100;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI goldText2;

    void Start()
    {
        UpdateGoldText();
    }

    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log("Gold added! Current gold: " + gold);
        UpdateGoldText();
    }

    public void RemoveGold(int amount)
    {
        gold -= amount;
        Debug.Log("Gold removed! Current gold: " + gold);
        UpdateGoldText();
    }

    private void UpdateGoldText()
    {
        if (goldText != null)
        {
            goldText.text = "" + gold;
            goldText2.text = "" + gold;
        }
    }
}
