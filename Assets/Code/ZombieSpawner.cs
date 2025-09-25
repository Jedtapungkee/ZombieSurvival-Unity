using UnityEngine;


public class CreepSpawner : MonoBehaviour
{
    public GameObject creepPrefab;
    public float spawnInterval = 3f;
    void Start()
    {
        if (creepPrefab != null)
        {
            InvokeRepeating("SpawnCreep", spawnInterval, spawnInterval);
        }
    }


    void SpawnCreep()
    {
        Transform spawnPoint = transform;
        Instantiate(creepPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}
