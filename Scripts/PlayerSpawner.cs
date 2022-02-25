using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner instance;
    public GameObject playerPrefab;

    public GameObject gmPrefab;
    private GameObject player;
    public GameObject deathAnimation;
    public float respawnTime = 5f;
    private void Awake() {
        instance = this;
    }

    void Start()
    {
        if (PhotonNetwork.IsConnected) 
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer() {
        bool isMaster = PhotonNetwork.IsMasterClient;
        Transform spawnPoint = SpawnManager.instance.pickSpawnPoint(isMaster);
        if (isMaster) {
            player = PhotonNetwork.Instantiate(gmPrefab.name, spawnPoint.position, spawnPoint.rotation);
        } else {
            player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
        }
        
    }

    public void playerDeath(string killer) {
        UiController.instance.deathText.text = "You were killed by " + killer;
        // Play death animation, destroy the player, show death screen, wait for 5 seconds, then respawn.
        MatchManager.instance.updatePlayerSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);
        if (player != null) {
            StartCoroutine(deathCoroutine());
        }
    }

    public IEnumerator deathCoroutine() {
        PhotonNetwork.Instantiate(deathAnimation.name, player.transform.position, Quaternion.identity);
        PhotonNetwork.Destroy(player);
        UiController.instance.deathScreen.SetActive(true);
        yield return new WaitForSeconds(respawnTime);
        UiController.instance.deathScreen.SetActive(false);
        SpawnPlayer();
    }
}