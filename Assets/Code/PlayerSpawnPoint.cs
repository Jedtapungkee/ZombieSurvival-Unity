using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public Transform spawnPoint;   // ลาก PlayerSpawnPoint เข้ามาใน Inspector
    public GameObject playerPrefab;

    void Start()
    {
        Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}
