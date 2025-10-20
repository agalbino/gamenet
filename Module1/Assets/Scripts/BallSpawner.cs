using Mirror;
using UnityEngine;

public class BallSpawner : NetworkBehaviour
{
    [SerializeField] private Goal _goal;
    [SerializeField] private GameObject _ballPrefab;
    private Ball _currentBall;

    #region Server Side Logic
    public override void OnStartServer() // So its the same for all clients
    {
        base.OnStartServer();

        _goal.OnGoalScored += HandleGoalScored;        
        SpawnNewBall(null);
    }

    public override void OnStopServer() // Remove listener
    {
        base.OnStopServer();
        _goal.OnGoalScored -= SpawnNewBall;
    }

    [Server] // Only runs on server
    void HandleGoalScored(PlayerController scorer)
    {
        // Destroy ball if it hasn't been already
        if (_currentBall != null) NetworkServer.Destroy(_currentBall.gameObject);

        SpawnNewBall(scorer);
    }

    [Server]
    void SpawnNewBall(PlayerController scorer)
    {
        // Spawn above scorer if not null, else spawn at BallSpawner pos
        Vector3 spawnPos = scorer != null ? scorer.transform.position + Vector3.up : transform.position;

        GameObject newBall = Instantiate(_ballPrefab, spawnPos, Quaternion.identity);
        NetworkServer.Spawn(newBall);
        _currentBall = newBall.GetComponent<Ball>();
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
