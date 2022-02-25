using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public List<Transform> spawnPointList;
    public static SpawnManager instance;

    private void Awake() {
        instance = this;
    }
    void Start()
    {
        foreach (Transform spawnPoint in spawnPointList) {
            spawnPoint.gameObject.SetActive(false);
        }
    }

    public Transform pickSpawnPoint(bool isMaster) {
        if (isMaster) {
            return spawnPointList[0];
        } else {
            return spawnPointList[Random.Range(1, spawnPointList.Count)];
        }
        
    }
}
