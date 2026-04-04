using UnityEngine;

public class HeadLookAt : MonoBehaviour
{
    public Transform target; 
    public Vector3 rotationOffset; 

    void LateUpdate()
    {
        if (target != null)
        {

            transform.LookAt(target);

            transform.Rotate(rotationOffset);
        }
    }
}