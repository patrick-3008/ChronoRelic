using UnityEngine;

public class TempleChest : MonoBehaviour
{
    public Transform lid; // Reference to the lid object
    public Vector3 openPosition = new Vector3(0, 0.167f, 0.108f);
    public Vector3 openRotation = new Vector3(-131.1f, 0, 0);
    public float openSpeed = 2f; // Speed of lid opening
    public int goldReward = 100; // Amount of gold the player gets

    public bool isPlayerNearby = false;
    private bool isOpened = false;
    private bool isOpening = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public TempleGuard guard1; // Reference to the Temple Guard script
    public TempleGuard guard2; // Reference to the Temple Guard script

    private void Start()
    {
        initialPosition = lid.localPosition;
        initialRotation = lid.localRotation;
    }

    private void Update()
    {
        if (isPlayerNearby && !isOpened && Input.GetKeyDown(KeyCode.E))
        {
            isOpened = true;
            isOpening = true;

            // Give player gold
            PlayerGold player = FindObjectOfType<PlayerGold>();
            if (player != null)
            {
                player.AddGold(goldReward);
            }

            // Make guard hostile
            if (guard1 != null)
            {
                guard1.hostile = true;
            }

            if (guard2 != null)
            {
                guard2.hostile = true;
            }
        }

        if (isOpening)
        {
            OpenLid();
        }
    }

    private void OpenLid()
    {
        float step = openSpeed * Time.deltaTime;

        lid.localPosition = Vector3.Lerp(lid.localPosition, openPosition, step);
        lid.localRotation = Quaternion.Lerp(lid.localRotation, Quaternion.Euler(openRotation), step);

        if (Vector3.Distance(lid.localPosition, openPosition) < 0.01f &&
            Quaternion.Angle(lid.localRotation, Quaternion.Euler(openRotation)) < 0.5f)
        {
            lid.localPosition = openPosition;
            lid.localRotation = Quaternion.Euler(openRotation);
            isOpening = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
        }
    }
}
