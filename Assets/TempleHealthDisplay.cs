using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TempleHealthDisplay : MonoBehaviour
{
    public Image FillImage;
    public Object Traveller;
    private int health;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        health = Traveller.GetComponent<TempleGuard>().health;
        FillImage.fillAmount = health / 100.0f;
    }
}
