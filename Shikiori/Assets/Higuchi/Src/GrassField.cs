using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class GrassField : MonoBehaviour
{
    // 草を生やしたい地面のRenderer
    [SerializeField] private Renderer _groundRenderer;

    // 草用のMaterial
    // Enable GPU Instancingにチェックを入れること
    [SerializeField] private Material _grassMaterial;

    [SerializeField] private float _bladeHeight = 0.5f;

    [SerializeField] private float _bladeWidth = 0.1f;

    // 草の傾きの最小角度(0-90度)
    [SerializeField] private float _leanMinDegrees = 5.0f;

    // 草の傾きの最大角度(0-90度)
    [SerializeField] private float _leanMaxDegrees = 30.0f;
    // チャンク一つのサイズ
    [SerializeField] private float _chunkSize = 5.0f;

    // チャンク一つあたりの草の本数
    [SerializeField] private int _bladesPerChunk = 1000;

    // 草の描画距離
    [SerializeField] private float _drawDistance = 40.0f;

    [SerializeField] private int _randomSeed = 12345;

    private class Chunk
    {
        // 草一本ごとのPosition, Rotation, Scaleをまとめた行列の配列
        public Matrix4x4[] Matrices;
        // チャンク全体を囲む箱。(カメラに映らかの判定に使う。)
        public Bounds Bounds;
    }

    private readonly List<Chunk> _chunks = new();

    // 草一本のメッシュ(Start()で生成する)
    private Mesh _bladeMesh;

    // カメラの視錐台を表す6つの平面
    private readonly Plane[] _frustumPlanes = new Plane[6];

    private Camera _camera;
    private void Start()
    {
        _camera = Camera.main;
        _bladeMesh = CreateBladeMesh(_bladeWidth, _bladeHeight);
        BuildChunks();
    }

    // Update is called once per frame
    void Update()
    {
        if(!_camera || !_grassMaterial) return;

        // カメラに見える範囲の6つの平面を取得する。
        GeometryUtility.CalculateFrustumPlanes(_camera, _frustumPlanes);
    
        var cameraPos = _camera.transform.position;
        var sqrDrawDistance = _drawDistance * _drawDistance;

        // 描画の設定。草は影を落とさない。
        var renderParams = new RenderParams(_grassMaterial);
        renderParams.shadowCastingMode = ShadowCastingMode.Off;
        renderParams.receiveShadows = false;

        foreach(var chunk in _chunks){
            // 遠すぎるチャンクは描画しない。
            var closestPoint = chunk.Bounds.ClosestPoint(cameraPos);
            if((closestPoint - cameraPos).sqrMagnitude > sqrDrawDistance) continue;

            // 画面に映らないチャンクも描画しない。
            if(!GeometryUtility.TestPlanesAABB(_frustumPlanes, chunk.Bounds)) continue;

            // このチャンクの箱を教えてから、草を一気に描く。
            renderParams.worldBounds = chunk.Bounds;
            Graphics.RenderMeshInstanced(renderParams, _bladeMesh, 0, chunk.Matrices);
        }

    }

    private void BuildChunks(){
        Bounds ground = _groundRenderer.bounds;
        float groundTopY = ground.max.y;

        // 1023本を超えないようにする。
        var bladeCount = Mathf.Min(_bladesPerChunk, 1023);

        System.Random random = new(_randomSeed);

        // ceilToInt: 引数以上の最小の整数を返す。
        var chunkCountX = Mathf.CeilToInt(ground.size.x / _chunkSize);
        var chunkCountZ = Mathf.CeilToInt(ground.size.z / _chunkSize);
    
        for(int cx = 0; cx < chunkCountX; cx++){
            for(int cz = 0; cz < chunkCountZ; cz++){

                var minX = ground.min.x + cx * _chunkSize;
                var minZ = ground.min.z + cz * _chunkSize;
                var chunk = new Chunk();
                chunk.Matrices = new Matrix4x4[bladeCount];
                
                // 草を一本ごとにランダムな位置、向き、大きさを決めて行列にする。
                for(int i = 0; i < bladeCount; i++){

                    //チャンク内のランダムな位置。地面の外にはみ出さにように制限する。
                    var x = Mathf.Min(minX + (float)random.NextDouble() * _chunkSize, ground.max.x);
                    var z = Mathf.Min(minZ + (float)random.NextDouble() * _chunkSize, ground.max.z);

                    // ランダムな向き(Y軸回転)と大きさ(0.7-1.3倍)を決める。
                    var yaw = (float)random.NextDouble() * 360.0f;
                    var scale = 0.7f + (float)random.NextDouble() * 0.6f;

                    // ランダムな傾きの角度(0-90度)を決める。
                    var lean = 
                        _leanMinDegrees + 
                        (float)random.NextDouble() * (_leanMaxDegrees - _leanMinDegrees);

                    // 位置、向き、大きさをまとめて行列にする。
                    chunk.Matrices[i] = 
                        Matrix4x4.TRS(
                        new Vector3(x, groundTopY, z), 
                        Quaternion.Euler(lean, yaw, 0.0f), 
                        new Vector3(scale, scale, scale));
                } // for i

                // チャンク全体を囲む箱を作る。
                var center = new Vector3(
                    minX + _chunkSize * 0.5f, 
                    groundTopY + _bladeHeight * 0.75f, 
                    minZ + _chunkSize * 0.5f);
                
                var size = new Vector3(
                    _chunkSize, 
                    _bladeHeight * 1.5f, 
                    _chunkSize);

                chunk.Bounds = new Bounds(center, size);
                _chunks.Add(chunk);

            } // for cz
        } // for cx
    } // BuildChunks()

    private static Mesh CreateBladeMesh(float width, float height){
        var mesh = new Mesh();
        mesh.name = "GrassBlade";

        var halfWidth = width * 0.5f;
        var midHalfWidth = width * 0.35f;

        // 頂点の位置
        mesh.vertices = new Vector3[]{
            new Vector3(-halfWidth, 0.0f, 0.0f), // 左下
            new Vector3(halfWidth, 0.0f, 0.0f), // 右下
            new Vector3(-midHalfWidth, height * 0.5f, 0.0f), // 左中
            new Vector3(midHalfWidth, height * 0.5f, 0.0f), // 右中
            new Vector3(0.0f, height, 0.0f) // 上
        };

        // UVのYを根本から先端に向かって0.0-1.0になるようにする。
        mesh.uv = new Vector2[]{
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(0.1f, 0.5f),
            new Vector2(0.9f, 0.5f),
            new Vector2(0.5f, 1.0f)
        };

        mesh.triangles = new int[]{
            0, 2, 1, // 下の三角形
            1, 2, 3, // 中央の三角形
            2, 4, 3 // 上の三角形
        };
        mesh.RecalculateNormals();
        return mesh;
    }
} // class GrassField
