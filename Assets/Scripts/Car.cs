using UnityEngine;
using System.Collections;

public class Car : MonoBehaviour
{

    //variables visible in the inspector
    public Rigidbody rb;

    public Transform[] wheelMeshes;
    public WheelCollider[] wheelColliders;

    public int rotateSpeed = 200;
    public int rotationAngle = 45;
    public int wheelRotateSpeed = 500;

    public Transform[] grassEffects;
    public Transform[] skidMarkPivots;
    public float grassEffectOffset = 0.5f;

    public Transform back;
    public float constantBackForce = 500f;

    public GameObject skidMark;
    public float skidMarkSize = 0.2f;
    public float skidMarkDelay = 0.05f;
    public float minRotationDifference = 2f;

    public GameObject ragdoll;

    [Header("Driving")]
    public float motorTorque = 800f;
    public float brakeTorque = 2000f;

    //not in the inspector
    int targetRotation;
    WorldGenerator generator;

    float lastRotation;
    bool skidMarkRoutine;

    void Start()
    {
        //find the world generator and start the skid mark coroutine
        generator = GameObject.FindObjectOfType<WorldGenerator>();
        StartCoroutine(SkidMark());

        // 定期检测附近 ScoreCube（无碰撞体方案，避免 WheelCollider 悬挂射线物理碰撞）
        InvokeRepeating(nameof(CheckNearbyScoreCubes), 0.5f, 0.5f);
        Debug.Log("[Car] InvokeRepeating started for CheckNearbyScoreCubes");

        // 设置车轮摩擦力
        foreach (var wc in wheelColliders)
        {
            if (wc == null) continue;

            WheelFrictionCurve sidewaysFriction = wc.sidewaysFriction;
            sidewaysFriction.stiffness = 0.5f;
            wc.sidewaysFriction = sidewaysFriction;
        }
    }

    void FixedUpdate()
    {
        // === 驱动轮子前进 / 刹车 ===
        float acceleration = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            acceleration = 1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            acceleration = -1f;

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            if (wheelColliders[i] == null) continue;

            // 后轮驱动
            if (i >= 2)
                wheelColliders[i].motorTorque = acceleration * motorTorque;
            else
                wheelColliders[i].motorTorque = 0f;

            // 无输入时刹车
            wheelColliders[i].brakeTorque = (acceleration == 0f) ? brakeTorque : 0f;

            // 前轮不转
            wheelColliders[i].steerAngle = 0f;
        }

