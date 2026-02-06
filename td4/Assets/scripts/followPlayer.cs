using UnityEngine;

public class followPlayer : MonoBehaviour
{
    public Transform player;
    public Vector3 marginFromPlayer;

    void Update()
    {
        transform.position = player.transform.position + marginFromPlayer;
    }
}