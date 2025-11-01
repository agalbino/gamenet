using UnityEngine;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _sensitivity;
    [SerializeField] private GameObject _playerCanvas;

    void Start() => _playerCanvas.SetActive(false);

    void Update()
    {
        if (!isLocalPlayer) return;

        Movement();
        MouseLook();
    }

    private void Movement()
    {
        Vector3 moveDirection = Vector3.zero;

        // UP, DOWN, LEFT, RIGHT
        if (Input.GetKey(KeyCode.W)) moveDirection += transform.forward;
        if (Input.GetKey(KeyCode.S)) moveDirection -= transform.forward;
        if (Input.GetKey(KeyCode.A)) moveDirection -= transform.right;
        if (Input.GetKey(KeyCode.D)) moveDirection += transform.right;

        moveDirection.y = 0f;
        moveDirection.Normalize();
        transform.position += _moveSpeed * Time.deltaTime * moveDirection;
    }

    private void MouseLook()
    {
        float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * _sensitivity;
        transform.localEulerAngles = new Vector3(0, rotationX, 0);
    }

    [Command(requiresAuthority = false)] // Inform server this player got the ball
    private void CmdSendPlayerAction(bool isEnter) => RpcShowPlayerAction(isEnter);

    [ClientRpc] // Inform everyone which player has the ball
    private void RpcShowPlayerAction(bool isShow) => _playerCanvas.SetActive(isShow);

    void OnCollisionStay(Collision collision) // Detect if this player stays touching the ball
    {
        if (collision.gameObject.GetComponent<Ball>())
            CmdSendPlayerAction(true);
    }

    void OnCollisionExit(Collision collision) // Detect if this player stops touching the ball
    {
        if (collision.gameObject.GetComponent<Ball>())
            CmdSendPlayerAction(false);
    }
}
