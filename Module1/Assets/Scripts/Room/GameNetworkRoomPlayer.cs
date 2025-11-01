using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-room-player
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkRoomPlayer.html
*/

/// <summary>
/// This component works in conjunction with the NetworkRoomManager to make up the multiplayer room system.
/// The RoomPrefab object of the NetworkRoomManager must have this component on it.
/// This component holds basic room player data required for the room to function.
/// Game specific data for room players can be put in other components on the RoomPrefab or in scripts derived from NetworkRoomPlayer.
/// </summary>
public class GameNetworkRoomPlayer : NetworkRoomPlayer
{
    GameNetworkRoomManager _room = GameNetworkRoomManager.singleton;

    [Header("Player Ready UI")]
    [SerializeField] GameObject _canvasUI;
    [SerializeField] Button _readyButton;
    // [SerializeField] Button _startButton;
    [SerializeField] TMP_InputField _nameInputField;

    [Header("Player List UI")]
    [SerializeField] Transform _playerList;
    [SerializeField] GameObject _playerStatePrefab;

    public Action<string> OnChangeName;
    public Action<bool> OnChangeReady;
    public Action OnDisconnected;

    #region Start & Stop Callbacks

    /// <summary>
    /// This is invoked for NetworkBehaviour objects when they become active on the server.
    /// <para>This could be triggered by NetworkServer.Listen() for objects in the scene, or by NetworkServer.Spawn() for objects that are dynamically created.</para>
    /// <para>This will be called for objects on a "host" as well as for object on a dedicated server.</para>
    /// </summary>
    public override void OnStartServer() { }

    /// <summary>
    /// Invoked on the server when the object is unspawned
    /// <para>Useful for saving object data in persistent storage</para>
    /// </summary>
    public override void OnStopServer() => OnDisconnected?.Invoke();

    /// <summary>
    /// Called on every NetworkBehaviour when it is activated on a client.
    /// <para>Objects on the host have this function called, as there is a local client on the host. The values of SyncVars on object are guaranteed to be initialized correctly with the latest state from the server when this function is called on the client.</para>
    /// </summary>
    public override void OnStartClient() { }

    /// <summary>
    /// This is invoked on clients when the server has caused this object to be destroyed.
    /// <para>This can be used as a hook to invoke effects or do client specific cleanup.</para>
    /// </summary>
    public override void OnStopClient() { }

    /// <summary>
    /// Called when the local player object has been set up.
    /// <para>This happens after OnStartClient(), as it is triggered by an ownership message from the server. This is an appropriate place to activate components or functionality that should only be active for the local player, such as cameras and input.</para>
    /// </summary>
    public override void OnStartLocalPlayer()
    {
        //_readyUI.SetActive(true);
    }

    /// <summary>
    /// This is invoked on behaviours that have authority, based on context and <see cref="NetworkIdentity.hasAuthority">NetworkIdentity.hasAuthority</see>.
    /// <para>This is called after <see cref="OnStartServer">OnStartServer</see> and before <see cref="OnStartClient">OnStartClient.</see></para>
    /// <para>When <see cref="NetworkIdentity.AssignClientAuthority"/> is called on the server, this will be called on the client that owns the object. When an object is spawned with <see cref="NetworkServer.Spawn">NetworkServer.Spawn</see> with a NetworkConnectionToClient parameter included, this will be called on the client that owns the object.</para>
    /// </summary>
    public override void OnStartAuthority()
    {
        _canvasUI.SetActive(true);
        _nameInputField.onValueChanged.AddListener(CmdSetName);
        
        // if (isServer) _startButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// This is invoked on behaviours when authority is removed.
    /// <para>When NetworkIdentity.RemoveClientAuthority is called on the server, this will be called on the client that owns the object.</para>
    /// </summary>
    public override void OnStopAuthority()
    {
        _canvasUI.SetActive(false);
    }

    #endregion

    #region Room Client Callbacks

    /// <summary>
    /// This is a hook that is invoked on all player objects when entering the room.
    /// <para>Note: isLocalPlayer is not guaranteed to be set until OnStartLocalPlayer is called.</para>
    /// </summary>
    public override void OnClientEnterRoom() => UpdatePlayerList(_room.roomSlots);

    /// <summary>
    /// This is a hook that is invoked on all player objects when exiting the room.
    /// </summary>
    public override void OnClientExitRoom() => UpdatePlayerList(_room.roomSlots);

    #endregion

    #region SyncVar Hooks

    /// <summary>
    /// This is a hook that is invoked on clients when the index changes.
    /// </summary>
    /// <param name="oldIndex">The old index value</param>
    /// <param name="newIndex">The new index value</param>
    public override void IndexChanged(int oldIndex, int newIndex) { }

    /// <summary>
    /// This is a hook that is invoked on clients when a RoomPlayer switches between ready or not ready.
    /// <para>This function is called when the a client player calls SendReadyToBeginMessage() or SendNotReadyToBeginMessage().</para>
    /// </summary>
    /// <param name="oldReadyState">The old readyState value</param>
    /// <param name="newReadyState">The new readyState value</param>
    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState) => OnChangeReady?.Invoke(newReadyState);

    #endregion

    #region Optional UI

    public override void OnGUI()
    {
        base.OnGUI();
    }

    public void ToggleReady()
    {
        bool isReady = !readyToBegin;
        CmdChangeReadyState(isReady);

        _readyButton.GetComponentInChildren<TextMeshProUGUI>().text = isReady ? "Cancel" : "Ready";
    }

    public void StartGame()
    {
        if (!isServer) return;
        _room.ServerChangeScene(_room.GameplayScene);
    }

    public void SetStartGameInteractable(bool isInteractable)
    {
        // if (isServer) // Only make startBtn interactable if all players are ready
        // {
        //     _startButton.interactable = isInteractable;
        //     Debug.Log("All players ready! Can start game");
        // }
            //_startButton.interactable = _room.allPlayersReady;
            //_room.roomSlots.Where(roomPlayer => roomPlayer.CompareTag("Player"));
    }

    [Command]
    public void CmdSetName(string playerName)
    {
        RpcSetName(playerName);
    }

    [ClientRpc]
    public void RpcSetName(string playerName)
    {
        //Debug.Log($"{gameObject.name} is changing name to {playerName}");
        OnChangeName?.Invoke(playerName);
    }

    [ClientRpc]
    public void RpcDisableCanvas()
    {
        _canvasUI.SetActive(false);
        //Debug.Log($"[{_canvasUI.name}] RoomUI isActive: ({_canvasUI.activeSelf})");
    }

    public void UpdatePlayerList(HashSet<NetworkRoomPlayer> players)
    {
        //Debug.Log("Updating playerList");
        List<GameNetworkRoomPlayer> roomPlayerList = _playerList.GetComponentsInChildren<PlayerReadyState>().Select(readyState => readyState.RoomPlayer).ToList();

        foreach (NetworkRoomPlayer player in players.Where(roomPlayer => !roomPlayerList.Contains(roomPlayer))) // players that arent inside the list
        {
            GameObject obj = Instantiate(_playerStatePrefab, _playerList);
            obj.GetComponent<PlayerReadyState>().SetRoomPlayer((GameNetworkRoomPlayer)player);
        }

        foreach (Transform t in _playerList)
        {
            t.SetSiblingIndex(t.GetComponent<PlayerReadyState>().RoomPlayer.index);
        }
    }

    #endregion
}