        //update the skidmark and the grass effects
        UpdateEffects();
    }

    void LateUpdate()
    {
        //for all wheels
        for (int i = 0; i < wheelMeshes.Length; i++)
        {
            //set the wheel mesh to the position of the wheel collider
            Quaternion quat;
            Vector3 pos;

            wheelColliders[i].GetWorldPose(out pos, out quat);

            wheelMeshes[i].position = pos;

            //rotate the wheel so it looks like we're driving
            wheelMeshes[i].Rotate(Vector3.right * Time.deltaTime * wheelRotateSpeed);
        }

        //if the player wants to turn, rotate the car
        if (Input.GetMouseButton(0) || Input.GetAxis("Horizontal") != 0)
        {
            UpdateTargetRotation();
        }
        else if (targetRotation != 0)
        {
            //else, rotate back to the center
            targetRotation = 0;
        }

        //apply the rotation by rotating towards the target angle on the y axis
        Vector3 rotation = new Vector3(transform.localEulerAngles.x, targetRotation, transform.localEulerAngles.z);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(rotation), rotateSpeed * Time.deltaTime);
    }

    void UpdateTargetRotation()
    {
        //if we're using the mouse to rotate
        if (Input.GetAxis("Horizontal") == 0)
        {
            //get the mouse position (left/right side of the screen)
            if (Input.mousePosition.x > Screen.width * 0.5f)
            {
                //rotate right
                targetRotation = rotationAngle;
            }
            else
            {
                //rotate left
                targetRotation = -rotationAngle;
            }
        }
        else
        {
            //if we're pressing arrow keys or a/d, rotate the car based on the horizontal input value
            targetRotation = (int)(rotationAngle * Input.GetAxis("Horizontal"));
        }
    }

    void UpdateEffects()
    {
        //if both wheels are off the ground, add force will be true
        bool addForce = true;
        //check if we rotated the car since last frame
        bool rotated = Mathf.Abs(lastRotation - transform.localEulerAngles.y) > minRotationDifference;

        //for both grass effects (rear wheels)
        for (int i = 0; i < 2; i++)
        {
            //get the rear wheels (one of them in each iteration)
            Transform wheelMesh = wheelMeshes[i + 2];

            //check if this wheel is grounded currently
            if (Physics.Raycast(wheelMesh.position, Vector3.down, grassEffectOffset * 1.5f))
            {
                //if so, show the grass effect
                if (!grassEffects[i].gameObject.activeSelf)
                    grassEffects[i].gameObject.SetActive(true);

                //update the grass effect height and the skidmark height to match this wheel
                float effectHeight = wheelMesh.position.y - grassEffectOffset;
                Vector3 targetPosition = new Vector3(grassEffects[i].position.x, effectHeight, wheelMesh.position.z);
                grassEffects[i].position = targetPosition;
                skidMarkPivots[i].position = targetPosition;

                //this wheel is grounded so we don't need any extra force at the back of the car
                addForce = false;
            }
            else if (grassEffects[i].gameObject.activeSelf)
            {
                //if we're not grounded, don't show the grass effect
                grassEffects[i].gameObject.SetActive(false);
            }
        }

        //add force at the back of the car for stabilization
        if (addForce)
        {
            rb.AddForceAtPosition(back.position, Vector3.down * constantBackForce);
            //don't show the skidmarks
            skidMarkRoutine = false;
        }
        else
        {
            if (targetRotation != 0)
            {
                //if the car has rotated show the skid mark
                if (rotated && !skidMarkRoutine)
                {
                    skidMarkRoutine = true;
                }
                else if (!rotated && skidMarkRoutine)
                {
                    skidMarkRoutine = false;
                }
            }
            else
            {
                //don't show the skidmark if we're rotating back to the center
                skidMarkRoutine = false;
            }
        }

        //update the last rotation (which is now the current rotation since everything has been updated)
        lastRotation = transform.localEulerAngles.y;
    }

    public void FallApart()
    {
        //destroy the car
        Instantiate(ragdoll, transform.position, transform.rotation);
        gameObject.SetActive(false);
    }

    IEnumerator SkidMark()
    {
        //loops continuesly
        while (true)
        {
            //wait for the delay in between individual skid marks
            yield return new WaitForSeconds(skidMarkDelay);

            //show skidmarks if we need skidmarks now
            if (skidMarkRoutine)
            {
                //for both rear wheels, instantiate a skid mark and parent it to the environment so it moves realistically
                for (int i = 0; i < skidMarkPivots.Length; i++)
                {
                    GameObject newskidMark = Instantiate(skidMark, skidMarkPivots[i].position, skidMarkPivots[i].rotation);
                    if (generator != null)
                    {
                        Transform worldPiece = generator.GetWorldPiece();
                        if (worldPiece != null)
                            newskidMark.transform.parent = worldPiece;
                    }
                    newskidMark.transform.localScale = new Vector3(1, 1, 4) * skidMarkSize;
                }
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            FallApart();
        }
    }

    void CheckNearbyScoreCubes()
    {
        ScoreCube[] cubes = FindObjectsOfType<ScoreCube>();
        if (cubes.Length > 0)
            Debug.Log($"[Car] CheckNearbyScoreCubes: found {cubes.Length} ScoreCube(s) in scene");
        
        foreach (var cube in cubes)
        {
            if (cube == null) continue;
            float dist = Vector3.Distance(transform.position, cube.transform.position);
            if (dist < 3f)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddScore(1);
                    Debug.Log($"[Car] Collected ScoreCube! Score +1 (dist={dist:F2})");
                }
                else
                {
                    Debug.LogWarning("[Car] GameManager.Instance is null! Cannot add score.");
                }
                Destroy(cube.gameObject);
            }
        }
    }
}