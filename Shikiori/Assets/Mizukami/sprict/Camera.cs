using UnityEngine;

public class Camera : MonoBehaviour
{
    GameObject player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }
    private void Update()
    {
        
    }
    // Update is called once per frame
    private void LateUpdate()   // Updateの後にされるUpdate。
    {

        Vector3 PlayerPos = player.transform.position;
        Vector3 CameraPos = new(PlayerPos.x, PlayerPos.y + 8, PlayerPos.z - 10);
        transform.position = CameraPos;
    }
}
