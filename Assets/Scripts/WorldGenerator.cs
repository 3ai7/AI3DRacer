using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class WorldGenerator : MonoBehaviour {

    //在检查器中可见的变量
    public Material meshMaterial;
	public float scale = 5f;
	public Vector2 dimensions = new Vector2(32, 20);
	public float perlinScale = 0.5f;
    public float waveHeight = 1f;
    public float offset = 0f;
	public float randomness = 0.1f;
	public float globalSpeed = 10f;
	public int startTransitionLength = 2;
	public BasicMovement lampMovement;
	public GameObject[] obstacles;
	public GameObject gate;
	public int startObstacleChance = 20;
	public int obstacleChanceAcceleration = 1;
	public int gateChance = 50;
	public int showItemDistance = 100;
	public float shadowHeight = 0f;

    //在检查器中不可见
    Vector3[] beginPoints;
	
	GameObject[] pieces = new GameObject[2];
	
	GameObject currentCylinder;
	
	void Start(){
        // 创建一个数组来存储每个世界部分的起始顶点（我们需要它来正确地在世界片段之间进行过渡） 
        beginPoints = new Vector3[(int)dimensions.x + 1];

        //首先生成两个世界片段
        for (int i = 0; i < 2; i++){
			GenerateWorldPiece(i);
		}
	}
	
	void LateUpdate(){
        //如果第二块（物体）离玩家足够近，我们可以移除第一块（物体）并更新地形
        if (pieces[1] && pieces[1].transform.position.z <= 0)
			StartCoroutine(UpdateWorldPieces());

        //更新场景中的所有物品，比如尖刺和闸门
        UpdateAllItems();
	}
	
	void UpdateAllItems(){
        //找到所有物品
        GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
		
		//遍历所有的物品
		for(int i = 0; i < items.Length; i++){
            //获取该物品的所有网格渲染器
            foreach (MeshRenderer renderer in items[i].GetComponentsInChildren<MeshRenderer>()){
                //如果该物品离玩家足够近，就显示它
                bool show = items[i].transform.position.z < showItemDistance;

                //如果我们想显示这个项目，更新它的 shadowCastingMode
                //由于世界是一个圆柱体，我们只需要为圆柱体下半部分的物体添加阴影即可
                //否则你会在各处看到来自圆柱体顶部物体的奇怪阴影
                if (show)
					renderer.shadowCastingMode = (items[i].transform.position.y < shadowHeight) ? ShadowCastingMode.On : ShadowCastingMode.Off;

                //只有在我们想要显示这个项目时，才启用渲染器
                renderer.enabled = show;
			}
		}
	}
	
	void GenerateWorldPiece(int i){
        //创建一个新的圆柱体并将其放入碎片数组中
        pieces[i] = CreateCylinder();
        //根据圆柱体的索引对其进行定位
        pieces[i].transform.Translate(Vector3.forward * (dimensions.y * scale * Mathf.PI) * i);

        //更新这段内容，使其包含一个端点，并且能够移动等。
        UpdateSinglePiece(pieces[i]);
	}
	
	IEnumerator UpdateWorldPieces(){
        //移除第一块（玩家已无法再看到的那块）
        Destroy(pieces[0]);

        //将世界数组中的第二块分配给第一块
        pieces[0] = pieces[1];

        //新创建一个新的第二部分
        pieces[1] = CreateCylinder();

        //放置新的部件并旋转它，使其与第一个部件相匹配
        pieces[1].transform.position = pieces[0].transform.position + Vector3.forward * (dimensions.y * scale * Mathf.PI);
		pieces[1].transform.rotation = pieces[0].transform.rotation;

        //更新这个新生成的世界片段
        UpdateSinglePiece(pieces[1]);

        //等一帧
        yield return 0;
	}
	
	void UpdateSinglePiece(GameObject piece){
        //给我们新生成的部件添加基本移动脚本，使其向玩家移动
        BasicMovement movement = piece.AddComponent<BasicMovement>();
        //让它以全局速度移动
        movement.movespeed = -globalSpeed;

        //将旋转速度设置为灯具（定向灯）的旋转速度
        if (lampMovement != null)
			movement.rotateSpeed = lampMovement.rotateSpeed;

        //为这部分内容创建一个端点
        GameObject endPoint = new GameObject();
		endPoint.transform.position = piece.transform.position + Vector3.forward * (dimensions.y * scale * Mathf.PI);
		endPoint.transform.parent = piece.transform;
		endPoint.name = "End Point";

        //改变柏林噪声的偏移量，以确保每一块都与上一块不同
        offset += randomness;

        //改变障碍物出现的概率，这意味着随着时间的推移将会出现更多的障碍物
        if (startObstacleChance > 5)
			startObstacleChance -= obstacleChanceAcceleration;
	}

	public GameObject CreateCylinder(){
        //为我们的世界部分创建基础对象并为其命名
        GameObject newCylinder = new GameObject();
		newCylinder.name = "World piece";

        //将当前圆柱体设置为这个新创建的对象
        currentCylinder = newCylinder;

        //给新的世界片段添加一个网格过滤器和一个网格渲染器
        MeshFilter meshFilter = newCylinder.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = newCylinder.AddComponent<MeshRenderer>();

        //给这件新片段一种材质
        meshRenderer.material = meshMaterial;
        //生成一个新的网格并将其分配给网格过滤器
        meshFilter.mesh = Generate();

        //创建网格后，添加一个与新网格匹配的碰撞体
        newCylinder.AddComponent<MeshCollider>();
		
		return newCylinder;
	}

    //这将返回我们新的世界部分的网格
    Mesh Generate(){
        //创建并命名一个新的网格
        Mesh mesh = new Mesh();
		mesh.name = "MESH";

        //创建数组来存储顶点（三维空间中的点）、UV 坐标（纹理坐标）和三角形（构成我们网格的顶点集合）
        Vector3[] vertices = null;
		Vector2[] uvs = null;
		int[] triangles = null;

        //通过填充数组来创建我们网格的形状
        CreateShape(ref vertices, ref uvs, ref triangles);

        //分配顶点、纹理坐标和三角形
        mesh.vertices = vertices;
		mesh.uv = uvs;
		mesh.triangles = triangles;

        //重新计算我们世界片段的法向量
        mesh.RecalculateNormals();
		
		return mesh;
	}
	
	void CreateShape(ref Vector3[] vertices, ref Vector2[] uvs, ref int[] triangles){

        //获取这件物品在 x 轴和 z 轴上的尺寸
        int xCount = (int)dimensions.x;
		int zCount = (int)dimensions.y;

        //使用所需的维度初始化顶点数组和 uv 数组
        vertices = new Vector3[(xCount + 1) * (zCount + 1)];
		uvs = new Vector2[(xCount + 1) * (zCount + 1)];
		
		int index = 0;

        //获取圆柱体的半径
        float radius = xCount * scale * 0.5f;

        //嵌套两个循环以遍历 x 轴和 z 轴上的所有顶点
        for (int x = 0; x <= xCount; x++){
			for(int z = 0; z <= zCount; z++){
                //获取圆柱体中的角度，以正确定位这个顶点
                float angle = x * Mathf.PI * 2f/xCount;

                //使用该角度的余弦和正弦来设置这个顶点
                vertices[index] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, z * scale * Mathf.PI);

                //还要更新 UVs，这样我们才能给地形添加纹理
                uvs[index] = new Vector2(x * scale, z * scale);

                //现在使用我们的柏林噪声比例和偏移量来为柏林噪声创建 x 值和 z 值
                float pX = (vertices[index].x * perlinScale) + offset;
				float pZ = (vertices[index].z * perlinScale) + offset;

                //获取圆柱体的中心，但保留这个顶点的 z 坐标，这样我们就能指向中心内部了
                Vector3 center = new Vector3(0, 0, vertices[index].z);
                //现在使用柏林噪声和期望的波高，将这个顶点向内朝中心移动
                vertices[index] += (center - vertices[index]).normalized * Mathf.PerlinNoise(pX, pZ) * waveHeight;

                //这部分负责世界片段之间的平滑过渡：

                //检查是否存在起始点，以及我们是否处于网格的起点（z 表示向前的方向，即穿过圆柱体）
                if (z < startTransitionLength && beginPoints[0] != Vector3.zero){
                    //如果是这样，我们必须将柏林噪声值与起始点结合起来
                    //我们需要提高来自柏林噪声的顶点所占的比例
                    //并从起始点降低百分比
                    //这样，它将从最后一个世界片段过渡到新的柏林噪声值

                    //当我们进一步深入圆柱体内部时，顶点中柏林噪声的占比将会增加。
                    float perlinPercentage = z * (1f/startTransitionLength);
                    //不要使用 z 起始点，因为它不会有正确的位置，而且我们只关心 x 轴和 y 轴上的噪声
                    Vector3 beginPoint = new Vector3(beginPoints[x].x, beginPoints[x].y, vertices[index].z);

                    //将起始点（即前一个世界片段的最后几个顶点）与原始顶点相结合，以平滑过渡到新的世界片段
                    vertices[index] = (perlinPercentage * vertices[index]) + ((1f - perlinPercentage) * beginPoint);
				}
				else if(z == zCount){
                    //如果这些是最后的顶点，请更新起始点，以便下一段也能平滑过渡
                    beginPoints[x] = vertices[index];
				}

                //利用网格顶点在随机位置生成物品
                if (Random.Range(0, startObstacleChance) == 0 && !(gate == null && obstacles.Length == 0))
					CreateItem(vertices[index], x);

                //增加当前的顶点索引
                index++;
			}
		}

        //初始化三角形数组（x 乘以 z 是正方形的数量，每个正方形有两个三角形，因此有 6 个顶点）
        triangles = new int[xCount * zCount * 6];

        //为我们的正方形创建基础（这会让生成算法更简单）
        int[] boxBase = new int[6];
		
		int current = 0;
		
		//遍历所有的x位置
		for(int x = 0; x < xCount; x++){
            //创建一个新的基准，我们可以用它在 z 轴上填充一行新的正方形。
            boxBase = new int[]{ 
				x * (zCount + 1), 
				x * (zCount + 1) + 1,
				(x + 1) * (zCount + 1),
				x * (zCount + 1) + 1,
				(x + 1) * (zCount + 1) + 1,
				(x + 1) * (zCount + 1),
			};
			
			
			
			//遍历所有的z位置
			for(int z = 0; z < zCount; z++){
                //将方框中所有顶点的索引增加 1，以移动到该 z 行上的下一个方块
                for (int i = 0; i < 6; i++){
					boxBase[i] = boxBase[i] + 1;
				}

                //根据 6 个顶点分配 2 个新的三角形，以填充一个新的正方形
                for (int j = 0; j < 6; j++){					
					triangles[current + j] = boxBase[j] - 1;
				}

                //现在将current增加 6 以进入下一个方块
                current += 6;
			}
		}
	}
	
	void CreateItem(Vector3 vert, int x){
        //获取圆柱体的中心，但使用顶点的 z 值
        Vector3 zCenter = new Vector3(0, 0, vert.z);

        //检查我们是否得到了中心点和顶点之间的正确角度
        if (zCenter - vert == Vector3.zero || x == (int)dimensions.x/4 || x == (int)dimensions.x/4 * 3)
			return;

        //创建一个新物品，它有很小的概率是传送门（gateChance），而有很大的概率是障碍物
        GameObject newItem = Instantiate((Random.Range(0, gateChance) == 0) ? gate : obstacles[Random.Range(0, obstacles.Length)]);


        MeshRenderer[] renderers = newItem.GetComponentsInChildren<MeshRenderer>();
        Color randomColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f); // 鲜艳颜色
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", randomColor);
            r.SetPropertyBlock(propBlock);
        }
        // 新增：随机缩放，比如 0.8 ~ 1.3 倍
        float randomScale = Random.Range(0.8f, 1.3f);
        newItem.transform.localScale = Vector3.one * randomScale;

        //将物品向内朝着中心位置旋转
        //newItem.transform.rotation = Quaternion.LookRotation(zCenter - vert, Vector3.up);
        Quaternion baseRot = Quaternion.LookRotation(zCenter - vert, Vector3.up);
        Quaternion twist = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward);
        newItem.transform.rotation = baseRot * twist;

        //将物品放置在顶点位置
        newItem.transform.position = vert;

        //将新项目作为当前圆柱体的子项，这样它就会随之移动和旋转
        newItem.transform.SetParent(currentCylinder.transform, false);
	}
	
	public Transform GetWorldPiece(){
        //返回第一个世界片段
        return pieces[0].transform;
	}
}