// Auto-generated helper: places Kenney furniture-kit props into the escape-room
// scene to reinforce the abandoned / forest-hive horror theme described in SPECS.md.
//
// Usage: in the Unity Editor run  Tools > Horror Decor > Place Furniture .
// It also runs once automatically after the assets import (guarded so it never
// duplicates). Everything it spawns lives under a single "HorrorDecor" root, so
// it is trivially reversible: delete that object (or `git checkout` the scene).
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorrorDecorTools
{
    public static class HorrorDecorPlacer
    {
        const string RootName = "HorrorDecor";
        const string TargetScene = "SampleScene"; // auto-run only touches this scene
        const string FurnitureDir = "Assets/Models/Furniture/";
        const int LayoutVersion = 2;              // bump to force the auto-run to rebuild
        const float FloorY = 0f; // both rooms sit on the floor plane at y = 0

        // One placed prop. `size` is the desired largest world-space dimension;
        // the model is scaled uniformly to hit it, which is robust against the
        // wildly different raw import scales in this kit. Floor props are snapped
        // so their base rests on the floor; ceiling props use a fixed Y.
        struct Prop
        {
            public string model;   // fbx file name without extension
            public string label;   // hierarchy name
            public float x, z;     // horizontal placement (world)
            public float rotY;     // yaw in degrees
            public float size;     // target largest dimension (world units)
            public bool ceiling;   // if true, pin to ceilingY instead of floor-snapping
            public float ceilY;
        }

        static Prop Floor(string model, string label, float x, float z, float rotY, float size)
            => new Prop { model = model, label = label, x = x, z = z, rotY = rotY, size = size };

        static Prop Ceil(string model, string label, float x, float z, float rotY, float size, float y)
            => new Prop { model = model, label = label, x = x, z = z, rotY = rotY, size = size, ceiling = true, ceilY = y };

        static List<Prop> BuildLayout()
        {
            var p = new List<Prop>();

            // ---- Room 1 (x in [-8,8], z in [-8,8]) -------------------------------
            // Abandoned research nook: desk + askew chair against the north wall.
            p.Add(Floor("desk",              "R1_Desk",            5.5f,  6.8f, 180f, 1.8f));
            p.Add(Floor("chairDesk",         "R1_DeskChair",      5.4f,  5.5f,  12f, 1.0f));
            p.Add(Floor("trashcan",          "R1_Trashcan",       6.9f,  6.7f,   0f, 0.6f));
            p.Add(Floor("televisionVintage", "R1_OldTV",         -6.7f,  6.4f, 135f, 1.0f));
            p.Add(Floor("coatRackStanding",  "R1_CoatRack",      -7.0f, -1.0f,  90f, 1.9f)); // silhouette by west wall
            p.Add(Floor("lampRoundFloor",    "R1_FloorLamp",     -6.7f, -6.4f,   0f, 1.7f));
            p.Add(Floor("pottedPlant",       "R1_DeadPlant",      6.7f, -6.4f,   0f, 1.3f));
            p.Add(Floor("plantSmall3",       "R1_SmallPlant",    -6.8f,  3.0f,   0f, 0.5f));
            p.Add(Floor("cardboardBoxClosed","R1_Box1",          -6.0f, -6.0f,  20f, 0.8f));
            p.Add(Floor("cardboardBoxOpen",  "R1_Box2",          -4.8f, -6.3f,  35f, 0.8f));
            p.Add(Floor("bench",             "R1_Bench",          7.0f,  1.5f,  90f, 1.6f));

            // Room 2 intentionally left undecorated (per request — Room 1 only).

            return p;
        }

        [MenuItem("Tools/Horror Decor/Place Furniture")]
        public static void PlaceMenu()
        {
            Place(true);
        }

        // Runs automatically once after a domain reload (e.g. when the editor
        // regains focus and recompiles). Idempotent: skips if the props exist.
        [InitializeOnLoadMethod]
        static void AutoPlaceOnce()
        {
            EditorApplication.delayCall += () =>
            {
                var scene = SceneManager.GetActiveScene();
                if (!scene.isLoaded || scene.name != TargetScene) return;
                var root = FindRoot();
                if (root != null && IsCurrentVersion(root)) return; // already placed & up to date
                Place(true); // (re)build — also refreshes a stale older layout
            };
        }

        static GameObject FindRoot()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == RootName) return go;
            return null;
        }

        static string VersionTag => $"__layout_v{LayoutVersion}";

        static bool IsCurrentVersion(GameObject root)
        {
            return root.transform.Find(VersionTag) != null;
        }

        static void Place(bool rebuild)
        {
            AssetDatabase.Refresh();

            // Remove a previous run so the menu command is repeatable & clean.
            var existing = FindRoot();
            if (existing != null)
            {
                if (!rebuild) return;
                Object.DestroyImmediate(existing);
            }

            var root = new GameObject(RootName);
            // Version marker child so the auto-run knows whether to rebuild.
            new GameObject(VersionTag).transform.SetParent(root.transform, false);
            int placed = 0, missing = 0;

            foreach (var prop in BuildLayout())
            {
                var path = FurnitureDir + prop.model + ".fbx";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    // Debug.LogWarning($"[HorrorDecor] Missing model: {path}");
                    missing++;
                    continue;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = prop.label;
                go.transform.SetParent(root.transform, true);
                go.transform.rotation = Quaternion.Euler(0f, prop.rotY, 0f);
                go.transform.localScale = Vector3.one;

                // Scale uniformly so the largest world dimension == prop.size.
                if (TryGetWorldBounds(go, out var b) && b.size != Vector3.zero)
                {
                    float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                    if (maxDim > 1e-5f)
                        go.transform.localScale = Vector3.one * (prop.size / maxDim);
                }

                // Re-measure after scaling, then align center (x,z) and base (y).
                if (TryGetWorldBounds(go, out b))
                {
                    float targetY = prop.ceiling ? prop.ceilY : FloorY + b.size.y * 0.5f;
                    Vector3 delta = new Vector3(prop.x - b.center.x,
                                                targetY - b.center.y,
                                                prop.z - b.center.z);
                    go.transform.position += delta;
                }
                else
                {
                    go.transform.position = new Vector3(prop.x, prop.ceiling ? prop.ceilY : FloorY, prop.z);
                }

                // Solid collision so the player can't walk through the prop.
                // The ceiling fan stays trigger-free of collision (out of reach).
                if (!prop.ceiling && TryGetLocalBounds(go, out var lb) && lb.size != Vector3.zero)
                {
                    var col = go.AddComponent<BoxCollider>();
                    col.center = lb.center;
                    col.size = lb.size;
                }

                placed++;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            // Debug.Log($"[HorrorDecor] Placed {placed} props ({missing} missing) under '{RootName}'.");
        }

        static bool TryGetWorldBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return false;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        // Bounds expressed in `go`'s local space (independent of go's own
        // rotation/scale), suitable for a BoxCollider that tracks the prop.
        // Only considers meshes that are actually RENDERED — some kit models carry
        // stray non-rendered meshes far from the origin which, if included, stretch
        // the collider into a long invisible wall.
        static bool TryGetLocalBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            var renderers = go.GetComponentsInChildren<MeshRenderer>();
            bool any = false;
            var toLocal = go.transform.worldToLocalMatrix;
            foreach (var r in renderers)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var m = toLocal * mf.transform.localToWorldMatrix;
                var mb = mf.sharedMesh.bounds;
                // Transform the 8 corners of the mesh AABB into go-local space.
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1,
                                    (i & 2) == 0 ? -1 : 1,
                                    (i & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                    else bounds.Encapsulate(p);
                }
            }
            return any;
        }
    }
}
