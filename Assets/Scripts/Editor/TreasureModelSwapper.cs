// One-shot helper: replaces the procedural cube "Brass_Key" and "Locked_Chest"
// in SampleScene with the imported Worn_Key / Treasure_Chest FBX models.
//
// Usage: in the Unity Editor run  Tools > Horror Decor > Swap Key & Chest Models .
//
// Why a menu command instead of editing the scene file directly: the FBX mesh
// references only exist once Unity has imported the models, so the swap has to
// happen inside the editor. It is reversible -- it keeps the logic objects
// (Key.cs, Lockable, ChestAnimation, Rigidbody, tags) and only replaces the
// visuals, so `git checkout Assets/Scenes/SampleScene.unity` fully reverts it.
//
// Re-runnable: each run clears the previously spawned KeyModel/ChestModel first,
// so you can click the menu again after tweaking the size/orientation constants
// below and it cleanly re-applies.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorrorDecorTools
{
    public static class TreasureModelSwapper
    {
        const string KeyModelPath   = "Assets/Models/Treasure/Worn_Key.fbx";
        const string ChestModelPath = "Assets/Models/Treasure/Treasure_Chest.fbx";
        const string KeyTexPath     = "Assets/Models/Treasure/T_Worn_Key.png";
        const string KeyMatPath     = "Assets/Models/Treasure/Worn_Key.mat";

        const string KeyObjectName   = "Brass_Key";
        const string ChestObjectName = "Locked_Chest";
        const string SwappedMarker   = "__models_swapped";

        // Target largest world-space dimension for each model (uniform scaled to fit).
        const float KeyTargetSize   = 0.5f;
        const float ChestTargetSize = 1.4f;

        // Import orientation fix. Blender-exported FBX often comes in lying on its
        // side (Z-up vs Unity Y-up), so the chest needs a -90 deg pitch to stand
        // upright. Tweak these if a model faces/leans the wrong way after the swap.
        static readonly Vector3 KeyEuler   = new Vector3(0f, 0f, 0f);
        static readonly Vector3 ChestEuler = new Vector3(-90f, 0f, 0f);

        // Lid hinge tuning. The swap lays the lid flat on the base (computing the
        // closed pose from geometry, since the FBX imports the lid standing open)
        // and hinges it along the base's wider top edge, oriented so ChestAnimation's
        // local-X rotation tips the lid straight up and over the back.
        //   HingeAtPositiveZ : which edge is the "back" -- flip if the lid opens
        //                      the wrong way (forward, or down into the chest).
        const bool HingeAtPositiveZ = false;

        [MenuItem("Tools/Horror Decor/Swap Key & Chest Models")]
        public static void Swap()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded)
            {
                // Debug.LogError("[TreasureSwap] No scene loaded.");
                return;
            }

            AssetDatabase.Refresh();

            var keyFbx   = AssetDatabase.LoadAssetAtPath<GameObject>(KeyModelPath);
            var chestFbx = AssetDatabase.LoadAssetAtPath<GameObject>(ChestModelPath);
            // if (keyFbx == null)   Debug.LogError($"[TreasureSwap] Missing model: {KeyModelPath}");
            // if (chestFbx == null) Debug.LogError($"[TreasureSwap] Missing model: {ChestModelPath}");
            if (keyFbx == null || chestFbx == null) return;

            int changed = 0;
            var key   = FindInScene(KeyObjectName);
            var chest = FindInScene(ChestObjectName);

            if (key == null)   { } // Debug.LogWarning($"[TreasureSwap] '{KeyObjectName}' not found in scene.");
            else if (SwapKey(key, keyFbx)) changed++;

            if (chest == null) { } // Debug.LogWarning($"[TreasureSwap] '{ChestObjectName}' not found in scene.");
            else if (SwapChest(chest, chestFbx)) changed++;

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveOpenScenes();
            }
            // Debug.Log($"[TreasureSwap] Done. Swapped {changed} object(s).");
        }

        // Moves ONLY the lid hinge pivot onto the lid's back-bottom edge, leaving
        // the lid exactly where it currently sits in the scene (so a manually
        // adjusted lid position is preserved). Use this instead of re-running the
        // full swap when the lid swings in an arc / appears to travel when opening.
        [MenuItem("Tools/Horror Decor/Fix Chest Lid Pivot")]
        public static void FixLidPivot()
        {
            var chest = FindInScene(ChestObjectName);
            if (chest == null) { /* Debug.LogError($"[TreasureSwap] '{ChestObjectName}' not found."); */ return; }

            var hinge = chest.transform.Find("Chest_LidHinge");
            if (hinge == null) { /* Debug.LogError("[TreasureSwap] 'Chest_LidHinge' not found under the chest."); */ return; }

            var lid = FindDeep(hinge, "ChestLid") ?? (hinge.childCount > 0 ? hinge.GetChild(0) : null);
            if (lid == null) { /* Debug.LogError("[TreasureSwap] No lid found under the hinge."); */ return; }

            // Lid's current world bounds (its manually placed position is respected).
            if (!TryGetWorldBounds(lid.gameObject, out var lb))
            {
                // Debug.LogError("[TreasureSwap] Lid has no renderer to measure.");
                return;
            }

            bool alongX = lb.size.x >= lb.size.z;
            Quaternion newRot = chest.transform.rotation *
                                (alongX ? Quaternion.identity : Quaternion.Euler(0f, -90f, 0f));
            Vector3 newPos = alongX
                ? new Vector3(lb.center.x, lb.min.y, HingeAtPositiveZ ? lb.max.z : lb.min.z)
                : new Vector3(HingeAtPositiveZ ? lb.max.x : lb.min.x, lb.min.y, lb.center.z);

            // Relocate the pivot without moving the lid: detach (keep world pose),
            // move the hinge, reattach (keep world pose).
            lid.SetParent(chest.transform, true);
            hinge.position = newPos;
            hinge.rotation = newRot;
            lid.SetParent(hinge, true);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            // Debug.Log("[TreasureSwap] Lid pivot moved to the lid's back-bottom edge; lid position unchanged.");
        }

        // ---- Key ------------------------------------------------------------
        static bool SwapKey(GameObject key, GameObject fbx)
        {
            // Re-runnable: clear any previous swap so new sizing/orientation applies.
            DestroyChild(key.transform, "KeyModel");
            DestroyChild(key.transform, SwappedMarker);

            // Drop the cube visual; keep Key.cs, Rigidbody, collider, tag, transform.
            DestroyIfPresent(key.GetComponent<MeshFilter>());
            DestroyIfPresent(key.GetComponent<MeshRenderer>());

            // The cube was shaped via a non-uniform parent scale; reset so the
            // imported model is not squashed.
            key.transform.localScale = Vector3.one;

            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            model.name = "KeyModel";
            model.transform.SetParent(key.transform, false);
            model.transform.localRotation = Quaternion.Euler(KeyEuler);

            FitUniform(model, KeyTargetSize);
            CenterOnParentOrigin(model, key.transform);

            // Worn-key texture (chest textures were not provided with the FBX).
            ApplyKeyMaterial(model);

            // Resize the pickup collider to the new mesh.
            if (TryGetLocalBounds(key, out var lb) && lb.size != Vector3.zero)
            {
                var col = key.GetComponent<BoxCollider>();
                if (col == null) col = key.AddComponent<BoxCollider>();
                col.center = lb.center;
                col.size = lb.size;
            }

            new GameObject(SwappedMarker).transform.SetParent(key.transform, false);
            return true;
        }

        static void ApplyKeyMaterial(GameObject model)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(KeyTexPath);
            if (tex == null) { /* Debug.LogWarning($"[TreasureSwap] Missing key texture: {KeyTexPath}"); */ return; }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(KeyMatPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.8f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.5f);
                AssetDatabase.CreateAsset(mat, KeyMatPath);
            }

            foreach (var r in model.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = mat;
        }

        // ---- Chest ----------------------------------------------------------
        static bool SwapChest(GameObject chest, GameObject fbx)
        {
            // Re-runnable: clear any previous swap so new sizing/orientation applies.
            DestroyChild(chest.transform, "ChestModel");
            DestroyChild(chest.transform, SwappedMarker);

            // Remove the procedural cube body + lid; keep Lockable & ChestAnimation.
            DestroyChild(chest.transform, "Chest_Body");
            DestroyChild(chest.transform, "Chest_LidHinge");

            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            model.name = "ChestModel";
            model.transform.SetParent(chest.transform, false);
            model.transform.localRotation = Quaternion.Euler(ChestEuler);
            // Unpack so we can freely reparent the lid out of the FBX hierarchy and
            // keep the saved scene as plain GameObjects (no prefab-instance block).
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            FitUniform(model, ChestTargetSize);
            BaseOnParentFloor(model, chest.transform);

            var anim  = chest.GetComponent<ChestAnimation>();
            var lid   = FindDeep(model.transform, "ChestLid");
            var baseT = FindDeep(model.transform, "ChestBase");
            if (lid != null)
            {
                // The base top rectangle drives everything. The FBX imports the lid
                // in an open/standing pose, so we don't trust it: lay it flat on the
                // base, then hinge it at the back edge.
                TryGetWorldBounds(baseT != null ? baseT.gameObject : model, out var bb);

                // 1) Lay the lid flat: rotate its thinnest mesh axis to point up.
                Vector3 thin = LidThinAxisWorld(lid);
                lid.rotation = Quaternion.FromToRotation(thin, Vector3.up) * lid.rotation;

                // 2) Drop it centered onto the base top, resting just above it.
                TryGetWorldBounds(lid.gameObject, out var lb2);
                Vector3 closedCenter = new Vector3(bb.center.x, bb.max.y + lb2.extents.y, bb.center.z);
                lid.position += closedCenter - lb2.center;

                // 3) Hinge along the wider top edge, at the back, oriented so its
                //    local X runs along that edge -- ChestAnimation rotates about
                //    local X, so the lid tips straight up and over the back.
                bool alongX = bb.size.x >= bb.size.z;
                var hinge = new GameObject("Chest_LidHinge").transform;
                hinge.SetParent(chest.transform, false);
                hinge.rotation = chest.transform.rotation *
                                 (alongX ? Quaternion.identity : Quaternion.Euler(0f, -90f, 0f));
                if (alongX)
                    hinge.position = new Vector3(bb.center.x, bb.max.y, HingeAtPositiveZ ? bb.max.z : bb.min.z);
                else
                    hinge.position = new Vector3(HingeAtPositiveZ ? bb.max.x : bb.min.x, bb.max.y, bb.center.z);

                lid.SetParent(hinge, true);     // lid is now closed; Open() tips it up
                if (anim != null) anim.lidHinge = hinge;
            }
            else
            {
                // Debug.LogWarning("[TreasureSwap] 'ChestLid' node not found in chest FBX; lid animation not wired.");
            }

            // Interaction raycast needs a collider that resolves to Lockable via
            // GetComponentInParent. Size it to the base (the lid is now under the hinge).
            if (TryGetLocalBounds(model, out var lb) && lb.size != Vector3.zero)
            {
                var col = model.AddComponent<BoxCollider>();
                col.center = lb.center;
                col.size = lb.size;
            }

            new GameObject(SwappedMarker).transform.SetParent(chest.transform, false);
            return true;
        }

        // World-space direction of the lid's thinnest dimension (its face normal),
        // used to lay the lid flat regardless of how the FBX authored its pose.
        static Vector3 LidThinAxisWorld(Transform lid)
        {
            var mf = lid.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return Vector3.up;
            Vector3 e = mf.sharedMesh.bounds.extents;
            Vector3 axis = (e.x <= e.y && e.x <= e.z) ? Vector3.right
                         : (e.y <= e.z) ? Vector3.up : Vector3.forward;
            return mf.transform.TransformDirection(axis).normalized;
        }

        // ---- shared geometry helpers (same approach as HorrorDecorPlacer) ----
        static void FitUniform(GameObject go, float targetSize)
        {
            if (TryGetWorldBounds(go, out var b) && b.size != Vector3.zero)
            {
                float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (maxDim > 1e-5f)
                    go.transform.localScale *= targetSize / maxDim;
            }
        }

        // Shift so the model's world bounds center sits on the parent's origin.
        static void CenterOnParentOrigin(GameObject go, Transform parent)
        {
            if (!TryGetWorldBounds(go, out var b)) return;
            go.transform.position += parent.position - b.center;
        }

        // Shift so the model is centered on x/z and its base rests at parent y.
        static void BaseOnParentFloor(GameObject go, Transform parent)
        {
            if (!TryGetWorldBounds(go, out var b)) return;
            Vector3 p = parent.position;
            Vector3 delta = new Vector3(p.x - b.center.x,
                                        p.y - (b.center.y - b.extents.y),
                                        p.z - b.center.z);
            go.transform.position += delta;
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

        static GameObject FindInScene(string name)
        {
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == name) return go;
            return null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root)
            {
                var found = FindDeep(c, name);
                if (found != null) return found;
            }
            return null;
        }

        static void DestroyChild(Transform parent, string name)
        {
            var c = parent.Find(name);
            if (c != null) Object.DestroyImmediate(c.gameObject);
        }

        static void DestroyIfPresent(Object o)
        {
            if (o != null) Object.DestroyImmediate(o);
        }
    }
}
