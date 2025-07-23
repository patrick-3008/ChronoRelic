using UnityEngine;

public class RockProjectile : MonoBehaviour
{
    public float lifetime = 5f;
    public int damage = 25;
    public float knockbackForce = 10f;

    private void Start()
    {
        Destroy(gameObject, lifetime);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        int rockLayer = gameObject.layer;
        int playerLayer = LayerMask.NameToLayer("Player");
        int obstacleLayer = LayerMask.NameToLayer("obstacleLayer");

        for (int i = 0; i < 32; i++)
        {
            if (i != playerLayer && i != obstacleLayer)
                Physics.IgnoreLayerCollision(rockLayer, i);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerCombatAndHealth player = other.GetComponentInParent<PlayerCombatAndHealth>();
        Animator playerAnimator = other.GetComponentInParent<Animator>();

        if (player == null || playerAnimator == null) return;

        // Prevent damage/knockback if already invincible or in knockdown animations
        if (player.isInvincible || IsInKnockdownState(playerAnimator)) return;

        // Apply damage
        player.health -= damage;
        player.isInvincible = true;

        // Trigger knockdown animation
        if (!playerAnimator.IsInTransition(0))
            playerAnimator.SetTrigger("knockedDown");

        // Knockback logic
        Vector3 knockbackDir = (other.transform.position - transform.position).normalized;
        knockbackDir.y = 0f;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            player.StartCoroutine(ApplyKnockback(controller, knockbackDir, 10f, 0.5f, player));
        }
        else
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);

            // End invincibility after a short delay if no controller
            player.StartCoroutine(EndInvincibilityAfterDelay(player, 0.5f));
        }

        Destroy(gameObject);
    }

    private bool IsInKnockdownState(Animator animator)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("Knocked Down") || stateInfo.IsName("Standing Up");
    }

    private System.Collections.IEnumerator ApplyKnockback(CharacterController controller, Vector3 direction, float distance, float duration, PlayerCombatAndHealth player)
    {
        float elapsed = 0f;
        Vector3 totalDisplacement = direction * distance;

        while (elapsed < duration)
        {
            float delta = Time.deltaTime / duration;
            Vector3 move = totalDisplacement * delta;

            controller.Move(move);
            elapsed += Time.deltaTime;
            yield return null;
        }

        player.isInvincible = false;
    }

    private System.Collections.IEnumerator EndInvincibilityAfterDelay(PlayerCombatAndHealth player, float delay)
    {
        yield return new WaitForSeconds(delay);
        player.isInvincible = false;
    }
}
