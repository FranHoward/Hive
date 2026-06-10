using UnityEngine;

// Persistent, quiet background track. Bootstraps itself the moment the game
// starts (no scene wiring needed) and survives scene loads so the music plays
// continuously throughout. Volume is kept low so it sits under the SFX.
public class BackgroundMusic : MonoBehaviour
{
    [Range(0f, 1f)]
    public float volume = 0.12f;

    static BackgroundMusic _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (_instance != null) return;

        var clip = Resources.Load<AudioClip>("Audio/backgroundMusic");
        if (clip == null) return;

        var go = new GameObject("BackgroundMusic");
        _instance = go.AddComponent<BackgroundMusic>();
        DontDestroyOnLoad(go);

        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.spatialBlend = 0f; // 2D, audible everywhere
        src.playOnAwake = false;
        src.volume = _instance.volume;
        src.Play();
    }
}
