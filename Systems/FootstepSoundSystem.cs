using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepSoundSystem : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource footstepSource;
    public AudioClip[] walkClips;
    public AudioClip[] sprintClips;
    public AudioClip[] crouchClips;

    [Header("Step Timing")]
    public float walkInterval = 0.5f;
    public float sprintInterval = 0.34f;
    public float crouchInterval = 0.68f;
    public float minMoveAmount = 0.1f;

    [Header("Randomization")]
    public Vector2 volumeRange = new Vector2(0.85f, 1f);
    public Vector2 pitchRange = new Vector2(0.96f, 1.04f);

    private float stepTimer;
    private int lastClipIndex = -1;

    private void Awake()
    {
        if (footstepSource == null)
            footstepSource = GetComponent<AudioSource>();

        if (footstepSource != null)
            footstepSource.playOnAwake = false;
    }

    public void Tick(bool isGrounded, float moveAmount, bool isSprinting, bool isCrouching, float deltaTime)
    {
        if (footstepSource == null)
            return;

        bool canPlaySteps = isGrounded && moveAmount >= minMoveAmount;

        if (!canPlaySteps)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= deltaTime;

        if (stepTimer > 0f)
            return;

        PlayStep(isSprinting, isCrouching);
        stepTimer = GetStepInterval(isSprinting, isCrouching) / Mathf.Max(moveAmount, 0.4f);
    }

    private float GetStepInterval(bool isSprinting, bool isCrouching)
    {
        if (isCrouching)
            return crouchInterval;

        if (isSprinting)
            return sprintInterval;

        return walkInterval;
    }

    private void PlayStep(bool isSprinting, bool isCrouching)
    {
        AudioClip[] pool = GetClipPool(isSprinting, isCrouching);
        if (pool == null || pool.Length == 0)
            return;

        int clipIndex = Random.Range(0, pool.Length);
        if (pool.Length > 1 && clipIndex == lastClipIndex)
            clipIndex = (clipIndex + 1) % pool.Length;

        AudioClip clip = pool[clipIndex];
        if (clip == null)
            return;

        lastClipIndex = clipIndex;
        footstepSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        footstepSource.PlayOneShot(clip, Random.Range(volumeRange.x, volumeRange.y));
    }

    private AudioClip[] GetClipPool(bool isSprinting, bool isCrouching)
    {
        if (isCrouching && crouchClips != null && crouchClips.Length > 0)
            return crouchClips;

        if (isSprinting && sprintClips != null && sprintClips.Length > 0)
            return sprintClips;

        if (walkClips != null && walkClips.Length > 0)
            return walkClips;

        if (sprintClips != null && sprintClips.Length > 0)
            return sprintClips;

        if (crouchClips != null && crouchClips.Length > 0)
            return crouchClips;

        return null;
    }
}
