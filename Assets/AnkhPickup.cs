using UnityEngine;

public class AnkhPickup : MonoBehaviour
{
    //public Transform pharaoh;               // Reference to the Pharaoh transform
    public Transform playerLeftHand;        // Reference to the player's left hand
    public float moveSpeed = 1.5f;          // Speed for flying into the hand

    private bool playerInRange = false;
    public bool isPickedUp = false;
    private AudioSource audioSource;
    public AudioClip ankhPickupSound;

    private Vector3 targetLocalPosition = new Vector3(0.139f, 0.294f, 0.079f);
    private Quaternion targetLocalRotation = Quaternion.Euler(0f, 90f, 90f);
    private Vector3 targetLocalScale = new Vector3(80f, 80f, 80f);

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

        void Update()
    {
        // Only allow pickup if Ankh is NOT a child of the Pharaoh
        if (transform.parent != null && !isPickedUp)
            return;

        if (playerInRange && !isPickedUp && Input.GetKeyDown(KeyCode.E))
        {
            PickUp();
        }

        // If Ankh has been picked up, move it smoothly to local position/rotation
        if (isPickedUp)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, moveSpeed * Time.deltaTime);
            Quaternion targetRotation = Quaternion.Euler(0f, 90f, 90f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, moveSpeed * Time.deltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, targetLocalScale, moveSpeed * Time.deltaTime);
        }
    }

    private void PickUp()
    {
        isPickedUp = true;
        transform.SetParent(playerLeftHand);
        audioSource.PlayOneShot(ankhPickupSound);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
