using System;
using UnityEngine;

public class Goal : MonoBehaviour
{
    public event Action<PlayerController> OnGoalScored;

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Ball _ball))
        {
            // Update score text based on ball owner from the Ball script
            if (_ball._ballOwner == null) return;

            //Debug.Log($"{_ball} Goal by {_ball._ballOwner}");

            OnGoalScored?.Invoke(_ball._ballOwner);
            //Destroy(_ball.gameObject);
        }
    }
}
