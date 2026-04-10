using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SceneStreamingTrigger : MonoBehaviour
{
    [Header("Load")]
    [SerializeField] private SceneReference[] scenesToLoad;
    [SerializeField] private bool setFirstLoadedSceneActive;
    [SerializeField] private bool releaseLoadedScenesOnExit = true;

    [Header("Filter")]
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private bool requireThirdPersonController = true;
    [SerializeField] private bool triggerOnlyOnce;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs;

    private int requesterId;
    private bool hasLoadedScenes;
    private bool hasTriggered;
    private int overlapCount;

    private void Awake()
    {
        requesterId = GetInstanceID();
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CanProcess(other))
            return;

        overlapCount++;
        if (overlapCount > 1)
            return;

        if (triggerOnlyOnce && hasTriggered)
            return;

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("SceneStreamingTrigger requires an active GameManager in the persistent scene.", this);
            return;
        }

        if (scenesToLoad != null)
        {
            for (int i = 0; i < scenesToLoad.Length; i++)
                gameManager.AcquireScene(scenesToLoad[i], requesterId, setFirstLoadedSceneActive && i == 0);
        }

        hasLoadedScenes = scenesToLoad != null && scenesToLoad.Length > 0;
        hasTriggered = true;
        Log("Trigger entered, processed scene streaming request.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!CanProcess(other))
            return;

        overlapCount = Mathf.Max(0, overlapCount - 1);
        if (overlapCount > 0 || !releaseLoadedScenesOnExit || !hasLoadedScenes)
            return;

        ReleaseOwnedScenes();
    }

    private void OnDisable()
    {
        if (hasLoadedScenes && releaseLoadedScenesOnExit)
            ReleaseOwnedScenes();
    }

    private bool CanProcess(Collider other)
    {
        if (other == null)
            return false;

        ThirdPersonController playerController = other.GetComponentInParent<ThirdPersonController>();
        if (requireThirdPersonController && playerController == null)
            return false;

        GameObject targetObject = playerController != null ? playerController.gameObject : other.gameObject;

        if (!string.IsNullOrWhiteSpace(requiredTag) && !targetObject.CompareTag(requiredTag))
            return false;

        return true;
    }

    private void ReleaseOwnedScenes()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
            return;

        if (scenesToLoad != null)
        {
            for (int i = 0; i < scenesToLoad.Length; i++)
                gameManager.ReleaseScene(scenesToLoad[i], requesterId);
        }

        hasLoadedScenes = false;
        overlapCount = 0;
        Log("Released scenes owned by this trigger.");
    }

    private void Log(string message)
    {
        if (verboseLogs)
            Debug.Log($"[SceneStreamingTrigger] {message}", this);
    }
}