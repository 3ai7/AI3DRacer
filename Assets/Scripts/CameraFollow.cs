using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform camTarget;
    public float distance = 10f;
    public float height = 5f;
    public float heightDamping = 2f;
    public float rotationDamping = 3f;

    private float originalRotationDamping;
    private float elapsedTime;
    private readonly float entryDuration = 3f;

    void Start()
    {
        originalRotationDamping = rotationDamping;
        elapsedTime = 0f;
    }

    void LateUpdate()
    {
        if (camTarget == null)
            return;

        elapsedTime += Time.deltaTime;
        if (elapsedTime < entryDuration)
        {
            rotationDamping = Mathf.Lerp(0.1f, originalRotationDamping, elapsedTime / entryDuration);
        }
        else
        {
            rotationDamping = originalRotationDamping;
        }

        float wantedRotationAngle = camTarget.eulerAngles.y;
        float wantedHeight = camTarget.position.y + height;

        float currentRotationAngle = transform.eulerAngles.y;
        float currentHeight = transform.position.y;

        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);

        Quaternion currentRotation = Quaternion.Euler(0f, currentRotationAngle, 0f);

        Vector3 targetPosition = camTarget.position - (currentRotation * Vector3.forward * distance);
        targetPosition.y = currentHeight;

        transform.position = targetPosition;
        transform.LookAt(camTarget);
    }
}