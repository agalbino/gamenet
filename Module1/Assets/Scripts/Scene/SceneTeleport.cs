using System.Collections;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTeleport : NetworkBehaviour
{
    [Scene, SerializeField]
    private string _sceneToTeleportTo;

    [SerializeField] private string _spawnName;

    public void OnTeleportButtonClicked()
    {
        if (!isServer) return;

        PlayerController player = null;
        NetworkIdentity[] allObjectsWithNetworkId = FindObjectsOfType<NetworkIdentity>();

        foreach (NetworkIdentity id in allObjectsWithNetworkId)
        { 
            id.enabled = true;

            if (id.gameObject.TryGetComponent(out PlayerController foundPlayer))
            {
                player = foundPlayer;
                StartCoroutine(SendPlayer(player.gameObject));
                break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.GetComponent<PlayerController>())
            StartCoroutine(SendPlayer(other.gameObject));
    }

    [ServerCallback]
    IEnumerator SendPlayer(GameObject player)
    {
        // Get network id and conn
        NetworkIdentity id = null;
        id = player.GetComponent<NetworkIdentity>();
        if (id == null) yield break;

        NetworkConnectionToClient conn = id.connectionToClient;
        if (conn == null) yield break;

        // Client unloads current additive scene and remove player from conn
        conn.Send(new SceneMessage { sceneName = gameObject.scene.path, sceneOperation = SceneOperation.UnloadAdditive, customHandling = true });
        NetworkServer.RemovePlayerForConnection(conn, RemovePlayerOptions.KeepActive);

        // Load target scene and 
        conn.Send(new SceneMessage { sceneName = _sceneToTeleportTo, sceneOperation = SceneOperation.LoadAdditive, customHandling = true });
        SceneManager.MoveGameObjectToScene(player, SceneManager.GetSceneByPath(_sceneToTeleportTo));

        StartCoroutine(SendBall());

        // Move to playerStart pos
        NetworkStartPosition[] positions = FindObjectsOfType<NetworkStartPosition>();
        Vector3 position = Vector3.zero;
        foreach(NetworkStartPosition pos in positions)
        {
            if (pos.gameObject.scene.path == _sceneToTeleportTo && pos.gameObject.name == _spawnName)
            {
                position = pos.transform.position;
                break;
            }
        }

        player.transform.position = position;

        yield return new WaitForEndOfFrame();
        NetworkServer.AddPlayerForConnection(conn, player);
        player.GetComponent<Rigidbody>().isKinematic = false;
    }

    [ServerCallback]
    IEnumerator SendBall()
    {
        // Find ball
        Ball ball = null;
        NetworkIdentity[] allObjectsWithNetworkId = FindObjectsOfType<NetworkIdentity>();

        foreach (NetworkIdentity id in allObjectsWithNetworkId)
        { 
            id.enabled = true;

            if (id.gameObject.TryGetComponent(out Ball foundBall))
                ball = foundBall; break;
        }

        // Wait for end of frame then make ball not Kinematic
        yield return new WaitForEndOfFrame();
        ball.gameObject.GetComponent<Rigidbody>().isKinematic = false;
    }
}
