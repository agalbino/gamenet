using Mirror;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    public PlayerController _ballOwner;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out PlayerController _player))
            _ballOwner = _player;
    }
}
