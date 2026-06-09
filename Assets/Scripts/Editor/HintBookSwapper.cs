// One-shot helper for the Room 1 hint flow.
//
//   Swaps the flat "Note_Page" prop on the pedestal for the imported open-book
//   FBX, keeping its Readable / InteractableGlow / collider / tag so it still
//   reads exactly the same -- only the visual changes.
//
// (The inspectable floating "Code_Note" that the puzzle-block slot reveals lives
// directly in the scene now, so this command no longer touches it.)
//
// Usage: in the Unity Editor run  Tools > Horror Decor > Swap Note -> Book .
//
// Why a menu command instead of editing the scene file directly: the book FBX mesh
// references only exist once Unity has imported the model, so the swap has to happen
// inside the editor. It is reversible -- the logic components are kept and only the
// visual is swapped, so `git checkout Assets/Scenes/SampleScene.unity` fully reverts it.
//
// Re-runnable: each run clears the previously spawned BookModel first, so you can
// click the menu again after tweaking the constants below.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorrorDecorTools
{
    public static class HintBookSwapper
    {
        const string BookModelPath  = "Assets/Models/Book/Book.fbx";
        const string BookTexPath    = "Assets/Models/Book/T_Book.jpg";
        const string BookNormalPath = "Assets/Models/Book/T_Book_Normal.jpg";
        const string BookMatPath    = "Assets/Models/Book/Book.mat";

        const string NoteObjectName = "Note_Page";

        // Largest world-space dimension the book is scaled to (it sits on the pedestal).
        const float BookTargetSize = 0.45f;

        // Import orientation fix. The Maya-exported FBX often comes in on its side or
        // upside down. Tweak this if the book imports leaning/face-down, then re-run.
        static readonly Vector3 BookEuler = new Vector3(0f, 0f, 0f);

        [MenuItem("Tools/Horror Decor/Swap Note -> Book")]
        public static void Run()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) { /* Debug.LogError("[HintBook] No scene loaded."); */ return; }

            AssetDatabase.Refresh();

            var bookFbx = AssetDatabase.LoadAssetAtPath<GameObject>(BookModelPath);
            if (bookFbx == null) { /* Debug.LogError($"[HintBook] Missing model: {BookModelPath}"); */ return; }

            int changed = 0;

            var note = FindInScene(NoteObjectName);
            if (note == null) { } // Debug.LogWarning($"[HintBook] '{NoteObjectName}' not found in scene.");
            else if (SwapNoteToBook(note, bookFbx)) changed++;

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveOpenScenes();
            }
            // Debug.Log($"[HintBook] Done. Applied {changed} change(s).");
        }

        // ---- Note -> Book ---------------------------------------------------
        static bool SwapNoteToBook(GameObject note, GameObject fbx)
        {
            // Re-runnable: clear any previous swap so new sizing/orientation applies.
            DestroyChild(note.transform, "BookModel");

            // Drop the flat-page cube visual; keep Readable, InteractableGlow, tag.
            DestroyIfPresent(note.GetComponent<MeshFilter>());
            DestroyIfPresent(note.GetComponent<MeshRenderer>());

            // The page was a flattened cube (non-uniform parent scale); reset so the
            // imported book is not squashed.
            note.transform.localScale = Vector3.one;

            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            model.name = "BookModel";
            model.transform.SetParent(note.transform, false);
            model.transform.localRotation = Quaternion.Euler(BookEuler);

            FitUniform(model, BookTargetSize);
            CenterOnParentOrigin(model, note.transform);

            ApplyBookMaterial(model);

            // Resize the interaction collider to the new mesh so the read raycast hits.
            if (TryGetLocalBounds(note, out var lb) && lb.size != Vector3.zero)
            {
                var col = note.GetComponent<BoxCollider>();
                if (col == null) col = note.AddComponent<BoxCollider>();
                col.center = lb.center;
                col.size = lb.size;
            }
            return true;
        }

        static void ApplyBookMaterial(GameObject model)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(BookMatPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(BookTexPath);
                var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(BookNormalPath);
                if (tex != null)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                }
                if (nrm != null && mat.HasProperty("_BumpMap"))
                {
                    mat.EnableKeyword("_NORMALMAP");
                    mat.SetTexture("_BumpMap", nrm);
                }
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);
                AssetDatabase.CreateAsset(mat, BookMatPath);
            }
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = mat;
        }

        // ---- shared geometry helpers (same approach as TreasureModelSwapper) ----
        static void FitUniform(GameObject go, float targetSize)
        {
            if (TryGetWorldBounds(go, out var b) && b.size != Vector3.zero)
            {
                float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (maxDim > 1e-5f) go.transform.localScale *= targetSize / maxDim;
            }
        }

        static void CenterOnParentOrigin(GameObject go, Transform parent)
        {
            if (!TryGetWorldBounds(go, out var b)) return;
            go.transform.position += parent.position - b.center;
        }

        static bool TryGetWorldBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return false;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
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
            {
                var found = FindDeep(go.transform, name);
                if (found != null) return found.gameObject;
            }
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
