using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class TreeAPreviewCapture
{
    public static void Capture()
    {
        const string folder="Assets/GeneratedTrees/TreeA";
        EditorSceneManager.OpenScene(folder+"/Tree_A_Comparison.unity");
        var winter=GameObject.Find("Tree_A_Snow_Preview");
        if(winter==null) throw new Exception("Winter preview missing");
        var wood=winter.transform.Find("Wood").GetComponent<MeshRenderer>();
        var foliage=winter.transform.Find("Foliage").GetComponent<MeshRenderer>();
        wood.sharedMaterials=new[]{AssetDatabase.LoadAssetAtPath<Material>(folder+"/Materials/Bark_Winter.mat"),AssetDatabase.LoadAssetAtPath<Material>(folder+"/Materials/BranchTop_Winter.mat")};
        var hidden=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Materials/Foliage_Winter_Hidden.mat"); foliage.sharedMaterials=new[]{hidden,hidden};
        PrefabUtility.RecordPrefabInstancePropertyModifications(wood); PrefabUtility.RecordPrefabInstancePropertyModifications(foliage);
        EditorSceneManager.SaveScene(winter.scene);
        var data=JsonUtility.FromJson<TreeAAssetBuilder.TreeData>(File.ReadAllText(folder+"/Source/Tree_A.meshdata.json"));
        foreach(var md in new[]{data.wood,data.foliage})
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/Meshes/Tree_A_"+md.name+".asset");
            var vs=new Vector3[md.vertices.Length/3]; var ns=new Vector3[vs.Length];
            for(int i=0;i<vs.Length;i++){int j=i*3;vs[i]=new Vector3(md.vertices[j],md.vertices[j+1],md.vertices[j+2]);ns[i]=new Vector3(md.normals[j],md.normals[j+1],md.normals[j+2]);}
            mesh.vertices=vs;mesh.normals=ns;mesh.SetTriangles(md.slot0,0);mesh.SetTriangles(md.slot1,1);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        }
        AssetDatabase.SaveAssets();
        RenderSettings.ambientLight=new Color(.22f,.23f,.21f);
        var key=GameObject.Find("Key Light").GetComponent<Light>(); key.intensity=.9f; key.color=Color.white;
        GameObject.Find("Fill Light").GetComponent<Light>().intensity=.18f;
        var ground=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Materials/PreviewGround.mat"); ground.SetColor("_BaseColor",new Color(.55f,.53f,.49f)); EditorUtility.SetDirty(ground);
        var pcPipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        if(pcPipeline!=null) {GraphicsSettings.defaultRenderPipeline=pcPipeline;QualitySettings.renderPipeline=pcPipeline;pcPipeline.shadowDistance=10;pcPipeline.msaaSampleCount=4;EditorUtility.SetDirty(pcPipeline);}
        EditorSceneManager.SaveScene(winter.scene);
        AssetDatabase.SaveAssets();
        TreeAAssetBuilder.Validate();
        var camera=GameObject.Find("Preview Camera").GetComponent<Camera>();
        var rt=new RenderTexture(1400,1100,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB); rt.antiAliasing=4; rt.Create();
        var request=new UniversalRenderPipeline.SingleCameraRequest { destination=rt };
        if(!RenderPipeline.SupportsRenderRequest(camera,request)) throw new Exception("Render request unsupported");
        RenderPipeline.SubmitRenderRequest(camera,request);
        RenderPipeline.SubmitRenderRequest(camera,request);
        var previous=RenderTexture.active; RenderTexture.active=rt;
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); image.Apply(); RenderTexture.active=previous;
        var output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../output/TreeA"));
        File.WriteAllBytes(Path.Combine(output,"Tree_A_Unity_preview.png"),image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
        AssetDatabase.SaveAssets();
        AssetDatabase.ExportPackage(folder,Path.Combine(output,"Tree_A.unitypackage"),ExportPackageOptions.Recurse);
        Debug.Log("TREE_A_RENDER_OK");
    }
}
