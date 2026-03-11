using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour 
{
    private CharacterController controller;

    [Header("Referencias")]
    public Transform playerCamera;
    private Camera camComponent;

    [Header("Stats de Movimiento")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float acceleration = 4f; 
    public float Gravity = -30f;
    public float JumpHeight = 2f;

    [Header("Stats de Cámara")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;
    
    [Header("Efectos de Velocidad (FOV)")]
    public float normalFOV = 60f;
    public float sprintFOV = 75f;
    public float fovChangeSpeed = 8f;

    [Header("Salud y Daño")]
    [SerializeField] private int maxHits = 3;
    [SyncVar] private int currentHits;
    [SyncVar] public bool isDead = false; // Variable para saber si estamos muertos

    [Header("Efecto Visual de Hit")]
    [SerializeField] private Image damageScreenImage;
    [SerializeField] private Color hitColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private float hitFlashFadeSpeed = 5f;

    private Vector3 currentMoveVelocity; 
    private float verticalVelocity;      
    private bool groundedPlayer;
    private float cameraPitch = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera != null) camComponent = playerCamera.GetComponent<Camera>();
    }

    private void Start()
    {
        if (isLocalPlayer && SceneManager.GetActiveScene().name == "GameScene")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (isLocalPlayer && damageScreenImage != null) damageScreenImage.color = Color.clear;
    }

    public override void OnStartServer()
    {
        currentHits = maxHits;
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // Efecto visual de daño
        if (damageScreenImage != null && damageScreenImage.color != Color.clear)
        {
            damageScreenImage.color = Color.Lerp(damageScreenImage.color, Color.clear, hitFlashFadeSpeed * Time.deltaTime);
        }

        // Si estamos muertos o no estamos en el juego, no nos movemos
        if (isDead || SceneManager.GetActiveScene().name != "GameScene") return;

        ManejarCamaraFPS();
        ManejarMovimiento();
    }

    [Server]
    public void TakeDamage()
    {
        if (isDead) return;

        currentHits--;
        TargetShowHitEffect(connectionToClient);

        if (currentHits <= 0)
        {
            Die();
        }
    }

    [TargetRpc]
    public void TargetShowHitEffect(NetworkConnection target)
    {
        if (damageScreenImage != null) damageScreenImage.color = hitColor;
    }

    [Server]
    private void Die()
    {
        isDead = true;
        RpcOnDeath(); // Entrar en modo muerto

        // Avisamos al Spawner para que compruebe si era el último jugador vivo
        if (EnemySpawner.instance != null)
        {
            EnemySpawner.instance.CheckPlayersState();
        }
    }

    [ClientRpc]
    private void RpcOnDeath()
    {
        if (isLocalPlayer)
        {
            Debug.Log("<color=red>¡HAS MUERTO! Espera a que maten a los enemigos restantes...</color>");
            controller.enabled = false;
            // Opcional: Te movemos arriba para simular modo espectador temporalmente
            transform.position = new Vector3(0, 50, 0); 
        }
    }

    [Server]
    public void Respawn()
    {
        isDead = false;
        currentHits = maxHits;
        RpcRespawn();
    }

    [ClientRpc]
    private void RpcRespawn()
    {
        if (isLocalPlayer)
        {
            Debug.Log("<color=green>¡REAPARECES!</color>");
            transform.position = new Vector3(0, 5, 0); // Vuelves a la zona de juego
            controller.enabled = true;
            if (damageScreenImage != null) damageScreenImage.color = Color.clear;
        }
    }

    // -- Lógica normal de movimiento (igual que antes) --
    private void ManejarCamaraFPS()
    {
        if (playerCamera == null) return;
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        playerCamera.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }

    private void ManejarMovimiento()
    {
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && verticalVelocity < 0) verticalVelocity = -2f; 
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool isMovingForward = v > 0;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && isMovingForward;
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        if (camComponent != null)
        {
            float targetFOV = isSprinting ? sprintFOV : normalFOV;
            camComponent.fieldOfView = Mathf.Lerp(camComponent.fieldOfView, targetFOV, fovChangeSpeed * Time.deltaTime);
        }
        Vector3 moveDirection = (transform.right * h + transform.forward * v).normalized;
        Vector3 targetVelocity = moveDirection * targetSpeed;
        currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, acceleration * Time.deltaTime);
        if (Input.GetButtonDown("Jump") && groundedPlayer) verticalVelocity = Mathf.Sqrt(JumpHeight * -3.0f * Gravity);
        verticalVelocity += Gravity * Time.deltaTime;
        Vector3 finalVelocity = currentMoveVelocity + (Vector3.up * verticalVelocity);
        controller.Move(finalVelocity * Time.deltaTime);
    }
}