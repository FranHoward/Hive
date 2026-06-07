using UnityEngine;

// Drives an unsteady, organic flicker on a Light. Used by the hive-atmosphere
// props (failing bulbs, pulsing red emergency lamps, sickly glow over egg sacs).
// Add to a GameObject with a Light; tune in the inspector. Runtime-only effect.
[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    [Tooltip("Light intensity when fully lit.")]
    public float baseIntensity = 2f;

    [Tooltip("How far below base the flicker can dip (0..1 of base).")]
    [Range(0f, 1f)] public float flickerDepth = 0.6f;

    [Tooltip("Flicker speed. Higher = more frantic.")]
    public float speed = 12f;

    [Tooltip("Chance per second of a hard 'blackout' blink.")]
    [Range(0f, 10f)] public float blackoutRate = 0.7f;

    [Tooltip("How long a blackout blink lasts (seconds).")]
    public float blackoutDuration = 0.08f;

    Light _light;
    float _noiseSeed;
    float _blackoutUntil;

    void Awake()
    {
        _light = GetComponent<Light>();
        _noiseSeed = Random.value * 100f;
        if (baseIntensity <= 0f) baseIntensity = _light.intensity;
    }

    void Update()
    {
        // Smooth pseudo-random dip via layered Perlin noise.
        float n = Mathf.PerlinNoise(_noiseSeed, Time.time * speed);
        float n2 = Mathf.PerlinNoise(_noiseSeed + 5.3f, Time.time * speed * 0.37f);
        float dip = Mathf.Lerp(1f - flickerDepth, 1f, n * 0.7f + n2 * 0.3f);

        // Occasional sharp blackout blink.
        if (Time.time >= _blackoutUntil && Random.value < blackoutRate * Time.deltaTime)
            _blackoutUntil = Time.time + blackoutDuration;
        if (Time.time < _blackoutUntil)
            dip = 0.04f;

        _light.intensity = baseIntensity * dip;
    }
}
