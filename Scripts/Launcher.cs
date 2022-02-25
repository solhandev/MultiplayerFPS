using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;
    public GameObject loadingScreenPanel;
    public GameObject menuButtons;
    public TMP_Text loadingText;
    public GameObject createRoomPanel;
    public TMP_InputField roomNameInput;
    public GameObject roomPanel;
    public TMP_Text roomNameText;
    public TMP_Text playerNameLabel;
    private List<TMP_Text> allPlayersNames = new List<TMP_Text>();
    public GameObject errorPanel;
    public TMP_Text errorText;

    public GameObject roomBrowserPanel;
    public RoomButton theRoomButton;
    private List<RoomButton> allRoomButtons = new List<RoomButton>();

    private Dictionary<string, RoomInfo> cachedRoomsList = new Dictionary<string, RoomInfo>();

    public GameObject createUsernamePanel;
    public TMP_InputField usernameInput;
    private static bool hasSetUsername;
    public string levelToPlay;
    public GameObject startGameButton;
    
    // Testing only
    public GameObject roomTestButton;

    private void Awake() {
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        closeMenus();
        menuButtons.SetActive(true);
        Connect();
#if UNITY_EDITOR
        roomTestButton.SetActive(true);
#endif
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Connect() {
        loadingScreenPanel.SetActive(true);
        loadingText.text = "Connecting to network...";
        // uses the Photon Server Settings (Window -> Photon Unity Networking -> Highlight Server Settings)
        PhotonNetwork.ConnectUsingSettings();
    }
    void closeMenus() {
        loadingScreenPanel.SetActive(false);
        menuButtons.SetActive(false);
        createRoomPanel.SetActive(false);
        roomPanel.SetActive(false);
        errorPanel.SetActive(false);
        roomBrowserPanel.SetActive(false);
        createUsernamePanel.SetActive(false);
    }

    public override void OnConnectedToMaster() {
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
        loadingText.text = "Joining lobby...";
    }

    public override void OnJoinedLobby() {
        closeMenus();
        Debug.Log("JOINED LOBBY");
        menuButtons.SetActive(true);
        if (!hasSetUsername) {
            promptUsername();
            if (PlayerPrefs.HasKey("playerName")) {
                usernameInput.text = PlayerPrefs.GetString("playerName");
            }
        // Shouldn't reach here but just in case
        } else {
            PhotonNetwork.NickName = PlayerPrefs.GetString("playerName");
        }
    }
    public void promptUsername() {
        closeMenus();
        createUsernamePanel.SetActive(true);
    }

    public void saveUsername() {
        if (!string.IsNullOrEmpty(usernameInput.text)) {
            PhotonNetwork.NickName = usernameInput.text;
            PlayerPrefs.SetString("playerName", usernameInput.text);
            hasSetUsername = true;
            returnToMainMenu();
        }
        
    }

    public void OpenRoomCreatePanel() {
        closeMenus();
        createRoomPanel.SetActive(true);
    }

    public void createRoom() {
        if (!string.IsNullOrEmpty(roomNameInput.text)) {
            Debug.Log("CREATE ROOM1");
            RoomOptions options = new RoomOptions();
            options.MaxPlayers = 8;
            PhotonNetwork.CreateRoom(roomNameInput.text, options);
            closeMenus();
            loadingText.text = "Creating room...";
            loadingScreenPanel.SetActive(true);
            // Photon auto joins the newly created room
        }
    }

    public override void OnJoinedRoom() {
        closeMenus();
        roomPanel.SetActive(true);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        ListAllPlayers();
        if (PhotonNetwork.IsMasterClient) {
            startGameButton.SetActive(true);
        } else {
            startGameButton.SetActive(false);
        }
    }

    private void ListAllPlayers() {
        foreach (TMP_Text playerName in allPlayersNames) {
            Destroy(playerName.gameObject);
        }
        allPlayersNames.Clear();
        // Photon.RealTime.Player
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++) {
            addToPlayerList(players[i]);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        addToPlayerList(newPlayer);
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer) {
        ListAllPlayers();
    }

    public override void OnMasterClientSwitched(Player newMasterClient) {
        if (PhotonNetwork.IsMasterClient) {
            startGameButton.SetActive(true);
        } else {
            startGameButton.SetActive(false);
        }
    }
    public void addToPlayerList(Player player) {
        TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
        newPlayerLabel.text = player.NickName;
        if (player.IsMasterClient) {
            newPlayerLabel.text = "(Host) " + newPlayerLabel.text;
            newPlayerLabel.fontStyle = FontStyles.Bold;
            newPlayerLabel.color = new Color(1.0f, 0.64f, 0.0f);
        }
        newPlayerLabel.gameObject.SetActive(true);
        allPlayersNames.Add(newPlayerLabel);
    }

    public override void OnCreateRoomFailed(short returnCode, string message) {
        errorText.text = "Failed To Create Room. " + message;
        closeMenus();
        errorPanel.SetActive(true);
    }

    public void leaveRoom() {
        PhotonNetwork.LeaveRoom();
        Debug.Log("LEFT ROOM1");
        closeMenus();
        loadingText.text = "Leaving room...";
        loadingScreenPanel.SetActive(true);
    }
    
    public override void OnLeftRoom() {
        Debug.Log("LEFT ROOM2");
        openRoomList();
    }

    public void closeErrorScreenReturnToCreateRoom() {
        PhotonNetwork.LeaveRoom();
        OpenRoomCreatePanel();
    }

    public void returnToMainMenu() {
        closeMenus();
        menuButtons.SetActive(true);
    }

    public void openRoomList() {
        closeMenus();
        Debug.Log("LEFT ROOM3");
        roomBrowserPanel.SetActive(true);
    }

    public void closeRoomList() {
        returnToMainMenu();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList) {
        for (int i = 0; i < roomList.Count; i++)
        {
            RoomInfo info = roomList[i];
            if (info.RemovedFromList)
            {
                cachedRoomsList.Remove(info.Name);
            }
            else
            {
                cachedRoomsList[info.Name] = info;                
            }
        }

        foreach(RoomButton rb in allRoomButtons) {
            Destroy(rb.gameObject);
        }
        allRoomButtons.Clear();

        theRoomButton.gameObject.SetActive(false);

        foreach (KeyValuePair<string, RoomInfo> roomInfo in cachedRoomsList)
        {
            RoomButton newButton = Instantiate(theRoomButton, theRoomButton.transform.parent);
            newButton.setButtonDetails(roomInfo.Value);
            newButton.gameObject.SetActive(true);
            allRoomButtons.Add(newButton);
        }
        /*Debug.Log("ON ROOM LIST UPDATE");
        foreach(RoomButton rb in allRoomButtons) {
            Destroy(rb.gameObject);
        }
        allRoomButtons.Clear();

        theRoomButton.gameObject.SetActive(false);

        for (int i = 0; i < roomList.Count; i++) {
            if (roomList[i].PlayerCount != roomList[i].MaxPlayers && !roomList[i].RemovedFromList) {
                RoomButton newButton = Instantiate(theRoomButton, theRoomButton.transform.parent);
                newButton.setButtonDetails(roomList[i]);
                newButton.gameObject.SetActive(true);
                allRoomButtons.Add(newButton);
            }
        }
        */
    }

    public void JoinRoom(RoomInfo inputInfo) {
        PhotonNetwork.JoinRoom(inputInfo.Name);
        closeMenus();
        loadingText.text = "Joining Room...";
        loadingScreenPanel.SetActive(true);
    }

    public void QuitGame() {
        Application.Quit();
    }

    public void StartGame() {
        PhotonNetwork.LoadLevel(levelToPlay);
        PhotonNetwork.CurrentRoom.IsVisible = false;
    }

    // Testing only
    public void QuickTest() {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 8;
        PhotonNetwork.CreateRoom("TestGame", options);
        closeMenus();
        loadingText.text = "Loading Test Game...";
        loadingScreenPanel.SetActive(true);
    }
}
