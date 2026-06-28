using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Persistent async scene loader. The single place that changes scenes, so a loading screen / transition
// can be added here later without touching callers. Created in the Boot scene (DontDestroyOnLoad).
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    public bool IsLoading { get; private set; }
    public float Progress { get; private set; }   // 0..1, for a loading bar later

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Load(string sceneName)
    {
        if (IsLoading) return;
        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        IsLoading = true;
        Progress = 0f;
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone)
        {
            Progress = op.progress;   // (caps at 0.9 until activation, which is automatic here)
            yield return null;
        }
        Progress = 1f;
        IsLoading = false;
    }
}
