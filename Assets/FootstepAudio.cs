using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepAudio : MonoBehaviour
{
    private CharacterController charController;
    private Animator anim;

    public AudioSource footstepSource;
    public AudioClip[] footstepClips;

    public float walkFootstepInterval = 0.4f;
    public float runFootstepInterval = 0.25f;
    private float footstepTimer = 0f;

    void Start()
    {
        charController = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();

        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.loop = false;
            footstepSource.playOnAwake = false;
        }
    }

    void Update()
    {
        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        if (charController.isGrounded && anim.GetBool("Walking") && !anim.GetBool("IsCrouching"))
        {
            float walkRunValue = anim.GetFloat("Walk_Run");
            float currentInterval = walkRunValue > 0.6f ? runFootstepInterval : walkFootstepInterval;

            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0f)
            {
                PlayRandomFootstep();
                footstepTimer = currentInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    private void PlayRandomFootstep()
    {
        if (footstepClips.Length > 0)
        {
            int randomIndex = Random.Range(0, footstepClips.Length);
            footstepSource.clip = footstepClips[randomIndex];
            footstepSource.Play();
        }
    }
}
