using UnityEngine;

public class ChestPotion : MonoBehaviour
{
    public Transform lid; // Reference to the lid object
    public Vector3 openPosition = new Vector3(0, 0.167f, 0.108f);
    public Vector3 openRotation = new Vector3(-131.1f, 0, 0);
    public float openSpeed = 2f;
    public bool isPlayerNearby = false;
    private bool isOpened = false;
    private bool isOpening = false;
    public GameObject Player;
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

            // ✅ Reward player with potion and update UI
            PlayerCombatAndHealth player = Player.GetComponent<PlayerCombatAndHealth>();
            if (player != null)
            {
                player.potionCount += 1;
                // Update the potion count display
                player.UpdatePotionCountText();
                Debug.Log("Player received a potion! Total potions: " + player.potionCount);
            }
            else
            {
                Debug.LogError("PlayerCombatAndHealth component not found on Player GameObject!");
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