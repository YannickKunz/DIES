using UnityEngine;
using System.Collections;

public class BoxDispenser : MonoBehaviour
{
    public GameObject boxPrefab; // Assign your box prefab in the Inspector
    public Transform spawnPoint; // Assign a spawn point in the Inspector
    public float spawnInterval = 3f; // Time interval between box spawns

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SpawnBox());
    }

    // Coroutine to spawn a box every 3 seconds
    IEnumerator SpawnBox()
    {
        while (true)
        {
            Instantiate(boxPrefab, spawnPoint.position, spawnPoint.rotation);
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}