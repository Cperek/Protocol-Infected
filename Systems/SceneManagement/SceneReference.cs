using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public sealed class SceneReference : ISerializationCallbackReceiver
{
    [SerializeField] private string sceneName;

#if UNITY_EDITOR
    [SerializeField] private SceneAsset sceneAsset;
#endif

    public string SceneName => sceneName;

    public bool IsAssigned => !string.IsNullOrWhiteSpace(sceneName);

    public void OnAfterDeserialize()
    {
    }

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        if (sceneAsset != null)
            sceneName = sceneAsset.name;
#endif
    }
}