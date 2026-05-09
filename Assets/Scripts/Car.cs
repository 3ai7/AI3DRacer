using UnityEngine;

public class Car : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody rb;
    public WheelCollider[] wheelColliders; // FL, FR, RL, RR
    public Transform[] wheelMeshes;     // FL, FR, RL, RR

    [Header("Steering Settings")]
    public int rotateSpeed = 2;
    public int rotationAngle = 45;
    public int wheelRotateSpeed = 10;

    [Header("Effects")]
    public ParticleSystem grassEffects;

    [Header("Downforce")]
    public float extraDownforce = 500f;

    [Header("Ragdoll")]
    public GameObject ragdollPrefab;

    private float currentAngle = 0f;
    private float targetAngleOffset = 0f;
    private float cylinderRadius = 5f;
    private bool hasLanded = false;

    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        WorldGenerator wg = FindObjectOfType<WorldGenerator>();
        if (wg != null)
            cylinderRadius = wg.scale;

        Vector3 pos = transform.position;
        currentAngle = Mathf.Atan2(pos.y, pos.x);

        rb.useGravity = false; // 禁用世界重力，使用圆柱体自定义重力
        rb.sleepThreshold = 0f; // 禁用休眠，确保 WheelCollider 持续检测地面
        hasLanded = false;
    }

    void Update()
    {
        HandleInput();
        UpdateWheelMeshes();
        HandleEffects();
    }

    void FixedUpdate()
    {
        if (!hasLanded)
        {
            ApplyFallingPhysics();
            AlignCarToSurface();
        }
        else
        {
            ApplyCylinderForces();
            UpdateCylinderAngularSteering();
        }
        PreventTunneling();
    }

    /// <summary>
    /// 下落阶段：将小车从外部拉向圆柱体表面，模拟重力下落效果
    /// </summary>
    void ApplyFallingPhysics()
    {
        Vector3 car2D = new Vector3(rb.position.x, rb.position.y, 0f);
        float distFromAxis = car2D.magnitude;

        if (distFromAxis < 0.01f)
        {
            // 异常情况：小车在轴心，推到表面
            rb.position = new Vector3(cylinderRadius, 0f, rb.position.z);
            return;
        }

        Vector3 inward = -car2D.normalized; // 指向圆柱体轴心（即"下方"）

        // 强引力将小车拉到圆柱体表面
        float fallForce = Physics.gravity.magnitude * 6f;
        rb.AddForce(inward * fallForce, ForceMode.Acceleration);

        // 阻尼：抵消向内的速度分量，防止弹跳/振荡
        float radialVel = Vector3.Dot(rb.velocity, inward);
        if (radialVel > 0f)
        {
            rb.AddForce(-inward * radialVel * 3f, ForceMode.Acceleration);
        }

        // 检测是否着陆：距离接近表面 且 任意车轮接地
        bool anyWheelGrounded = false;
        if (wheelColliders != null && wheelColliders.Length > 0)
        {
            foreach (var wc in wheelColliders)
            {
                if (wc == null) continue;
                WheelHit hit;
                if (wc.GetGroundHit(out hit))
                {
                    anyWheelGrounded = true;
                    break;
                }
            }
        }

        if (distFromAxis <= cylinderRadius + 0.15f && anyWheelGrounded)
        {
            hasLanded = true;
        }
    }

    /// <summary>
    /// 下落过程中将车身姿态对齐到圆柱体表面（车顶朝外）
    /// </summary>
    void AlignCarToSurface()
    {
        Vector3 car2D = new Vector3(rb.position.x, rb.position.y, 0f);
        if (car2D.magnitude < 0.01f) return;

        Vector3 outward = car2D.normalized;
        // up 指向外（远离轴心），forward 沿 Z 轴
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, outward);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 10f * Time.fixedDeltaTime));
    }

    /// <summary>
    /// 防穿模：硬性限制小车与圆柱体轴心的最小距离，防止穿透赛道网格
    /// </summary>
    void PreventTunneling()
    {
        Vector3 car2D = new Vector3(rb.position.x, rb.position.y, 0f);
        float distFromAxis = car2D.magnitude;

        float wheelRadius = 0.35f;
        float minDist = cylinderRadius - wheelRadius - 0.3f;

        if (minDist < 0.5f) minDist = 0.5f;

        if (distFromAxis < minDist && distFromAxis > 0.001f)
        {
            Vector3 outward = car2D.normalized;
            Vector3 clampedPos = outward * minDist;
            rb.position = new Vector3(clampedPos.x, clampedPos.y, rb.position.z);

            // 消除指向内侧的速度分量，避免持续冲撞
            float radialVel = Vector3.Dot(rb.velocity, outward);
            if (radialVel < 0f)
            {
                Vector3 tangentialVel = rb.velocity - outward * radialVel;
                rb.velocity = tangentialVel;
            }
        }
    }

    void HandleInput()
    {
        float steerInput = 0f;

        // Mouse position — left/right third of screen
        if (Input.mousePosition.x < Screen.width * 0.3f)
            steerInput = -1f;
        else if (Input.mousePosition.x > Screen.width * 0.7f)
            steerInput = 1f;

        // Arrow keys
        steerInput += Input.GetAxis("Horizontal");
        steerInput = Mathf.Clamp(steerInput, -1f, 1f);

        targetAngleOffset = steerInput * rotationAngle;
    }

    void UpdateCylinderAngularSteering()
    {
        // 仅处理角度旋转，不再覆盖位置
        float offsetRad = targetAngleOffset * Mathf.Deg2Rad;
        float targetAngle = currentAngle + offsetRad;

        float maxDelta = rotateSpeed * Time.fixedDeltaTime;
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, maxDelta);

        // 计算朝向：forward +Z，up 指向圆柱体轴心
        Vector3 up = new Vector3(-Mathf.Cos(currentAngle), -Mathf.Sin(currentAngle), 0f);
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, up);

        float rotSpeed = rotateSpeed * 50f * Time.fixedDeltaTime;
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, rotSpeed));

        // 用力将车辆保持在圆柱体表面附近，替代 MovePosition
        Vector3 car2D = new Vector3(rb.position.x, rb.position.y, 0f);
        float distFromAxis = car2D.magnitude;
        Vector3 outward = car2D.normalized;

        float wheelRadius = 0.35f;
        float targetDist = cylinderRadius - wheelRadius;

        if (distFromAxis > targetDist + 0.1f)
        {
            // 离轴太远（脱离表面），用力拉回
            Vector3 pullIn = -outward * Physics.gravity.magnitude * 20f;
            rb.AddForce(pullIn, ForceMode.Acceleration);
        }
        else if (distFromAxis < targetDist - 0.3f)
        {
            // 离轴太近，推向外表面
            rb.AddForce(outward * Physics.gravity.magnitude * 5f, ForceMode.Acceleration);
        }
    }

    void ApplyCylinderForces()
    {
        // 自定义重力：将车辆拉向圆柱体内表面（远离轴心方向 = 朝向表面外侧）
        Vector3 car2D = new Vector3(rb.position.x, rb.position.y, 0f);
        Vector3 outward = car2D.normalized;

        float distFromAxis = car2D.magnitude;

        // 距离相关的重力强度：越靠近轴心，推力越强（模拟向"下"滚落）
        float forceScale = 1.5f;
        if (distFromAxis < cylinderRadius - 3f)
            forceScale = 12f;
        else if (distFromAxis < cylinderRadius - 1.5f)
            forceScale = 8f;
        else if (distFromAxis < cylinderRadius - 0.5f)
            forceScale = 4f;
        else if (distFromAxis < cylinderRadius)
            forceScale = 2f;

        rb.AddForce(outward * Physics.gravity.magnitude * forceScale, ForceMode.Acceleration);
    }

    void UpdateWheelMeshes()
    {
        for (int i = 0; i < wheelColliders.Length && i < wheelMeshes.Length; i++)
        {
            if (wheelColliders[i] == null || wheelMeshes[i] == null) continue;

            Vector3 pos;
            Quaternion rot;
            wheelColliders[i].GetWorldPose(out pos, out rot);

            wheelMeshes[i].position = pos;
            wheelMeshes[i].rotation = rot * Quaternion.Euler(wheelRotateSpeed * Time.time * 100f, 0f, 0f);
        }
    }

    void HandleEffects()
    {
        if (grassEffects == null) return;

        bool rearGrounded = true;
        if (wheelColliders.Length >= 4)
        {
            WheelHit hitRL;
            WheelHit hitRR;
            bool rl = wheelColliders[2].GetGroundHit(out hitRL);
            bool rr = wheelColliders[3].GetGroundHit(out hitRR);
            rearGrounded = rl || rr;
        }

        if (!rearGrounded)
        {
            // Extra downforce at the rear — push toward cylinder surface (outward)
            Vector3 rearPos = transform.position - transform.forward * 1f;
            Vector3 car2D = new Vector3(transform.position.x, transform.position.y, 0f);
            Vector3 outward = car2D.normalized;
            rb.AddForceAtPosition(outward * extraDownforce, rearPos, ForceMode.Force);

            if (!grassEffects.isPlaying)
                grassEffects.Play();
        }
        else
        {
            if (grassEffects.isPlaying)
                grassEffects.Stop();
        }
    }

    public void FallApart()
    {
        if (ragdollPrefab != null)
            Instantiate(ragdollPrefab, transform.position, transform.rotation);

        gameObject.SetActive(false);
    }
}