using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour{
    public GameObject followTarget;
    public float followSpeed = 1f;
    
    void Start(){
        
    }

    void FixedUpdate(){
        transform.position = Vector3.Lerp(transform.position, new Vector3(followTarget.transform.position.x, followTarget.transform.position.y, transform.position.z), followSpeed);
    }
}
