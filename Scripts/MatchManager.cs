using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;
public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static MatchManager instance;
    public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    private int index;

    public int gmKillsToWin = 3;

    public GameState state = GameState.Waiting;

    public bool isGmWin;
    public string winnerName;
    public float waitAfterEnding = 5f;
    public enum EventCodes : byte {
        NewPlayer,
        ListPlayers,
        UpdateStat
    }

    public enum GameState {
        Waiting,
        Playing,
        Ending
    }

    private void Awake() {
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.IsConnected) {
            SceneManager.LoadScene(0);
        } else {
            if (PhotonNetwork.IsMasterClient) {
                newPlayerSend(PhotonNetwork.NickName, true);
            } else {
                newPlayerSend(PhotonNetwork.NickName, false);
            }
            state = GameState.Playing;
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

     public void OnEvent(EventData photonEvent) 
    {
        //Codes > 199 are reserved by the Photon system
        if(photonEvent.Code < 200) {
            EventCodes eventCode = (EventCodes) photonEvent.Code;
            object[] data = (object[]) photonEvent.CustomData;
            if (eventCode == EventCodes.NewPlayer) {
                newPlayerRecieve(data);
            } else if (eventCode == EventCodes.ListPlayers) {
                listPlayerRecieve(data);
            } else if (eventCode == EventCodes.UpdateStat) {
                updatePlayerRecieve(data);
            }
    
        }
    }
    // when this gameObject is enabled
    public override void OnEnable() {
        // add this to the list so when an event callback happens, this script listens to those events
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable() {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void newPlayerSend(string username, bool isMaster) {
        object[] playerInfo = new object[5];
        playerInfo[0] = username;
        playerInfo[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        playerInfo[2] = 0;
        playerInfo[3] = 0;
        playerInfo[4] = isMaster;
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer, 
            playerInfo, 
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
            );
    }

    public void newPlayerRecieve(object[] data) {
        PlayerInfo player = new PlayerInfo((string)data[0], (int)data[1], (int)data[2], (int)data[3], (bool)data[4]);
        allPlayers.Add(player);
        listPlayerSend();
    }

    public void listPlayerSend() {
        object[] listPlayers = new object[allPlayers.Count + 1];
        listPlayers[0] = state;
        for (int i = 0; i < allPlayers.Count; i++) {
            object[] player = new object[5];
            player[0] = allPlayers[i].name;
            player[1] = allPlayers[i].actor;
            player[2] = allPlayers[i].kills;
            player[3] = allPlayers[i].deaths;
            player[4] = allPlayers[i].isGameMaster;
            listPlayers[i + 1] = player;
        }
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ListPlayers, 
            listPlayers, 
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void listPlayerRecieve(object[] data) {
        allPlayers.Clear();
        state = (GameState) data[0];
        for (int i = 1; i < data.Length; i++) {
            object[] player = (object[])data[i];
            PlayerInfo playerInfo = new PlayerInfo((string)player[0], (int)player[1], (int)player[2], (int)player[3], (bool)player[4]);
            allPlayers.Add(playerInfo);
            if (PhotonNetwork.LocalPlayer.ActorNumber == playerInfo.actor) {
                index = i - 1;
            }
        }
        StateCheck();
    }

    public void updatePlayerSend(int sendingActor, int statToUpdate, int amountToChange) {
        object[] package = new object[] { sendingActor, statToUpdate, amountToChange };
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdateStat, 
            package, 
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );

    }

    public void updatePlayerRecieve(object[] data) {
        int actor = (int)data[0];
        int stat = (int)data[1];
        int amount = (int)data[2];
        for (int i = 0; i < allPlayers.Count; i++) {
            if (allPlayers[i].actor == actor) {
                switch(stat) {
                    // kills
                    case 0:
                        allPlayers[i].kills += amount;
                        break;
                    // deaths
                    case 1:
                        allPlayers[i].deaths += amount;
                        break;
                }
                if (allPlayers[i].isGameMaster) {
                    UpdateGmKills(allPlayers[i].kills);
                    gmWinConditionCheck(allPlayers[i].kills);
                }
                break;
            }
        }
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        int index = allPlayers.FindIndex(x => x.name == otherPlayer.NickName);

        Debug.Log(index);

        if (index != -1)
            allPlayers.RemoveAt(index);
        listPlayerSend();

    }

    public override void OnLeftRoom() {
        base.OnLeftRoom();
        SceneManager.LoadScene(0);
    }

    void gmWinConditionCheck(int gmKills) {
        Debug.Log("gmKills " + gmKills);
        Debug.Log("IsGmWIN1? " + isGmWin);
        if(gmKills >= gmKillsToWin && state != GameState.Ending) {
            state = GameState.Ending;
            isGmWin = true;
            winnerName = PhotonNetwork.MasterClient.NickName;
            if (PhotonNetwork.IsMasterClient) {
                listPlayerSend();
            }
        }

        
    }
    public void UpdateGmKills(int gmKills) {
        UiController.instance.gmKills.text = "GM Kills: " + gmKills;
    }

    public void StateCheck() {
        Debug.Log("IsGmWIN2? " + isGmWin);
        if (state == GameState.Ending) {
            EndGame(isGmWin);
        }
    }

    void EndGame(bool isGmWin) {
        state = GameState.Ending;
        if (PhotonNetwork.IsMasterClient) {
            PhotonNetwork.DestroyAll();
        }
        if (isGmWin) {
            UiController.instance.winText.text = "GM " + winnerName + " wins.";
        } else {
            UiController.instance.winText.text = "Player " + winnerName + " wins.";
        }
        UiController.instance.winScreen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(endGameCoroutine());
    }
    private IEnumerator endGameCoroutine() {
        yield return new WaitForSeconds(waitAfterEnding);
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
    }
}
[System.Serializable]
public class PlayerInfo {
        public string name;
        public int actor;
        public int kills;
        public int deaths;
        public bool isGameMaster;
        public PlayerInfo (string _name, int _actor, int _kills, int _deaths, bool _isGameMaster) {
            name = _name;
            actor = _actor;
            kills = _kills;
            deaths = _deaths;
            isGameMaster = _isGameMaster;
        }
}