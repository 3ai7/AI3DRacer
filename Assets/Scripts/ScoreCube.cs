using UnityEngine;

public class ScoreCube : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"[ScoreCube {name}] Spawned at {transform.position} - expecting distance-based pickup");
    }

    // OnDestroy 中播放收集动画的残留已被 Car.CheckNearbyScoreCubes 接管
    // ScoreCube 自身无碰撞体，由 Car 周期性距离检测来拾取
}