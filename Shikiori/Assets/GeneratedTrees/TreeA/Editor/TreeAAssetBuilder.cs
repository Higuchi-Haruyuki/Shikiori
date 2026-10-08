using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;

public static class TreeAAssetBuilder
{
    const string Folder = "Assets/GeneratedTrees/TreeA";
    [Serializable] public class MeshData { public string name; public float[] vertices; public float[] normals; public int[] slot0; public int[] slot1; }
    [Serializable] public class CollisionData { public float[] center; public float[] size; }
    [Serializable] public class TreeData { public float height; public MeshData wood; public MeshData foliage; public CollisionData[] colliders; }
    static Vector3 Vec(float[] a, int i = 0) => new Vector3(a[i], a[i+1], a[i+2]);
    static Mesh MakeMesh(MeshData data)
    {
        var mesh = new Mesh { name = "Tree_A_" + data.name };
        var positions = new Vector3[data.vertices.Length/3];
        var normals = new Vector3[positions.Length];
        for(int i=0;i<positions.Length;i++) { positions[i]=Vec(data.vertices,i*3); normals[i]=Vec(data.normals,i*3); }
        mesh.vertices=positions; mesh.normals=normals; mesh.uv = new Vector2[positions.Length];
        mesh.subMeshCount=2; mesh.SetTriangles(data.slot0,0); mesh.SetTriangles(data.slot1,1);
        mesh.RecalculateBounds();
        var assetPath=Folder+"/Meshes/"+mesh.name+".asset";
        if(File.Exists(assetPath)) throw new InvalidOperationException("Existing asset would be overwritten: "+assetPath);
        AssetDatabase.CreateAsset(mesh,assetPath);
        return mesh;
    }
    static Material MakeMaterial(string name, Vector3 linearRGB, bool hidden = false)
    {
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        if(shader==null) throw new InvalidOperationException("URP Lit shader is unavailable.");
        var m=new Material(shader) { name=name, enableInstancing=true };
        Color c=new Color(linearRGB.x,linearRGB.y,linearRGB.z,hidden?0:1).gamma; c.a=hidden?0:1;
        m.SetColor("_BaseColor",c); m.SetColor("_Color",c);
        m.SetFloat("_Smoothness",0.12f); m.SetFloat("_Metallic",0);
        m.SetFloat("_Surface",0); m.SetFloat("_Cull",2);
        m.SetFloat("_AlphaClip",hidden?1:0); m.SetFloat("_Cutoff",0.5f);
        if(hidden) { m.EnableKeyword("_ALPHATEST_ON"); m.SetOverrideTag("RenderType","TransparentCutout"); m.renderQueue=(int)RenderQueue.AlphaTest; }
        AssetDatabase.CreateAsset(m,Folder+"/Materials/"+name+".mat");
        return m;
    }
    static GameObject Child(GameObject root,string name,Mesh mesh,Material[] materials)
    {
        var obj=new GameObject(name); obj.transform.SetParent(root.transform,false);
        obj.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=obj.AddComponent<MeshRenderer>(); renderer.sharedMaterials=materials;
        renderer.shadowCastingMode=ShadowCastingMode.On; renderer.receiveShadows=true;
        return obj;
    }
    [MenuItem("Tools/Generated Trees/Build Tree A Assets")]
    public static void Build()
    {
        if(File.Exists(Folder+"/Tree_A.prefab")) throw new InvalidOperationException("Tree_A.prefab already exists. Build is for a fresh copy of the source assets.");
        Directory.CreateDirectory(Folder+"/Meshes"); Directory.CreateDirectory(Folder+"/Materials");
        AssetDatabase.Refresh();
        var data=JsonUtility.FromJson<TreeData>(File.ReadAllText(Folder+"/Source/Tree_A.meshdata.json"));
        var wood=MakeMesh(data.wood); var leaves=MakeMesh(data.foliage);
        var bark=MakeMaterial("Bark_Normal",new Vector3(.27f,.19f,.115f));
        var branch=MakeMaterial("BranchTop_Normal",new Vector3(.33f,.245f,.16f));
        var leaf=MakeMaterial("Foliage_Normal",new Vector3(.19f,.265f,.12f));
        var tip=MakeMaterial("FoliageTip_Normal",new Vector3(.265f,.34f,.17f));
        var winterBark=MakeMaterial("Bark_Winter",new Vector3(.32f,.245f,.175f));
        var snow=MakeMaterial("BranchTop_Winter",new Vector3(.88f,.90f,.87f));
        var invisible=MakeMaterial("Foliage_Winter_Hidden",Vector3.zero,true);
        var root=new GameObject("Tree_A");
        GameObject prefab;
        try
        {
            Child(root,"Wood",wood,new[]{bark,branch});
            Child(root,"Foliage",leaves,new[]{leaf,tip});
            var collisions=new GameObject("Collision"); collisions.transform.SetParent(root.transform,false);
            for(int i=0;i<data.colliders.Length;i++)
            {
                var c=new GameObject(i==0?"Trunk":"CrownTier_"+i); c.transform.SetParent(collisions.transform,false);
                var box=c.AddComponent<BoxCollider>(); box.center=Vec(data.colliders[i].center); box.size=Vec(data.colliders[i].size);
            }
            prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Tree_A.prefab");
            if(prefab==null) throw new InvalidOperationException("Prefab save failed.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        foreach(var shaderAsset in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
        {
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(shaderAsset));
            if(pipeline==null) continue;
            GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=pipeline;
            pipeline.shadowDistance=20;
            break;
        }
        RenderSettings.ambientMode=AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.22f,.23f,.21f);
        var green=(GameObject)PrefabUtility.InstantiatePrefab(prefab); green.name="Tree_A_Green_Preview"; green.transform.position=new Vector3(-.85f,0,0);
        var winter=(GameObject)PrefabUtility.InstantiatePrefab(prefab); winter.name="Tree_A_Snow_Preview"; winter.transform.position=new Vector3(.85f,0,0);
        winter.transform.Find("Wood").GetComponent<MeshRenderer>().sharedMaterials=new[]{winterBark,snow};
        winter.transform.Find("Foliage").GetComponent<MeshRenderer>().sharedMaterials=new[]{invisible,invisible};
        var ground=GameObject.CreatePrimitive(PrimitiveType.Plane); ground.name="Preview Ground"; ground.transform.localScale=Vector3.one*3;
        var gm=new Material(Shader.Find("Universal Render Pipeline/Lit")); gm.SetColor("_BaseColor",new Color(.55f,.53f,.49f)); gm.SetFloat("_Smoothness",0);
        AssetDatabase.CreateAsset(gm,Folder+"/Materials/PreviewGround.mat"); ground.GetComponent<Renderer>().sharedMaterial=gm;
        var light=new GameObject("Key Light").AddComponent<Light>(); light.type=LightType.Directional; light.color=Color.white; light.intensity=.9f; light.shadows=LightShadows.Soft; light.transform.rotation=Quaternion.Euler(42,-30,0);
        var fill=new GameObject("Fill Light").AddComponent<Light>(); fill.type=LightType.Directional; fill.intensity=.18f; fill.transform.rotation=Quaternion.Euler(55,135,0);
        var camera=new GameObject("Preview Camera").AddComponent<Camera>(); camera.transform.position=new Vector3(4.3f,3.6f,-8); camera.transform.LookAt(new Vector3(0,1.25f,0)); camera.orthographic=true; camera.orthographicSize=1.62f; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.67f,.65f,.60f); camera.tag="MainCamera";
        var cameraData=camera.gameObject.AddComponent<UniversalAdditionalCameraData>(); cameraData.renderPostProcessing=false;
        EditorSceneManager.SaveScene(scene,Folder+"/Tree_A_Comparison.unity");
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Validate();
        var output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../output/TreeA/Tree_A.unitypackage"));
        AssetDatabase.ExportPackage(Folder,output,ExportPackageOptions.Recurse);
        Debug.Log("TREE_A_BUILD_OK: "+output);
    }
    static bool ContainsWithTolerance(Bounds b, Vector3 v) { b.Expand(0.0002f); return b.Contains(v); }
    public static void Validate()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Tree_A.prefab");
        if(prefab==null) throw new Exception("Missing prefab");
        var renderers=prefab.GetComponentsInChildren<MeshRenderer>();
        if(renderers.Length!=2) throw new Exception("Expected two renderers");
        if(prefab.GetComponentsInChildren<Collider>().Length!=11) throw new Exception("Expected trunk + ten crown collision volumes");
        int triangleCount=0;
        foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>())
        {
            var mesh=filter.sharedMesh;
            if(mesh==null || mesh.subMeshCount!=2) throw new Exception("Invalid mesh");
            if(mesh.normals.Length!=mesh.vertexCount) throw new Exception("Missing normals");
            triangleCount+=mesh.triangles.Length/3;
            for(int v=0;v<mesh.vertexCount;v++) if(!float.IsFinite(mesh.vertices[v].x)) throw new Exception("Nonfinite mesh");
        }
        foreach(var r in renderers) foreach(var m in r.sharedMaterials) if(m==null || m.shader.name!="Universal Render Pipeline/Lit") throw new Exception("Incorrect material");
        var hidden=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Foliage_Winter_Hidden.mat");
        if(!hidden.IsKeywordEnabled("_ALPHATEST_ON") || hidden.GetColor("_BaseColor").a!=0) throw new Exception("Winter foliage must be completely clipped");
        if(AssetDatabase.FindAssets("t:Prefab",new[]{Folder}).Length!=1) throw new Exception("Only one prefab is allowed");
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            var rs=instance.GetComponentsInChildren<Renderer>(); var bounds=rs[0].bounds; foreach(var r in rs) bounds.Encapsulate(r.bounds);
            if(Mathf.Abs(bounds.size.y-2.5f)>.025f) throw new Exception("Unexpected height "+bounds.size.y);
            Physics.SyncTransforms();
            if(!Physics.Raycast(new Vector3(0,1.1f,-2),Vector3.forward,out var hit,4)) throw new Exception("Trunk collision did not work");
            var cols=instance.GetComponentsInChildren<BoxCollider>();
            foreach(var filter in instance.GetComponentsInChildren<MeshFilter>())
                foreach(var v in filter.sharedMesh.vertices)
                    if(!cols.Any(c=>ContainsWithTolerance(c.bounds,filter.transform.TransformPoint(v)))) throw new Exception("Tree vertex outside collision bounds");
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
        var report="Unity "+Application.unityVersion+"\nPrefab count: 1\nHeight: 2.5\nTriangles: "+triangleCount+"\nRenderers: 2\nCollider count: 11\nSeason materials: PASS\nMesh normals/indices: PASS\nTrunk raycast: PASS\nWhole mesh collision bounds: PASS\n";
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../output/TreeA/unity-validation.txt")),report);
        Debug.Log("TREE_A_VALIDATION_OK\n"+report);
    }
}
