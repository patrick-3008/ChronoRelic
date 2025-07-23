using UnityEngine;

public class ArrowCollision : MonoBehaviour
{
    public int damage;
    public string playerTag;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            PlayerCombatAndHealth playerScript = other.GetComponent<PlayerCombatAndHealth>();
            if (playerScript != null)
            {
                playerScript.health -= damage;
                Debug.Log("Player hit! Health reduced by " + damage);
            }
            Destroy(gameObject);
        }
    }
}
