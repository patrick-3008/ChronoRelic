using UnityEngine;

public class PotionChest : MonoBehaviour
{
    public Transform lid; // Reference to the lid object
    public Vector3 openPosition = new Vector3(0, 0.167f, 0.108f);
    public Vector3 openRotation = new Vector3(-131.1f, 0, 0);
    public float openSpeed = 2f; // Speed of lid opening
    public int goldReward = 50; // Amount of gold the player gets

    public bool isPlayerNearby = false;
    private bool isOpened = false;
    private bool isOpening = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Start()
    {
        // Store the lid's initial transform
        initialPosition = lid.localPosition;
        initialRotation = lid.localRotation;
    }

    private void Update()
    {
        if (isPlayerNearby && !isOpened && Input.GetKeyDown(KeyCode.E))
        {
            isOpened = true;
            isOpening = true;

            // Find the player's script and add gold
            PlayerGold player = FindObjectOfType<PlayerGold>();
            if (player != null)
            {
                player.AddGold(goldReward);
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

        // Gradually interpolate position and rotation
        lid.localPosition = Vector3.Lerp(lid.localPosition, openPosition, step);
        lid.localRotation = Quaternion.Lerp(lid.localRotation, Quaternion.Euler(openRotation), step);

        // Stop interpolation once target is reached
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
