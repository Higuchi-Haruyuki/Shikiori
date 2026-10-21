using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームシーンとリザルトシーンの猫を、修正版の猫モデル(glb)に差し替えるエディタツール。
/// メニュー「Tools/Shikiori/猫を差し替え/～」から、使いたいモデルを選んで実行する。
/// 何度実行してもよく、別のモデルへ差し替え直すこともできる。
///
/// やること
///   1. そのglbのアニメーションを使うAnimatorControllerを作る(cat.controllerを複製してモーションだけ差し替え)
///   2. Cat.prefabと同じコンポーネント構成で、そのglbを元にしたプレハブを作る(作成済みならそれを使う)
///   3. 各シーンの猫を置き換え、シーン内からの参照もつなぎ直す
///      シーン側で外したコンポーネント・付け足したコンポーネントも引き継ぐ
///      (リザルトシーンの猫はPlayerControllerを外してResultSceneCatWanderで歩かせているので、それを保つ)
/// </summary>
public static class CatFixReplacer
{
    private const string OldModelPath = "Assets/Higuchi/cat_rigged_anim.glb";
    private const string OldPrefabPath = "Assets/Higuchi/Cat.prefab";
    private const string OldControllerPath = "Assets/Mizukami/sozai/anime/cat.controller";
    private const string MenuRoot = "Tools/Shikiori/猫を差し替え/";

    // 猫を差し替えるシーン
    private static readonly string[] TargetScenePaths =
    {
        "Assets/Higuchi/Scene/SeasonChangeTest.unity", // ゲームシーン
        "Assets/Higuchi/Scene/ResultScene.unity",      // リザルトシーン
    };

    // 差し替え先の猫モデルと、それ用に作るコントローラー・プレハブの置き場所
    private struct CatModel
    {
        public string ModelPath;
        public string ControllerPath;
        public string PrefabPath;
    }

    private static readonly CatModel RiggedAnimFixed = new CatModel
    {
        ModelPath = "Assets/Higuchi/cat_anim_fix/cat_rigged_anim_fixed.glb",
        ControllerPath = "Assets/Higuchi/cat_anim_fix/cat_rigged_anim_fixed.controller",
        PrefabPath = "Assets/Higuchi/Cat_RiggedAnimFixed.prefab",
    };

    private static readonly CatModel WalkFixed = new CatModel
    {
        ModelPath = "Assets/Higuchi/cat_anim_fix/cat_rigged_anim_walk_fixed.glb",
        ControllerPath = "Assets/Higuchi/cat_anim_fix/cat_fixed.controller",
        PrefabPath = "Assets/Higuchi/Cat_Fixed.prefab",
    };

    [MenuItem(MenuRoot + "cat_rigged_anim_fixed")]
    public static void ReplaceWithRiggedAnimFixed() => Replace(RiggedAnimFixed);

    [MenuItem(MenuRoot + "cat_rigged_anim_walk_fixed")]
    public static void ReplaceWithWalkFixed() => Replace(WalkFixed);

    private static void Replace(CatModel model)
    {
        // 開いているシーンに未保存の変更があれば先に保存してもらう
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var controller = CreateFixedController(model);
        if (!controller) return;

        var fixedPrefab = CreateFixedPrefab(controller, model);
        if (!fixedPrefab) return;

        foreach (var scenePath in TargetScenePaths)
        {
            ReplaceInScene(scenePath, fixedPrefab);
        }
    }

