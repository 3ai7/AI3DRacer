using UnityEngine;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour
{
    [Header("Cylinder Settings")]
    [Tooltip("圆柱体半径")]
    public float scale = 5f;

    [Tooltip("x = 径向分段数, y = 长度分段数")]
    public Vector2Int dimensions = new Vector2Int(32, 20);

    [Tooltip("柏林噪声采样缩放")]
    public float perlinScale = 0.5f;

    [Tooltip("地形起伏最大高度")]
    public float waveHeight = 1f;

    [Tooltip("柏林噪声采样偏移")]
    public float offset = 0f;

    [Tooltip("柏林噪声偏移增量（每个新片段叠加）")]
    public float randomness = 0.1f;

    [Tooltip("世界向玩家移动的速度")]
    public float globalSpeed = 10f;

    // 实际圆柱体半径（由其他脚本读取）
    public float CylinderRadius => dimensions.x * scale * 0.5f;

    /// <summary>
    /// 返回离玩家最近的世界片段的 Transform，用于刹车痕等特效的父级绑定
    /// </summary>
    public Transform GetWorldPiece()
    {
        if (segments.Count == 0) return null;

        SegmentData closest = segments[0];
        float minDist = float.MaxValue;

        foreach (var seg in segments)
        {
            if (seg.gameObject == null) continue;
            float d = Mathf.Abs(seg.gameObject.transform.position.z - player.position.z);
            if (d < minDist)
            {
                minDist = d;
                closest = seg;
            }
        }

        return closest.gameObject != null ? closest.gameObject.transform : null;
    }

    [Tooltip("起始过渡长度（行数）")]
    public int startTransitionLength = 2;

    [Header("References")]
    public Transform player;
    public Material segmentMaterial;

    private List<SegmentData> segments = new List<SegmentData>();
    private int segmentIndex = 0;
    private float segmentZLength;
    private Vector3[] beginPoints;

    private class SegmentData
    {
        public GameObject gameObject;
        public MeshFilter meshFilter;
        public MeshCollider meshCollider;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // 径向顶点数 + 1（首尾焊接）
        beginPoints = new Vector3[dimensions.x + 1];

        // Z 轴方向总长 = 长度分段数 × 间距（scale * PI）
        segmentZLength = dimensions.y * scale * Mathf.PI;

        // 生成初始两个片段
        CreateSegment(Vector3.zero, false);
        CreateSegment(new Vector3(0, 0, segmentZLength), true);
    }

    void Update()
    {
        if (player == null) return;

        float moveAmount = -globalSpeed * Time.deltaTime;

        // 所有片段向 -Z 移动
        for (int i = segments.Count - 1; i >= 0; i--)
        {
            if (segments[i].gameObject != null)
                segments[i].gameObject.transform.position += Vector3.forward * moveAmount;
        }

        // 前方生成新片段
        if (segments.Count > 0)
        {
            SegmentData lastSeg = segments[segments.Count - 1];
            if (lastSeg.gameObject != null)
            {
                float lastSegEndZ = lastSeg.gameObject.transform.position.z + segmentZLength;
                float playerZ = player.position.z;

                if (lastSegEndZ - playerZ < segmentZLength * 1.5f)
                {
                    Vector3 spawnPos = new Vector3(0, 0, lastSeg.gameObject.transform.position.z + segmentZLength);
                    CreateSegment(spawnPos, true);
                }
            }
        }

        // 移除后方旧片段
        if (segments.Count > 1)
        {
            SegmentData firstSeg = segments[0];
            if (firstSeg.gameObject != null)
            {
                float firstSegEndZ = firstSeg.gameObject.transform.position.z + segmentZLength;
                float playerZ = player.position.z;

                if (firstSegEndZ < playerZ - segmentZLength)
                {
                    Destroy(firstSeg.gameObject);
                    segments.RemoveAt(0);
                }
            }
        }
    }

    /// <summary>
    /// 创建圆柱体网格片段（完全对齐 WorldGenerators 的 col-major 顶点布局）
    /// </summary>
    void CreateSegment(Vector3 worldPosition, bool useTransition)
    {
        int xCount = dimensions.x;      // 径向分段数
        int zCount = dimensions.y;      // 轴向分段数
        int vertexCols = xCount + 1;    // 径向顶点数（+1 用于首尾焊接到 2π）

        GameObject segObj = new GameObject("Segment_" + segmentIndex);
        segObj.transform.position = worldPosition;
        segObj.transform.rotation = Quaternion.identity;

        MeshFilter mf = segObj.AddComponent<MeshFilter>();
        MeshRenderer mr = segObj.AddComponent<MeshRenderer>();
        mr.material = segmentMaterial != null ? segmentMaterial : new Material(Shader.Find("Universal Render Pipeline/Lit"));

        Mesh mesh = new Mesh();
        mesh.name = "MESH_Segment_" + segmentIndex;
        mf.mesh = mesh;

        // 每个新片段增大噪声偏移
        offset += randomness;

        int vertexCount = (xCount + 1) * (zCount + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int index = 0;

        // 获取圆柱半径
        float radius = xCount * scale * 0.5f;

        // ★ 关键：外层 x（径向），内层 z（轴向）—— 与 WorldGenerators 完全一致的 col-major 布局
        for (int x = 0; x <= xCount; x++)
        {
            float angle = x * Mathf.PI * 2f / xCount;

            for (int z = 0; z <= zCount; z++)
            {
                // 圆柱表面顶点
                vertices[index] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    z * scale * Mathf.PI
                );

                // === Perlin 噪声向圆柱中心内推 ===
                float pX = vertices[index].x * perlinScale + offset;
                float pZ = vertices[index].z * perlinScale + offset;
                float noise = Mathf.PerlinNoise(pX, pZ);

                Vector3 center = new Vector3(0, 0, vertices[index].z);
                vertices[index] += (center - vertices[index]).normalized * noise * waveHeight;

                // === 片段间平滑过渡 ===
                if (useTransition && z < startTransitionLength && beginPoints[0] != Vector3.zero)
                {
                    float perlinPercentage = z * (1f / startTransitionLength);
                    Vector3 beginPoint = new Vector3(beginPoints[x].x, beginPoints[x].y, vertices[index].z);
                    vertices[index] = perlinPercentage * vertices[index] + (1f - perlinPercentage) * beginPoint;
                }
                else if (z == zCount)
                {
                    // 末尾行：存入 beginPoints 供下一片段过渡
                    beginPoints[x] = vertices[index];
                }

                // UV 坐标
                uvs[index] = new Vector2(x * scale, z * scale);

                index++;
            }
        }

        // === 三角形索引（boxBase 滑动窗口） ===
        int[] triangles = new int[xCount * zCount * 6];
        int current = 0;

        for (int x = 0; x < xCount; x++)
        {
            int[] boxBase = new int[]
            {
                x * (zCount + 1),
                x * (zCount + 1) + 1,
                (x + 1) * (zCount + 1),
                x * (zCount + 1) + 1,
                (x + 1) * (zCount + 1) + 1,
                (x + 1) * (zCount + 1),
            };

            for (int z = 0; z < zCount; z++)
            {
                for (int i = 0; i < 6; i++)
                {
                    boxBase[i] += 1;
                    triangles[current + i] = boxBase[i] - 1;
                }
                current += 6;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshCollider mc = segObj.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;

        SegmentData data = new SegmentData
        {
            gameObject = segObj,
            meshFilter = mf,
            meshCollider = mc
        };

        segments.Add(data);
        segmentIndex++;
    }
}