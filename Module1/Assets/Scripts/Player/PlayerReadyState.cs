using TMPro;
using UnityEngine;

public class PlayerReadyState : MonoBehaviour
{
    GameNetworkRoomPlayer _roomPlayer;
    public GameNetworkRoomPlayer RoomPlayer => _roomPlayer;

    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _readyText;

    void Start()
    {
        SetReady(false);
    }

    public void SetRoomPlayer(GameNetworkRoomPlayer player)
    {
        // Set refs
        _roomPlayer = player;
        _roomPlayer.OnChangeName += SetName;
        _roomPlayer.OnChangeReady += SetReady;
        _roomPlayer.OnDisconnected += DestroyState;
    }

    public void SetName(string playerName)
    {
        _nameText.text = playerName;
    }

    public void SetReady(bool isReady)
    {
        _readyText.text = isReady ? "Ready" : "Not Ready";
        _readyText.color = isReady ? Color.green : Color.red;
    }

    public void DestroyState() => Destroy(this);
}
