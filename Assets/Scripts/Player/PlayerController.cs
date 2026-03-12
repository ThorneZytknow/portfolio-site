using UnityEngine;
using BrawlerShared.Enums;
using BrawlerShared.Packets;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Identificação Rede Local")]
    public bool isLocalPlayer = false;
    public int actorNumber; // Substitui o OwnerActorNr do Photon

    [Header("Movimentação")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;

    [Header("Física e Colisão")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isGrounded;
    private float moveInput;
    private bool facingRight = true;

    // Variáveis para sincronização de posição do Servidor Customizado
    private Vector2 networkPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
    }

    private void Start()
    {
        if (LocalServerClient.Instance != null)
        {
            LocalServerClient.Instance.OnAnyPacketReceived += HandleNetworkPackets;
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            ProcessInputs();
        }
        else
        {
            SmoothMovement();
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        CheckGrounded();
        MovePlayer();

        // Em vez de IPunObservable (Photon), envia Inputs/Posições via pacote UDP/TCP próprio
        SendSyncPacket();
    }

    private void ProcessInputs()
    {
        moveInput = Input.GetAxis("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();
    }

    private void MovePlayer()
    {
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    private void SmoothMovement()
    {
        transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
    }

    private void SendSyncPacket()
    {
        if (LocalServerClient.Instance == null) return;

        var inputPacket = new GamePlayerInput
        {
            InputX = moveInput,
            InputY = rb.velocity.y,
            JumpPressed = Input.GetButton("Jump"),
            AttackPressed = Input.GetButton("Fire1")
        };

        // UDP seria mais rápido, mas aqui demonstramos a lógica agnóstica via Socket customizado
        LocalServerClient.Instance.SendPacket(PacketType.Game_PlayerInput, inputPacket);
    }

    private void HandleNetworkPackets(BasePacket packet)
    {
        // Intercepta GameState global do servidor e atualiza posições
        if (packet.Type == PacketType.Game_StateSync)
        {
            var sync = packet.GetPayload<GameStateSync>();
            if (sync.Players != null && sync.Players.TryGetValue(actorNumber, out var stateData))
            {
                if (!isLocalPlayer)
                {
                    networkPosition = new Vector2(stateData.PosX, stateData.PosY);
                    rb.velocity = new Vector2(stateData.VelX, stateData.VelY);
                }
            }
        }
    }
}
