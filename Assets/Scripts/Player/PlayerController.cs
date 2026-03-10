using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
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

    // Variáveis para sincronização de posição (Smooth Syncing)
    private Vector2 networkPosition;
    private Quaternion networkRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Garante que a gravidade esteja ativada
        rb.gravityScale = 3f;
    }

    private void Update()
    {
        // Só permite o controle do jogador dono do objeto instanciado via Photon
        if (photonView.IsMine)
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
        // A física só deve ser aplicada no dono local
        if (!photonView.IsMine) return;

        CheckGrounded();
        MovePlayer();
    }

    /// <summary>
    /// Captura entradas do jogador (teclado/controle)
    /// </summary>
    private void ProcessInputs()
    {
        moveInput = Input.GetAxis("Horizontal");

        // Controle de pulo
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        // Inverter sprite de acordo com a direção
        if (moveInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && facingRight)
        {
            Flip();
        }
    }

    /// <summary>
    /// Aplica a velocidade horizontal
    /// </summary>
    private void MovePlayer()
    {
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
    }

    /// <summary>
    /// Aplica a força de pulo
    /// </summary>
    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    /// <summary>
    /// Verifica colisões no chão para permitir pulo
    /// </summary>
    private void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    /// <summary>
    /// Vira a orientação do objeto do jogador
    /// </summary>
    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    /// <summary>
    /// Movimento suave para clientes remotos (interpolação da posição enviada pela rede)
    /// </summary>
    private void SmoothMovement()
    {
        transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
    }

    #region IPunObservable implementation

    /// <summary>
    /// Envia e recebe a posição e rotação pela rede.
    /// Chamado automaticamente pelo PhotonView configurado para observar este componente.
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // O jogador local envia sua posição e rotação
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(rb.velocity); // Envio da velocidade para interpolar melhor se necessário
        }
        else
        {
            // Jogadores remotos recebem a atualização
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            rb.velocity = (Vector2)stream.ReceiveNext(); // Aplica velocidade remotamente

            // Evita que a gravidade e o movimento afetem os clones remotos desativando simulações pesadas (opcional)
            // Em configurações avançadas é possível usar simulação determinística, mas aqui usamos a clássica
        }
    }

    #endregion
}
