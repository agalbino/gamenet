using TMPro;
using Mirror;
using UnityEngine;
using System.Collections;

public class UIController : NetworkBehaviour
{
    [Header("Score References")]
    [SerializeField] private Goal _goal;
    [SerializeField] private TextMeshProUGUI _hostScoreText;
    [SerializeField] private TextMeshProUGUI _joinerScoreText;

    // SyncVar Hooks to call a function when the values change, updating variables on all clients
    // https://mirror-networking.gitbook.io/docs/manual/guides/synchronization/syncvar-hooks/
    [SyncVar(hook = nameof(OnHostScoreChanged))] private int _hostScore = 0;
    [SyncVar(hook = nameof(OnJoinerScoreChanged))] private int _joinerScore = 0;

    [Header("Notif References")]
    [SerializeField] private TextMeshProUGUI _whoScoredText;
    [SerializeField] private float _notifDisplayTime;
    private Coroutine _notifRoutine;

    #region Server Side Logic
    public override void OnStartServer() // Listens for goal event
    {
        base.OnStartServer();

        if (_goal != null)
            _goal.OnGoalScored += HandleGoalScored;
        else
            Debug.LogError("[UIController] Goal is not assigned in inspector!");
    }

    public override void OnStopServer() // Remove listener
    {
        base.OnStopServer();

        if (_goal != null)
            _goal.OnGoalScored -= HandleGoalScored;
    }

    [Server] // Only runs on server
    private void HandleGoalScored(PlayerController _player) // Modify syncVar changes (when goal is scored)
    {
        // Check for connectionId to know who is hoster (id = 0) and joiner (id not 0)
        if (_player != null && _player.connectionToClient != null && _player.connectionToClient.connectionId == 0)
            _hostScore++;
        else
            _joinerScore++;

        RpcShowGoalNotif(_player.netIdentity); // Show goal notif based on who the scorer is
    }
    #endregion

    #region Client Side Logic
    public override void OnStartClient() // Set initial text for scores upon client join / game start
    {
        base.OnStartClient();

        _hostScoreText.text = $"Host Score:\n{_hostScore}";
        _joinerScoreText.text = $"Joiner Score:\n{_joinerScore}";

        if (_whoScoredText != null)
            _whoScoredText.transform.parent.gameObject.SetActive(false);
    }

    #region Hook Methods for Score Text Update
    private void OnHostScoreChanged(int oldScore, int newScore) => _hostScoreText.text = $"Host Score:\n{newScore}";
    private void OnJoinerScoreChanged(int oldScore, int newScore) => _joinerScoreText.text = $"Joiner Score:\n{newScore}";
    #endregion

    [ClientRpc] // To update all clients
    void RpcShowGoalNotif(NetworkIdentity scorer) 
    {
        NetworkIdentity localPlayer = NetworkClient.localPlayer;
        if (localPlayer == null) return;

        bool isLocalPlayerTheScorer = scorer.netId == localPlayer.netId;
        string notifText;

        if (isLocalPlayerTheScorer)
            notifText = $"GOAAAAALLL!!! by you!";

        else // Not the scorer
            notifText = $"Enemy scored a goal!";

        // Show and hide notif panel coroutine
        if (_notifRoutine != null) StopCoroutine(_notifRoutine);
        _notifRoutine = StartCoroutine(ShowNotifRoutine(notifText));
    }

    private IEnumerator ShowNotifRoutine(string notifText)
    {
        _whoScoredText.text = notifText;
        _whoScoredText.transform.parent.gameObject.SetActive(true);

        yield return new WaitForSeconds(_notifDisplayTime);

        _whoScoredText.transform.parent.gameObject.SetActive(false);
        _notifRoutine = null;
    }
    #endregion
}
