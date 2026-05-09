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
    public float waveHeight = 0.15f;

    [Tooltip("柏林噪声采样偏移")]
    public float offset = 0f;

    [Tooltip("顶点随机偏移量")]
    public float randomness = 0.03f;

    [Tooltip("世界向玩家移动的速度")]
    public float globalSpeed = 10f;

    [Tooltip("起始过渡长度")]
    public float startTransitionLength = 2f;

    [Header("References")]
    public Transform player;
    public Material segmentMaterial;

    private List<SegmentData> segments = new List<SegmentData>();
    private int segmentIndex = 0;
    private float segmentZLength;

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

        // 每个片段的Z轴长度 = scale * 5，让片段更长
        segmentZLength = scale * 5f;

        // 创建初始两个片段
        CreateSegment(Vector3.zero, null);
        CreateSegment(new Vector3(0, 0, segmentZLength), segments[0]);
    }

void Update()
    {
        if (player == null) return;

        float moveAmount = -globalSpeed * Time.deltaTime;

        for (int i = segments.Count - 1; i >= 0; i--)
        {
            if (segments[i].gameObject != null)
                segments[i].gameObject.transform.position += Vector3.forward * moveAmount;
        }

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
                    CreateSegment(spawnPos, lastSeg);
                }
            }
        }

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
    /// 获取指定片段末尾一环顶点的世界空间坐标
    /// </summary>
    Vector3[] GetEndRingWorldPositions(SegmentData seg)
    {
        int radialCount = dimensions.x;
        int lengthCount = dimensions.y;
        int vertexCols = radialCount + 1;
        int lastRowStart = lengthCount * vertexCols;

        Vector3[] endRing = new Vector3[vertexCols];
        Vector3[] vertices = seg.meshFilter.mesh.vertices;
        Transform t = seg.gameObject.transform;

        for (int col = 0; col < vertexCols; col++)
        {
            endRing[col] = t.TransformPoint(vertices[lastRowStart + col]);
        }

        return endRing;
    }

    /// <summary>
    /// 创建一个世界片段
    /// </summary>
    /// <param name="worldPosition">片段的世界空间位置</param>
    /// <param name="previousSegment">前一个片段引用（用于平滑过渡），可为null</param>
void CreateSegment(Vector3 worldPosition, SegmentData previousSegment)
    {
        int radialCount = dimensions.x;
        int lengthCount = dimensions.y;
        int vertexCols = radialCount + 1;

        GameObject segObj = new GameObject("Segment_" + segmentIndex);
        segObj.transform.position = worldPosition;
        segObj.transform.rotation = Quaternion.identity;

        MeshFilter mf = segObj.AddComponent<MeshFilter>();
        MeshRenderer mr = segObj.AddComponent<MeshRenderer>();
        mr.material = segmentMaterial != null ? segmentMaterial : new Material(Shader.Find("Standard"));

        Mesh mesh = new Mesh();
        mf.mesh = mesh;

        Vector3[] beginPoints = null;
        if (previousSegment != null)
        {
            beginPoints = GetEndRingWorldPositions(previousSegment);
        }

        int vertexRows = lengthCount + 1;
        int vertCount = vertexRows * vertexCols;

        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];

        for (int row = 0; row < vertexRows; row++)
        {
            float t = (float)row / lengthCount;
            float localZ = t * segmentZLength;
            float worldZ = worldPosition.z + localZ;

            // 过渡渐变因子：开始几行平滑过渡，减少突变
            float transitionFactor = (beginPoints != null && row < 3) ? Mathf.SmoothStep(0f, 1f, (float)row / 3f) : 1f;

            for (int col = 0; col < vertexCols; col++)
            {
                int idx = row * vertexCols + col;
                float angle = (float)col / radialCount * Mathf.PI * 2f;

                if (row == 0 && beginPoints != null)
                {
                    vertices[idx] = segObj.transform.InverseTransformPoint(beginPoints[col]);
                }
                else
                {
                    float nx = Mathf.Cos(angle) * perlinScale + offset + worldZ * 0.3f;
                    float ny = Mathf.Sin(angle) * perlinScale + offset + worldZ * 0.3f;
                    float noise = Mathf.PerlinNoise(nx, ny);

                    float rand = (Mathf.PerlinNoise(angle * 10f + offset + 100f, worldZ * 0.5f + offset + 200f) - 0.5f) * 2f * randomness;

                    float radiusOffset = (noise * waveHeight + rand) * transitionFactor;
                    float radius = scale - radiusOffset;
                    radius = Mathf.Max(radius, 0.1f);

                    float x = Mathf.Cos(angle) * radius;
                    float y = Mathf.Sin(angle) * radius;
                    vertices[idx] = new Vector3(x, y, localZ);
                }

                uvs[idx] = new Vector2((float)col / radialCount, t);
            }
        }

        int[] triangles = new int[radialCount * lengthCount * 6];
        int triIdx = 0;

        for (int row = 0; row < lengthCount; row++)
        {
            for (int col = 0; col < radialCount; col++)
            {
                int bl = row * vertexCols + col;
                int br = bl + 1;
                int tl = (row + 1) * vertexCols + col;
                int tr = tl + 1;

                triangles[triIdx++] = bl;
                triangles[triIdx++] = tl;
                triangles[triIdx++] = br;

                triangles[triIdx++] = br;
                triangles[triIdx++] = tl;
                triangles[triIdx++] = tr;
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