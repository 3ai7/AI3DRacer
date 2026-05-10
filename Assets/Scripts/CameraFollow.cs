using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour {
	
	//Variables visible in the inspector
	public Transform camTarget;
	
	public float startDelay;
    public float distance = 6.0f;
    public float height = 5.0f;
    public float heightDamping = 0.5f;
    public float rotationDamping = 1.0f;
	
	//not in the inspector
	float originalRotationDamping;
	bool canSwitch;
	
	void Start(){
		//get the default rotation damping (so we can make it smaller in the beginning)
		originalRotationDamping = rotationDamping;
		//set rotation damping to a really small value to have a smooth transition at the start of the game
		rotationDamping = 0.1f;
		
		//switch the camera angle/rotation damping after a while
		StartCoroutine(SwitchAngle());
	}
	
	void Update(){
		//reset the rotation damping when the player first turns the car
		if((Input.GetMouseButtonDown(0) || Input.GetAxis("Horizontal") != 0) && rotationDamping == 0.1f && canSwitch)
			rotationDamping = originalRotationDamping;
	}
	 
	void LateUpdate(){		
		//Check if the camera has a target to follow
        if(!camTarget)
            return;		
		
		//Some private variables for the rotation and position of the camera
        float wantedRotationAngle = camTarget.eulerAngles.y;
        float wantedHeight = camTarget.position.y + height;
        float currentRotationAngle = transform.eulerAngles.y;
        float currentHeight = transform.position.y;
		
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);
 
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);
 
        Quaternion currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);
     
        transform.position = camTarget.position;
        transform.position -= currentRotation * Vector3.forward * distance;
		
		//Set camera postition
        transform.position = new Vector3(transform.position.x, currentHeight, transform.position.z);
		
		//Look at the camera target
        transform.LookAt(camTarget);
    }
	
	IEnumerator SwitchAngle(){
		//wait and switch rotation damping
		yield return new WaitForSeconds(startDelay);
		
		canSwitch = true;
	}
}
