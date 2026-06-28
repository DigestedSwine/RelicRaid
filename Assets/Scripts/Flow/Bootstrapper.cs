using UnityEngine;
using UnityEngine.SceneManagement;

// Lives in the Boot scene. Ensures the persistent managers exist, then hands off to the main menu.
// Boot is first in Build Settings so this always runs before anything else.
public class Bootstrapper : MonoBehaviour
{
    public string firstScene = "MainMenu";

    void Start()
    {
        // GameSession / SceneLoader live on sibling objects in the Boot scene and self-persist (DontDestroyOnLoad).
        if (GameSession.Instance == null) new GameObject("GameSession").AddComponent<GameSession>();
        if (SceneLoader.Instance == null) new GameObject("SceneLoader").AddComponent<SceneLoader>();

        SceneManager.LoadScene(firstScene);
    }
}