    // 1. cat.controllerを複製し、各ステートのモーションをfix版glbの同名クリップに差し替える
    private static AnimatorController CreateFixedController(CatModel model)
    {
        var fixedClips = AssetDatabase.LoadAllAssetsAtPath(model.ModelPath)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__"))
            .ToList();
        if (fixedClips.Count == 0)
        {
            Debug.LogError($"[CatFixReplacer] {model.ModelPath} にアニメーションクリップが見つかりません");
            return null;
        }

        if (!AssetDatabase.LoadAssetAtPath<AnimatorController>(model.ControllerPath))
        {
            AssetDatabase.CopyAsset(OldControllerPath, model.ControllerPath);
        }
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(model.ControllerPath);

        foreach (var layer in controller.layers)
        {
            foreach (var child in layer.stateMachine.states)
            {
                var state = child.state;
                if (!state.motion) continue;

                // 元のクリップ名(Cat_Walkなど)と同じ名前のクリップを探す
                var clip = fixedClips.FirstOrDefault(c => c.name == state.motion.name)
                        ?? fixedClips.FirstOrDefault(c => c.name.EndsWith(state.motion.name));
                if (!clip)
                {
                    Debug.LogWarning($"[CatFixReplacer] ステート {state.name} に対応するクリップ({state.motion.name})がfix版にありません。元のまま残します");
                    continue;
                }
                state.motion = clip;
            }
        }
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    // 2. glbを元にCat.prefabと同じ構成のプレハブを作る
    private static GameObject CreateFixedPrefab(AnimatorController controller, CatModel model)
    {
        // 作成済みのプレハブは作り直さない(作り直すと中のIDが変わり、シーンからの参照が切れるため)
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(model.PrefabPath);
        if (existing)
        {
            Debug.Log($"[CatFixReplacer] 作成済みの {model.PrefabPath} を使います");
            return existing;
        }

        var oldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OldPrefabPath);
        var fixedModel = AssetDatabase.LoadAssetAtPath<GameObject>(model.ModelPath);
        var oldModel = AssetDatabase.LoadAssetAtPath<GameObject>(OldModelPath);
        if (!oldPrefab || !fixedModel || !oldModel)
        {
            Debug.LogError("[CatFixReplacer] Cat.prefab または猫のglbが見つかりません");
            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(fixedModel);
        try
        {
            instance.tag = oldPrefab.tag;
            instance.layer = oldPrefab.layer;

            // モデルの大きさが変わっていても、見た目の高さが元の猫と同じになるように拡大率を合わせる
            float oldHeight = GetHeight(oldModel);
            float newHeight = GetHeight(fixedModel);
            float ratio = (oldHeight > 0f && newHeight > 0f) ? oldHeight / newHeight : 1f;
            instance.transform.localScale = oldPrefab.transform.localScale * ratio;

            // Animatorの設定を元の猫からコピーし、コントローラーだけfix版にする
            var oldAnimator = oldPrefab.GetComponent<Animator>();
            var animator = GetOrAddComponent(instance, typeof(Animator)) as Animator;
            if (oldAnimator)
            {
                animator.applyRootMotion = oldAnimator.applyRootMotion;
                animator.updateMode = oldAnimator.updateMode;
                animator.cullingMode = oldAnimator.cullingMode;
            }
            animator.runtimeAnimatorController = controller;

            // PlayerController・コライダー・Rigidbody・AudioSourceなどを同じ設定でコピーする
            foreach (var source in oldPrefab.GetComponents<Component>())
            {
                if (source is Transform || source is Animator) continue;
                var copy = GetOrAddComponent(instance, source.GetType());
                EditorUtility.CopySerialized(source, copy);
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, model.PrefabPath);
            Debug.Log($"[CatFixReplacer] {model.PrefabPath} を作成しました (拡大率 x{ratio:F3})");
            return prefab;
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    // UnityのGetComponentは「偽のnull」を返すことがあり ?? が使えないので、明示的にチェックする
    private static Component GetOrAddComponent(GameObject target, System.Type type)
    {
        var component = target.GetComponent(type);
        return component ? component : target.AddComponent(type);
    }

    // モデルのレンダラー全体の高さを測る(アセットのままだとboundsが正しく取れないので一時的にシーンに出す)
    private static float GetHeight(GameObject model)
    {
        var temp = Object.Instantiate(model);
        try
        {
            temp.transform.localScale = Vector3.one;
            var renderers = temp.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0f;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds.size.y;
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }
    }

    // 3. シーンの猫を置き換え、シーン内の参照をつなぎ直す
    private static void ReplaceInScene(string scenePath, GameObject fixedPrefab)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // PlayerController か ResultSceneCatWander が付いたプレハブを「主役の猫」とみなす
        // (元のCat.prefabでも、差し替え済みの猫でもOK)
        var oldCats = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(t => t.gameObject)
            .Where(go => PrefabUtility.IsAnyPrefabInstanceRoot(go)
                      && (go.GetComponent<PlayerController>() || go.GetComponent<ResultSceneCatWander>()))
            .ToList();
        if (oldCats.Count == 0)
        {
            Debug.LogWarning($"[CatFixReplacer] {scenePath} に猫が見つかりません");
            return;
        }
        if (oldCats.All(go => PrefabUtility.GetCorrespondingObjectFromSource(go) == fixedPrefab))
        {
            Debug.Log($"[CatFixReplacer] {scenePath} の猫はすでに {fixedPrefab.name} です");
            return;
        }

        foreach (var oldCat in oldCats)
        {
            if (PrefabUtility.GetCorrespondingObjectFromSource(oldCat) == fixedPrefab) continue;

            var newCat = (GameObject)PrefabUtility.InstantiatePrefab(fixedPrefab, scene);
            newCat.name = oldCat.name;
            newCat.transform.SetParent(oldCat.transform.parent, false);
            newCat.transform.SetSiblingIndex(oldCat.transform.GetSiblingIndex());
            newCat.transform.SetPositionAndRotation(oldCat.transform.position, oldCat.transform.rotation);

            // シーン上で上書きされていた値(cameraTransformなど)を引き継ぐ
            // シーン側で付け足していたスクリプト(ResultSceneCatWanderなど)は新しい猫にも付ける
            foreach (var oldComponent in oldCat.GetComponents<MonoBehaviour>())
            {
                if (!oldComponent) continue;
                var newComponent = GetOrAddComponent(newCat, oldComponent.GetType());
                EditorUtility.CopySerialized(oldComponent, newComponent);
            }

            // シーン側で外していたスクリプト(リザルトシーンのPlayerControllerなど)は新しい猫からも外す
            foreach (var newComponent in newCat.GetComponents<MonoBehaviour>())
            {
                if (newComponent && !oldCat.GetComponent(newComponent.GetType()))
                {
                    Object.DestroyImmediate(newComponent);
                }
            }

            // 古い猫を参照していたものを新しい猫へ付け替える(CameraManagerのPlayerControllerなど)
            var remap = new Dictionary<Object, Object> { { oldCat, newCat }, { oldCat.transform, newCat.transform } };
            foreach (var oldComponent in oldCat.GetComponents<Component>())
            {
                var newComponent = newCat.GetComponent(oldComponent.GetType());
                if (newComponent) remap[oldComponent] = newComponent;
            }
            RemapReferences(scene, remap, oldCat);

            Object.DestroyImmediate(oldCat);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[CatFixReplacer] {scenePath} の猫を {fixedPrefab.name} に差し替えました");
    }

    private static void RemapReferences(Scene scene, Dictionary<Object, Object> remap, GameObject skipRoot)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (!component || component.transform.IsChildOf(skipRoot.transform)) continue;

                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                bool changed = false;
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var target = property.objectReferenceValue;
                    if (target && remap.TryGetValue(target, out var replacement))
                    {
                        property.objectReferenceValue = replacement;
                        changed = true;
                    }
                }
                if (changed)
                {
                    serialized.ApplyModifiedProperties();
                    Debug.Log($"[CatFixReplacer] 参照を付け替え: {component.gameObject.name} ({component.GetType().Name})");
                }
            }
        }
    }
}
