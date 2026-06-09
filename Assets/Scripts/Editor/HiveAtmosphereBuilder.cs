// Procedurally dresses the two escape-room spaces as an infected bug hive:
// organic wall/ceiling growths, hanging egg sacs, corner webbing, sickly fog,
// colored flickering lights and drifting spores. Built from cheap primitives
// (spheres / capsules) with dark emissive materials, exactly the "cluster simple
// shapes" approach that reads as hive infestation.
//
// Everything lives under a single "HiveAtmosphere" root, grouped by section, so
// it is fully reversible (Tools > Horror Decor > Hive > Clear, or delete the
// root / git checkout the scene). Each section is idempotent: rebuilding clears
// just that group. Placement is seeded, so results are stable between runs.
//
// Menu:  Tools > Horror Decor > Hive > ...
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HorrorDecorTools
{
    public static class HiveAtmosphereBuilder
    {
        const string RootName = "HiveAtmosphere";
        const string TargetScene = "SampleScene";
        const string MatDir = "Assets/Materials/Hive";
        const string HorrorProfilePath = MatDir + "/HiveHorrorPostProfile.asset";

        // Room volumes (world space). Floors at y=0, ceilings at y=4.
        struct Region { public float minX, maxX, minZ, maxZ, floorY, ceilY; public int seed; }
        static readonly Region Room1 = new Region { minX = -8, maxX = 8, minZ = -8, maxZ = 8, floorY = 0, ceilY = 4, seed = 1337 };
        static readonly Region Room2 = new Region { minX = -8, maxX = 8, minZ = -34, maxZ = -22, floorY = 0, ceilY = 4, seed = 7331 };
        // Room 1 only (per request). Add Room2 back to this array to decorate both.
        static readonly Region[] Regions = { Room1 };

        // Room 1 scare landmarks. These line up with the visible hive crack / wall bugs
        // and are intentionally clustered so the room has pools of light, not blanket fill.
        static readonly Vector3 Room1BugFocus = new Vector3(7.35f, 1.85f, 0.2f);
        static readonly Vector3 Room1NestFocus = new Vector3(0.5f, 1.0f, 4.0f);

        // ------------------------------------------------------------------ menus
        [MenuItem("Tools/Horror Decor/Hive/Build All")]
        public static void BuildAll()
        {
            if (!GuardScene()) return;
            BuildGrowths(false);
            BuildPillars(false);
            BuildEggSacs(false);
            BuildWebbing(false);
            SetContrastLightingBaseline(false);
            BuildLights(false);
            BuildSpores(false);
            BuildGroundHaze(false);
            BuildPostProcessing(false);
            SetupFog(false);
            SaveAndLog("Built full hive atmosphere");
        }

        [MenuItem("Tools/Horror Decor/Hive/Build Wall + Ceiling Growths")]
        public static void BuildGrowthsMenu() { if (GuardScene()) { BuildGrowths(true); SaveAndLog("Built growths"); } }
        [MenuItem("Tools/Horror Decor/Hive/Build Hive Pillars (break up space)")]
        public static void BuildPillarsMenu() { if (GuardScene()) { BuildPillars(true); SaveAndLog("Built pillars"); } }
        [MenuItem("Tools/Horror Decor/Hive/Build Hanging Egg Sacs")]
        public static void BuildEggSacsMenu() { if (GuardScene()) { BuildEggSacs(true); SaveAndLog("Built egg sacs"); } }
        [MenuItem("Tools/Horror Decor/Hive/Build Corner Webbing")]
        public static void BuildWebbingMenu() { if (GuardScene()) { BuildWebbing(true); SaveAndLog("Built webbing"); } }
        [MenuItem("Tools/Horror Decor/Hive/Build Atmosphere Lights")]
        public static void BuildLightsMenu() { if (GuardScene()) { BuildLights(true); SaveAndLog("Built lights"); } }
        [MenuItem("Tools/Horror Decor/Hive/Build Drifting Spores")]
        public static void BuildSporesMenu() { if (GuardScene()) { BuildSpores(true); SaveAndLog("Built spores"); } }
        [MenuItem("Tools/Horror Decor/Hive/Build Low Ground Haze")]
        public static void BuildGroundHazeMenu() { if (GuardScene()) { BuildGroundHaze(true); SaveAndLog("Built low ground haze"); } }
        [MenuItem("Tools/Horror Decor/Hive/Build Horror Post Processing")]
        public static void BuildPostProcessingMenu() { if (GuardScene()) { BuildPostProcessing(true); SaveAndLog("Built horror post processing"); } }
        [MenuItem("Tools/Horror Decor/Hive/Build Horror Effects")]
        public static void BuildHorrorEffectsMenu()
        {
            if (!GuardScene()) return;
            SetContrastLightingBaseline(true);
            BuildLights(true);
            BuildSpores(true);
            BuildGroundHaze(true);
            BuildPostProcessing(true);
            SetupFog(true);
            SaveAndLog("Built Room 1 horror effects");
        }
        [MenuItem("Tools/Horror Decor/Hive/Set High Contrast Base Lighting")]
        public static void SetContrastLightingBaselineMenu() { if (GuardScene()) { SetContrastLightingBaseline(true); SaveAndLog("Set high contrast base lighting"); } }
        [MenuItem("Tools/Horror Decor/Hive/Setup Fog + Dark Ambient")]
        public static void SetupFogMenu() { if (GuardScene()) { SetupFog(true); SaveAndLog("Setup fog"); } }

        [MenuItem("Tools/Horror Decor/Hive/Dim Existing Scene Lights")]
        public static void DimExistingLights()
        {
            if (!GuardScene()) return;
            SetContrastLightingBaseline(true);
            SaveAndLog("Set existing lights to high contrast horror baseline");
        }

        static void SetContrastLightingBaseline(bool _)
        {
            var root = FindRoot();
            int n = 0;
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (root != null && l.transform.IsChildOf(root.transform)) continue; // skip generated hive lights
                if (l.type == LightType.Directional)
                {
                    l.intensity = 0.22f;
                    l.color = new Color(0.55f, 0.68f, 0.62f);
                    l.shadows = LightShadows.Soft;
                    l.shadowStrength = 0.85f;
                    n++;
                    continue;
                }

                l.intensity = Mathf.Min(l.intensity, 0.65f);
                l.range = Mathf.Min(l.range, 4f);
                l.shadows = LightShadows.Soft;
                l.shadowStrength = Mathf.Max(l.shadowStrength, 0.75f);
                n++;
            }
            // Debug.Log($"[Hive] Set {n} existing lights to a high contrast baseline.");
        }

        [MenuItem("Tools/Horror Decor/Hive/Clear Hive Atmosphere")]
        public static void Clear()
        {
            if (!GuardScene()) return;
            var existing = FindRoot();
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            SaveAndLog("Cleared hive atmosphere");
        }

        // -------------------------------------------------------------- materials
        static Material _flesh, _resin, _egg, _eggGlow, _web;

        static void EnsureMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MatDir))
                AssetDatabase.CreateFolder("Assets/Materials", "Hive");

            // Flesh stays matte/dark; resin carries only a faint green emissive accent
            // (a small glowing part within each cluster, not the whole mass). Visibility
            // comes from the green growth lights added in BuildLights, not the material.
            _flesh   = Lit("HiveFlesh",  new Color(0.10f, 0.06f, 0.07f), 0.25f, 0f, null);
            _resin   = Lit("HiveResin",  new Color(0.06f, 0.05f, 0.04f), 0.85f, 0.1f, new Color(0.05f, 0.08f, 0.03f) * 0.4f);
            _egg     = Lit("HiveEggSac", new Color(0.18f, 0.22f, 0.10f), 0.75f, 0f, new Color(0.20f, 0.35f, 0.08f) * 0.6f);
            _eggGlow = Lit("HiveEggGlow",new Color(0.30f, 0.45f, 0.15f), 0.6f, 0f, new Color(0.35f, 0.70f, 0.15f) * 1.6f);
            _web     = Lit("HiveWeb",    new Color(0.55f, 0.55f, 0.52f), 0.1f, 0f, null);
        }

        // Idempotent: reuses the existing .mat asset if present (preserving its GUID
        // and any references) and just updates its properties in place.
        static Material Lit(string name, Color baseCol, float smooth, float metal, Color? emission)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(sh) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != sh) m.shader = sh;

            m.SetColor("_BaseColor", baseCol);
            m.SetColor("_Color", baseCol);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------------------------------------------------------------- growths
        static void BuildGrowths(bool _)
        {
            EnsureMaterials();
            var group = Section("Growths");
            foreach (var r in Regions)
            {
                var rng = new System.Random(r.seed);
                float midZ = (r.minZ + r.maxZ) * 0.5f;

                // Wall patches: (worldPos, inwardNormal). Kept toward corners / high
                // up and away from door centers so they don't mask interactables.
                var patches = new List<(Vector3 pos, Vector3 nrm, float rad)>
                {
                    (new Vector3(r.minX + 0.4f, 1.6f, midZ - 4f), Vector3.right,  1.0f),
                    (new Vector3(r.minX + 0.4f, 2.9f, midZ + 3f), Vector3.right,  0.8f),
                    (new Vector3(r.maxX - 0.4f, 1.7f, midZ - 3f), Vector3.left,   1.0f),
                    (new Vector3(r.maxX - 0.4f, 2.6f, midZ + 3f), Vector3.left,   0.9f),
                    (new Vector3(r.minX + 4f,   2.3f, r.maxZ - 0.4f), Vector3.back,  0.9f),
                    // (south-wall patch removed — it sat right on the keypad)
                    // ceiling
                    (new Vector3(r.minX + 5f,   r.ceilY - 0.3f, midZ + 3f), Vector3.down, 1.0f),
                    (new Vector3(r.maxX - 4f,   r.ceilY - 0.3f, midZ - 3f), Vector3.down, 0.9f),
                    (new Vector3((r.minX+r.maxX)/2, r.ceilY - 0.3f, midZ), Vector3.down, 0.8f),
                };
                foreach (var p in patches)
                    Cluster(group, p.pos, p.nrm, p.rad, 6 + rng.Next(4), _flesh, _resin, rng);

                // Low floor mounds creeping out of the corners.
                Cluster(group, new Vector3(r.minX + 1.6f, r.floorY, r.minZ + 1.6f), Vector3.up, 1.1f, 7, _flesh, _resin, rng);
                Cluster(group, new Vector3(r.maxX - 1.6f, r.floorY, r.maxZ - 1.6f), Vector3.up, 1.0f, 7, _flesh, _resin, rng);
            }
        }

        // Tall floor-to-ceiling organic columns that break sightlines and carve
        // the open room into implied zones (without colliders, so the player can
        // still squeeze past — tune positions if any blocks a needed path).
        static void BuildPillars(bool _)
        {
            EnsureMaterials();
            var group = Section("Pillars");
            foreach (var r in Regions)
            {
                var rng = new System.Random(r.seed + 99);
                var bases = new[]
                {
                    new Vector3(r.maxX - 1.2f, 0, r.maxZ - 1.2f),
                    new Vector3(r.minX + 1.2f, 0, r.minZ + 1.2f),
                    new Vector3(r.maxX - 1.6f, 0, r.minZ + 5f),
                };
                foreach (var b in bases)
                {
                    int segments = 6;
                    for (int i = 0; i < segments; i++)
                    {
                        float t = i / (float)(segments - 1);
                        float y = Mathf.Lerp(0.3f, r.ceilY - 0.2f, t);
                        float rad = Mathf.Lerp(0.9f, 0.45f, t) * (0.8f + (float)rng.NextDouble() * 0.4f);
                        var jitter = new Vector3((float)(rng.NextDouble() - 0.5) * 0.4f, 0, (float)(rng.NextDouble() - 0.5) * 0.4f);
                        Blob(group, b + new Vector3(0, y, 0) + jitter, Vector3.one * rad, _flesh);
                    }
                }
            }
        }

        // ---------------------------------------------------------------- egg sacs
        static void BuildEggSacs(bool _)
        {
            EnsureMaterials();
            var group = Section("EggSacs");
            foreach (var r in Regions)
            {
                var rng = new System.Random(r.seed + 17);
                float midX = (r.minX + r.maxX) * 0.5f;
                float midZ = (r.minZ + r.maxZ) * 0.5f;
                // (x, z, sacBottomY) — at least one hung low enough to walk under.
                var sacs = new List<Vector3>
                {
                    new Vector3(midX - 3f, midZ + 3f, 1.7f),
                    new Vector3(midX + 2f, midZ - 3f, 2.3f),
                    new Vector3(midX + 4f, midZ + 4f, 2.4f),
                    new Vector3(midX - 4f, midZ - 2f, 2.6f),
                    new Vector3(midX + 0.5f, midZ + 4f, 1.5f), // low
                };
                foreach (var s in sacs)
                {
                    var pos = new Vector3(s.x, 0, s.y);
                    float bottomY = s.z;
                    float sacH = 0.55f + (float)rng.NextDouble() * 0.35f;
                    float sacCenterY = bottomY + sacH * 0.5f;
                    float sacTopY = bottomY + sacH;

                    // hanging stalk from ceiling to the sac
                    float stalkBottom = sacTopY - 0.1f;
                    float stalkH = (r.ceilY - 0.1f) - stalkBottom;
                    var stalk = Capsule(group, new Vector3(pos.x, stalkBottom + stalkH * 0.5f, pos.z),
                                        new Vector3(0.08f, stalkH * 0.5f, 0.08f), Quaternion.identity, _resin);
                    stalk.name = "EggStalk";

                    var sac = Blob(group, new Vector3(pos.x, sacCenterY, pos.z),
                                   new Vector3(0.55f, sacH, 0.55f), _egg);
                    sac.name = "EggSac";
                    sac.transform.rotation = Quaternion.Euler((float)(rng.NextDouble() - 0.5) * 12f, 0, (float)(rng.NextDouble() - 0.5) * 12f);

                    // inner glow nub
                    var glow = Blob(group, new Vector3(pos.x, sacCenterY, pos.z), Vector3.one * 0.22f, _eggGlow);
                    glow.name = "EggGlow";

                    // a couple of smaller eggs clustered at the base
                    int extra = 1 + rng.Next(3);
                    for (int i = 0; i < extra; i++)
                    {
                        var off = new Vector3((float)(rng.NextDouble() - 0.5) * 0.5f, (float)rng.NextDouble() * 0.3f, (float)(rng.NextDouble() - 0.5) * 0.5f);
                        Blob(group, new Vector3(pos.x, bottomY + 0.15f, pos.z) + off, Vector3.one * (0.2f + (float)rng.NextDouble() * 0.15f), _egg);
                    }
                }
            }
        }

        // ---------------------------------------------------------------- webbing
        static void BuildWebbing(bool _)
        {
            EnsureMaterials();
            var group = Section("Webbing");
            foreach (var r in Regions)
            {
                float y = r.ceilY - 0.35f;
                var corners = new[]
                {
                    new Vector2(r.minX + 0.3f, r.minZ + 0.3f),
                    new Vector2(r.maxX - 0.3f, r.minZ + 0.3f),
                    new Vector2(r.minX + 0.3f, r.maxZ - 0.3f),
                    new Vector2(r.maxX - 0.3f, r.maxZ - 0.3f),
                };
                foreach (var c in corners)
                {
                    Vector3 inward = new Vector3(Mathf.Sign((r.minX + r.maxX) / 2 - c.x), 0, Mathf.Sign((r.minZ + r.maxZ) / 2 - c.y));
                    // three slack strands fanning across the corner
                    for (int i = 0; i < 3; i++)
                    {
                        float d = 1.0f + i * 0.6f;
                        Vector3 a = new Vector3(c.x, y, c.y);
                        Vector3 b = a + new Vector3(inward.x * d, -0.2f - i * 0.15f, 0);
                        Vector3 c2 = a + new Vector3(0, -0.2f - i * 0.15f, inward.z * d);
                        Strand(group, a, b, 0.025f, _web);
                        Strand(group, a, c2, 0.025f, _web);
                        Strand(group, b, c2, 0.02f, _web);
                    }
                }
            }
        }

        // ------------------------------------------------------------------ lights
        static void BuildLights(bool _)
        {
            var group = Section("Lights");
            foreach (var r in Regions)
            {
                float midX = (r.minX + r.maxX) * 0.5f;
                float midZ = (r.minZ + r.maxZ) * 0.5f;

                // Narrow bug key light: the bug reads clearly while the room edges stay dark.
                AddSpotLight(group, "Bug_Key_Spot", new Vector3(3.7f, 3.45f, -2.2f), Room1BugFocus,
                             new Color(0.78f, 0.86f, 0.72f), intensity: 9.5f, range: 8f, spotAngle: 27f,
                             shadows: true, flickerDepth: 0.18f, speed: 2.2f, blackout: 0.02f);

                // Back / rim light catches the bug silhouette and hive crack.
                AddSpotLight(group, "Bug_Rim_Backlight", new Vector3(7.0f, 3.1f, 2.6f), Room1BugFocus,
                             new Color(0.25f, 0.95f, 0.35f), intensity: 5.8f, range: 4.5f, spotAngle: 42f,
                             shadows: false, flickerDepth: 0.22f, speed: 3.5f, blackout: 0.03f);

                // Hard overhead flicker: brief blackouts make the whole space unstable.
                AddSpotLight(group, "Flickering_Overhead_Pool", new Vector3(midX - 2.4f, r.ceilY - 0.22f, midZ - 2.6f),
                             new Vector3(midX - 2.2f, 0.2f, midZ - 2.2f),
                             new Color(1f, 0.82f, 0.55f), intensity: 8.0f, range: 6f, spotAngle: 52f,
                             shadows: true, flickerDepth: 0.82f, speed: 16f, blackout: 1.45f);

                // Small red danger accents. Kept local so the whole room does not become red soup.
                AddPointLight(group, "Red_Emergency_Door_Accent", new Vector3(midX, 1.25f, r.minZ + 0.7f),
                              new Color(1f, 0.06f, 0.025f), intensity: 2.4f, range: 3.0f,
                              shadows: true, flickerDepth: 0.35f, speed: 5.5f, blackout: 0.2f);

                AddPointLight(group, "Red_Emergency_Wall_Accent", new Vector3(r.maxX - 0.85f, 1.2f, midZ + 2.9f),
                              new Color(1f, 0.05f, 0.025f), intensity: 2.1f, range: 2.7f,
                              shadows: true, flickerDepth: 0.5f, speed: 7f, blackout: 0.28f);

                // Sickly nest pool light for eggs/spores without filling the corners.
                AddPointLight(group, "Nest_Sickly_Glow", new Vector3(Room1NestFocus.x, 1.5f, Room1NestFocus.z),
                              new Color(0.35f, 0.9f, 0.18f), intensity: 2.3f, range: 3.2f,
                              shadows: false, flickerDepth: 0.28f, speed: 2f, blackout: 0.04f);

                // Soft accent glows so the key interactables and the dark corners read
                // clearly. These are gentle, no-blackout fill pools layered on top of the
                // dramatic spots — they lift the gloom without flattening the atmosphere.
                AddPointLight(group, "Keypad_Glow", new Vector3(3f, 1.5f, r.minZ + 0.85f),
                              new Color(0.95f, 0.78f, 0.42f), intensity: 2.0f, range: 3.6f,
                              shadows: false, flickerDepth: 0.08f, speed: 2f, blackout: 0f);
                AddPointLight(group, "Key_Glow", new Vector3(3f, 0.6f, 3f),
                              new Color(0.85f, 0.92f, 0.5f), intensity: 1.8f, range: 3.0f,
                              shadows: false, flickerDepth: 0.1f, speed: 2.4f, blackout: 0f);

                // Four corner fill pools so the room edges aren't pitch black.
                var cornerGlow = new Color(0.32f, 0.6f, 0.28f);
                AddPointLight(group, "Corner_Glow_NW", new Vector3(r.minX + 1.4f, 0.7f, r.maxZ - 1.4f),
                              cornerGlow, intensity: 1.1f, range: 3.8f, shadows: false, flickerDepth: 0.06f, speed: 1.6f, blackout: 0f);
                AddPointLight(group, "Corner_Glow_NE", new Vector3(r.maxX - 1.4f, 0.7f, r.maxZ - 1.4f),
                              cornerGlow, intensity: 1.1f, range: 3.8f, shadows: false, flickerDepth: 0.06f, speed: 1.7f, blackout: 0f);
                AddPointLight(group, "Corner_Glow_SW", new Vector3(r.minX + 1.4f, 0.7f, r.minZ + 1.4f),
                              cornerGlow, intensity: 1.1f, range: 3.8f, shadows: false, flickerDepth: 0.06f, speed: 1.5f, blackout: 0f);
                AddPointLight(group, "Corner_Glow_SE", new Vector3(r.maxX - 1.4f, 0.7f, r.minZ + 1.4f),
                              cornerGlow, intensity: 1.1f, range: 3.8f, shadows: false, flickerDepth: 0.06f, speed: 1.8f, blackout: 0f);

                // Green growth lights: graze the wall/ceiling clusters so the organic
                // masses are visible without making the masses themselves emissive. The
                // floor mounds are already lit by the corner glows above.
                var growthGlow = new Color(0.32f, 0.7f, 0.24f);
                AddPointLight(group, "Growth_Glow_WestWall", new Vector3(r.minX + 1f, 2.2f, midZ - 0.5f),
                              growthGlow, intensity: 1.6f, range: 6.5f, shadows: false, flickerDepth: 0.12f, speed: 2.2f, blackout: 0f);
                AddPointLight(group, "Growth_Glow_EastWall", new Vector3(r.maxX - 1f, 2.1f, midZ + 0.5f),
                              growthGlow, intensity: 1.6f, range: 6.5f, shadows: false, flickerDepth: 0.12f, speed: 2.4f, blackout: 0f);
                AddPointLight(group, "Growth_Glow_NorthWall", new Vector3(r.minX + 4f, 2.3f, r.maxZ - 1f),
                              growthGlow, intensity: 1.4f, range: 4.5f, shadows: false, flickerDepth: 0.12f, speed: 2.6f, blackout: 0f);
                AddPointLight(group, "Growth_Glow_Ceiling", new Vector3(midX, r.ceilY - 0.6f, midZ),
                              growthGlow, intensity: 1.5f, range: 7f, shadows: false, flickerDepth: 0.1f, speed: 2f, blackout: 0f);
            }
        }

        static void AddPointLight(Transform parent, string name, Vector3 pos, Color color,
                                  float intensity, float range, bool shadows,
                                  float flickerDepth, float speed, float blackout)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            l.shadowStrength = shadows ? 0.95f : 0f;
            l.shadowBias = 0.02f;
            l.shadowNormalBias = 0.15f;
            AddFlicker(go, intensity, flickerDepth, speed, blackout);
        }

        static void AddSpotLight(Transform parent, string name, Vector3 pos, Vector3 target, Color color,
                                 float intensity, float range, float spotAngle, bool shadows,
                                 float flickerDepth, float speed, float blackout)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation((target - pos).normalized, Vector3.up);

            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.spotAngle = spotAngle;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            l.shadowStrength = shadows ? 1f : 0f;
            l.shadowBias = 0.015f;
            l.shadowNormalBias = 0.12f;
            AddFlicker(go, intensity, flickerDepth, speed, blackout);
        }

        static void AddFlicker(GameObject go, float intensity, float flickerDepth, float speed, float blackout)
        {
            var f = go.AddComponent<FlickeringLight>();
            f.baseIntensity = intensity;
            f.flickerDepth = flickerDepth;
            f.speed = speed;
            f.blackoutRate = blackout;
        }

        // ------------------------------------------------------------------ spores
        static void BuildSpores(bool _)
        {
            EnsureMaterials();
            var sporeMat = SporeMaterial();
            var group = Section("Spores");
            foreach (var r in Regions)
            {
                float midX = (r.minX + r.maxX) * 0.5f;
                float midZ = (r.minZ + r.maxZ) * 0.5f;
                float w = r.maxX - r.minX;
                float d = r.maxZ - r.minZ;

                // room-wide faint drifting motes
                MakeSpores(group, "RoomSpores", new Vector3(midX, 1.5f, midZ),
                           new Vector3(w * 0.85f, 2.6f, d * 0.85f), sporeMat,
                           rate: 14f, size: 0.05f, color: new Color(0.6f, 0.8f, 0.4f, 0.5f), rise: 0.15f);

                // denser greenish spores rising near the egg zone
                MakeSpores(group, "EggSpores", new Vector3(midX + 0.5f, 0.8f, midZ + 4f),
                           new Vector3(2.5f, 1.5f, 2.5f), sporeMat,
                           rate: 10f, size: 0.07f, color: new Color(0.4f, 0.9f, 0.2f, 0.7f), rise: 0.4f);

                // Tight dust column through the bug spotlight so the beam is visible.
                MakeSpores(group, "BugLightDust", new Vector3(4.8f, 1.8f, -1.1f),
                           new Vector3(2.6f, 2.8f, 2.2f), sporeMat,
                           rate: 18f, size: 0.035f, color: new Color(0.8f, 0.85f, 0.62f, 0.55f), rise: 0.08f);
            }
        }

        // ------------------------------------------------------------------ haze
        static void BuildGroundHaze(bool _)
        {
            EnsureMaterials();
            var hazeMat = HazeMaterial();
            var group = Section("GroundHaze");
            foreach (var r in Regions)
            {
                float midX = (r.minX + r.maxX) * 0.5f;
                float midZ = (r.minZ + r.maxZ) * 0.5f;
                float w = r.maxX - r.minX;
                float d = r.maxZ - r.minZ;

                MakeHaze(group, "LowFloorMist", new Vector3(midX, 0.14f, midZ),
                         new Vector3(w * 0.75f, 0.25f, d * 0.75f), hazeMat,
                         rate: 9f, size: 1.15f, color: new Color(0.28f, 0.34f, 0.28f, 0.20f), drift: 0.18f);
                MakeHaze(group, "NestHeavyMist", new Vector3(Room1NestFocus.x, 0.2f, Room1NestFocus.z),
                         new Vector3(3.5f, 0.22f, 3.5f), hazeMat,
                         rate: 11f, size: 0.85f, color: new Color(0.25f, 0.42f, 0.22f, 0.24f), drift: 0.12f);
            }
        }

        static void MakeHaze(Transform parent, string name, Vector3 pos, Vector3 boxSize,
                             Material mat, float rate, float size, Color color, float drift)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 11f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size);
            main.startColor = color;
            main.maxParticles = 180;
            main.gravityModifier = -0.004f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission; emission.rateOverTime = rate;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = boxSize;
            var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-drift, drift);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(-drift, drift);

            var colorLife = ps.colorOverLifetime; colorLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.35f), new GradientAlphaKey(0f, 1f) });
            colorLife.color = grad;

            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.material = mat;
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.sortingFudge = -0.2f;
        }

        static void MakeSpores(Transform parent, string name, Vector3 pos, Vector3 boxSize,
                               Material mat, float rate, float size, Color color, float rise)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 8f;
            main.startSpeed = 0.05f;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = 400;
            main.gravityModifier = -0.01f; // very slight float
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = ps.emission; em.rateOverTime = rate;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = boxSize;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(rise * 0.5f, rise);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.material = mat;
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        static Material SporeMaterial()
        {
            var dot = SporeTexture();
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            string path = $"{MatDir}/HiveSpore.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(sh) { name = "HiveSpore" };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != sh) m.shader = sh;
            m.SetTexture("_BaseMap", dot);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);   // transparent
            m.SetFloat("_Blend", 2f);     // additive glow
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material HazeMaterial()
        {
            var dot = HazeTexture();
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            string path = $"{MatDir}/HiveGroundHaze.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(sh) { name = "HiveGroundHaze" };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != sh) m.shader = sh;
            m.SetTexture("_BaseMap", dot);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f); // alpha blend, not additive
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Texture2D SporeTexture()
        {
            string path = $"{MatDir}/spore_dot.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "spore_dot", wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) * 2f;       // 0 center .. 1 edge
                    float a = Mathf.Clamp01(1f - dist);
                    a = a * a;                                             // soft falloff
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        static Texture2D HazeTexture()
        {
            string path = $"{MatDir}/haze_soft_disc.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "haze_soft_disc", wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    float a = Mathf.SmoothStep(1f, 0f, dist);
                    a *= a * 0.6f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        // -------------------------------------------------------------------- fog
        static void SetupFog(bool _)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.042f);
            RenderSettings.fogDensity = 0.022f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.052f);
        }

        // ----------------------------------------------------------- post process
        static void BuildPostProcessing(bool _)
        {
            EnsureMaterials();
            var group = Section("PostProcessing");
            var profile = HorrorPostProfile();

            var go = new GameObject("HiveHorrorVolume");
            go.transform.SetParent(group, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 25f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
        }

        static VolumeProfile HorrorPostProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(HorrorProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "HiveHorrorPostProfile";
                AssetDatabase.CreateAsset(profile, HorrorProfilePath);
            }

            SetVolumeComponent(profile, out Vignette vignette);
            vignette.active = true;
            vignette.intensity.Override(0.46f);
            vignette.smoothness.Override(0.58f);
            vignette.color.Override(Color.black);

            SetVolumeComponent(profile, out ColorAdjustments color);
            color.active = true;
            color.postExposure.Override(-0.45f);
            color.contrast.Override(32f);
            color.saturation.Override(-34f);
            color.colorFilter.Override(new Color(0.68f, 0.82f, 0.66f));

            SetVolumeComponent(profile, out Bloom bloom);
            bloom.active = true;
            bloom.threshold.Override(1.15f);
            bloom.intensity.Override(0.22f);
            bloom.scatter.Override(0.45f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        static void SetVolumeComponent<T>(VolumeProfile profile, out T component) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out component))
                component = profile.Add<T>(true);
        }

        // --------------------------------------------------------------- geometry
        static GameObject Blob(Transform parent, Vector3 worldPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            StripCollider(go);
            go.name = "Growth";
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject Capsule(Transform parent, Vector3 worldPos, Vector3 scale, Quaternion rot, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            StripCollider(go);
            go.name = "Capsule";
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        // A thin cylinder between two points — used for web strands.
        static void Strand(Transform parent, Vector3 a, Vector3 b, float thickness, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(go);
            go.name = "Web";
            go.transform.SetParent(parent, false);
            Vector3 mid = (a + b) * 0.5f;
            Vector3 dir = b - a;
            float len = dir.magnitude;
            go.transform.position = mid;
            if (len > 1e-4f) go.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            go.transform.localScale = new Vector3(thickness, len * 0.5f, thickness);
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // A cluster of overlapping spheres flattened against a wall/ceiling/floor.
        static void Cluster(Transform parent, Vector3 center, Vector3 normal, float radius, int count,
                            Material body, Material accent, System.Random rng)
        {
            var holder = new GameObject("GrowthCluster");
            holder.transform.SetParent(parent, false);
            holder.transform.position = center;
            normal = normal.normalized;
            // tangent basis on the surface
            Vector3 t1 = Mathf.Abs(normal.y) > 0.9f ? Vector3.right : Vector3.Cross(normal, Vector3.up).normalized;
            Vector3 t2 = Vector3.Cross(normal, t1).normalized;

            for (int i = 0; i < count; i++)
            {
                float u = (float)(rng.NextDouble() - 0.5) * radius * 2f;
                float v = (float)(rng.NextDouble() - 0.5) * radius * 2f;
                float push = (float)rng.NextDouble() * radius * 0.4f;     // bulge off surface
                Vector3 p = center + t1 * u + t2 * v + normal * push;
                float s = radius * (0.45f + (float)rng.NextDouble() * 0.7f);
                // flatten along the surface normal so it hugs the wall
                Vector3 scale = (t1 * s + t2 * s + normal * (s * 0.5f));
                scale = new Vector3(Mathf.Abs(scale.x) + s * 0.3f, Mathf.Abs(scale.y) + s * 0.3f, Mathf.Abs(scale.z) + s * 0.3f);
                var mat = rng.NextDouble() < 0.25 ? accent : body;
                Blob(holder.transform, p, scale, mat);
            }
        }

        // ----------------------------------------------------------------- plumbing
        static GameObject FindRoot()
        {
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == RootName) return go;
            return null;
        }

        static GameObject Root()
        {
            var r = FindRoot();
            if (r == null) r = new GameObject(RootName);
            return r;
        }

        // Returns a fresh (cleared) section group under the root.
        static Transform Section(string name)
        {
            var root = Root().transform;
            var existing = root.Find(name);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            return go.transform;
        }

        static void StripCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
        }

        static bool GuardScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != TargetScene)
            {
                // Debug.LogWarning($"[Hive] Active scene is '{scene.name}', expected '{TargetScene}'. Aborting to avoid editing the wrong scene.");
                return false;
            }
            return true;
        }

        static void SaveAndLog(string what)
        {
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            // Debug.Log($"[Hive] {what}.");
        }
    }
}
