using System.Collections;
using Mirror;
using UnityEngine;

public class BallSpawner : NetworkBehaviour
{
    [SerializeField] private Goal[] _goals;
    [SerializeField] private Transform _ballRespawn;
    [SerializeField] private GameObject _ballPrefab;
    private Ball _currentBall;

    #region Server Side Logic
    public override void OnStartServer() // So its the same for all clients
    {
        base.OnStartServer();

        if (_goals.Length == 0) 
            { Debug.LogError("No goals set in inspector"); return; }
        
        foreach (var g in _goals)
            g.OnGoalScored += HandleGoalScored;
          
        SpawnNewBall(null);
    }

    public override void OnStopServer() // Remove listener
    {
        base.OnStopServer();

        foreach (var g in _goals)
            g.OnGoalScored -= HandleGoalScored;
    }

    [Server] // Only runs on server
    void HandleGoalScored(PlayerController scorer)
    {
        // Destroy ball if it hasn't been already
        //if (_currentBall != null) NetworkServer.Destroy(_currentBall.gameObject);

        SpawnNewBall(scorer);
    }

    [Server]
    void SpawnNewBall(PlayerController scorer)
    {        
        // Spawn above scorer if not null, else spawn at BallSpawner pos
        Vector3 spawnPos = scorer != null ? scorer.transform.position + Vector3.up : _ballRespawn.transform.position;

        if (!_currentBall) // Spawn a new ball
        {
            GameObject newBall = Instantiate(_ballPrefab, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(newBall);
            _currentBall = newBall.GetComponent<Ball>();
        }
        else // Move the ball
            StartCoroutine(MoveBallToScorer(_ballRespawn.transform.position));
    }

    IEnumerator MoveBallToScorer(Vector3 newPos)
    {
        yield return new WaitForSeconds(1.0f);
        _currentBall.transform.SetLocalPositionAndRotation(newPos + Vector3.up, Quaternion.identity);
        Debug.Log($"Moved ball to {newPos} in {_ballRespawn.gameObject.scene.name}");

        // Reset velocity
        _currentBall.GetComponent<Rigidbody>().velocity = Vector3.zero;
        _currentBall.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }
    #endregion

    void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.TryGetComponent(out Ball _ball))
        {
            // Reset ball position if outside play area
            _ball.transform.position = transform.position;

            // Reset velocity
            _ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
            _ball.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            Debug.Log("Resetting ball");
        }
    }
}
