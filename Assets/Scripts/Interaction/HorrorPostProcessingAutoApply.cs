using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class HorrorPostProcessingAutoApply : MonoBehaviour
{
    public VolumeProfile horrorProfile;
    public string volumeName = "HiveHorrorVolume";
    public bool applyToAllCameras = true;
    public float priority = 25f;
    public float weight = 1f;

    VolumeProfile _runtimeProfile;

    // Guarantees the horror grade is applied every time the game starts —
    // including in builds — even if the scene has no HiveHorrorVolume (the
    // in-scene object/profile kept getting reset). If an applier already exists
    // in the loaded scene we leave it alone; otherwise we spawn a persistent one.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindFirstObjectByType<HorrorPostProcessingAutoApply>() != null) return;

        var go = new GameObject("HiveHorrorVolume (auto)");
        DontDestroyOnLoad(go);
        go.AddComponent<HorrorPostProcessingAutoApply>();
    }

    void Awake()
    {
        Apply();
    }

    void OnEnable()
    {
        Apply();
    }

    void Start()
    {
        Apply();
    }

    void LateUpdate()
    {
        ApplyCameraPostProcessing();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        Apply();
    }
#endif

    void Apply()
    {
        gameObject.name = volumeName;
        EnsureVolume();
        ApplyCameraPostProcessing();
    }

    void EnsureVolume()
    {
        Volume volume = GetComponent<Volume>();
        if (volume == null)
            volume = gameObject.AddComponent<Volume>();

        volume.isGlobal = true;
        volume.priority = priority;
        volume.weight = weight;

        if (horrorProfile != null)
        {
            EnsureHorrorOverrides(horrorProfile);
            volume.sharedProfile = horrorProfile;
        }
        else
        {
            if (_runtimeProfile == null)
            {
                _runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                _runtimeProfile.name = "RuntimeHiveHorrorPostProfile";
            }

            EnsureHorrorOverrides(_runtimeProfile);
            volume.sharedProfile = _runtimeProfile;
        }
    }

    void ApplyCameraPostProcessing()
    {
        if (!applyToAllCameras)
            return;

        foreach (Camera sceneCamera in Camera.allCameras)
            EnablePostProcessing(sceneCamera);

        EnablePostProcessing(Camera.main);
    }

    void EnablePostProcessing(Camera targetCamera)
    {
        if (targetCamera == null)
            return;

        UniversalAdditionalCameraData cameraData = targetCamera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
    }

    void EnsureHorrorOverrides(VolumeProfile profile)
    {
        if (profile == null)
            return;

        profile.components.RemoveAll(component => component == null);

        SetVolumeComponent(profile, out Vignette vignette);
        vignette.active = true;
        // Values kept in sync with HiveAtmosphereBuilder.HorrorPostProfile
        // (the "Build Horror Post Processing" tool) so the auto-applied grade
        // looks identical to what the tool produces.
        vignette.intensity.Override(0.46f);
        vignette.smoothness.Override(0.58f);
        vignette.color.Override(Color.black);

        SetVolumeComponent(profile, out ColorAdjustments color);
        color.active = true;
        color.postExposure.Override(0f);
        color.contrast.Override(32f);
        color.saturation.Override(-34f);
        color.colorFilter.Override(new Color(0.68f, 0.82f, 0.66f));

        SetVolumeComponent(profile, out Bloom bloom);
        bloom.active = true;
        bloom.threshold.Override(1.15f);
        bloom.intensity.Override(0.22f);
        bloom.scatter.Override(0.45f);

#if UNITY_EDITOR
        EditorUtility.SetDirty(profile);
#endif
    }

    void SetVolumeComponent<T>(VolumeProfile profile, out T component) where T : VolumeComponent
    {
        if (!profile.TryGet(out component))
            component = profile.Add<T>(true);
    }
}
