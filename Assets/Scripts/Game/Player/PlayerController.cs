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
    private Camera cam;

    [Header("Stats de Movimiento")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float acceleration = 10f; // Qué tan rápido alcanza la velocidad máxima o frena
    public float Gravity = -30f;
    public float JumpHeight = 2f;

    [Header("Stats de Cámara (Ratón)")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;
    public float normalFOV = 60f;
    public float sprintFOV = 75f;
    public float fovChangeSpeed = 8f;

    [Header("Sistema de Vida y Daño")]
    public int maxHits = 3;
    
    [SyncVar(hook = nameof(OnHitsChanged))]
    public int currentHits; // La vida actual sincronizada en red
    
    [SyncVar] 
    public bool isDead = false;
    
    [Header("UI Daño y Maná")]
    public Image damageScreenImage;
    public Color hitColor = new Color(1f, 0f, 0f, 0.5f);
    public float hitFlashFadeSpeed = 5f;
    public Slider manaHealthBar; // Tu nueva barra azul

    private Vector3 playerVelocity; // Velocidad vertical (gravedad/salto)
    private Vector3 currentMoveVelocity; // Velocidad horizontal actual (para suavizado)
    private bool groundedPlayer;
    private float cameraPitch = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera != null)
        {
            cam = playerCamera.GetComponent<Camera>();
        }
    }

    public override void OnStartServer()
    {
        // Al nacer en el servidor, empezamos con la vida al máximo
        currentHits = maxHits;
        isDead = false;
    }

    private void Start()
    {
        // Solo bloqueamos el cursor al nacer si ya estamos en la escena de juego.
        if (isLocalPlayer && SceneManager.GetActiveScene().name == "GameScene")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Si somos el jugador local, configuramos nuestra UI
        if (isLocalPlayer)
        {
            if (manaHealthBar != null)
            {
                manaHealthBar.maxValue = maxHits;
                manaHealthBar.value = currentHits; 
            }
        }
        else
        {
            // Apagamos la UI de los otros jugadores en nuestra pantalla
            if (manaHealthBar != null) manaHealthBar.gameObject.SetActive(false);
            if (damageScreenImage != null) damageScreenImage.gameObject.SetActive(false);
        }
    }

    // Este Hook se ejecuta automáticamente cuando el servidor cambia la vida "currentHits"
    private void OnHitsChanged(int oldHits, int newHits)
    {
        if (isLocalPlayer)
        {
            // Actualizar visualmente la barra de maná
            if (manaHealthBar != null)
            {
                manaHealthBar.value = newHits;
            }

            // Efecto de daño en pantalla si perdimos vida
            if (newHits < oldHits && damageScreenImage != null)
            {
                damageScreenImage.color = hitColor;
            }
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // Desvanecer el pantallazo rojo gradualmente (incluso si está muerto)
        if (damageScreenImage != null && damageScreenImage.color.a > 0)
        {
            damageScreenImage.color = Color.Lerp(damageScreenImage.color, Color.clear, hitFlashFadeSpeed * Time.deltaTime);
        }

        if (isDead) return;

        // REGLA DEL LOBBY: Si NO estamos en GameScene, no leemos teclado ni ratón.
        if (SceneManager.GetActiveScene().name != "GameScene") return;

        ManejarCamaraFPS();
        ManejarMovimiento();
    }

    private void ManejarCamaraFPS()
    {
        if (playerCamera == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotar el cuerpo (Horizontal)
        transform.Rotate(Vector3.up * mouseX);

        // Rotar la cámara (Vertical)
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        
        playerCamera.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }

    private void ManejarMovimiento()
    {
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; 
        }

        // 1. Leer Inputs
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 2. Determinar si estamos sprintando
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && v > 0;
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // Efecto del FOV (Zoom) al esprintar
        if (cam != null)
        {
            float targetFOV = isSprinting ? sprintFOV : normalFOV;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovChangeSpeed * Time.deltaTime);
        }

        // 3. Calcular la dirección de movimiento deseada
        Vector3 moveDirection = (transform.right * h + transform.forward * v).normalized;
        Vector3 targetVelocity = moveDirection * targetSpeed;

        // 4. Suavizar el movimiento (Aceleración/Desaceleración)
        currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, acceleration * Time.deltaTime);
        controller.Move(currentMoveVelocity * Time.deltaTime);

        // 5. Salto
        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            playerVelocity.y += Mathf.Sqrt(JumpHeight * -3.0f * Gravity);
        }

        // 6. Gravedad (Movimiento vertical)
        playerVelocity.y += Gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    // --- LÓGICA DE RECIBIR DAÑO ---
    [Server]
    public void TakeDamage(int damage = 1)
    {
        if (isDead) return;

        currentHits -= damage;

        if (currentHits <= 0)
        {
            currentHits = 0;
            Die();
        }
    }

    [Server]
    private void Die()
    {
        isDead = true;
        Debug.Log("El jugador " + netId + " ha muerto.");
        RpcOnDie();
    }

    [ClientRpc]
    private void RpcOnDie()
    {
        // Opcional: Puedes desactivar mallas o disparar animaciones de muerte aquí
    }

    // --- LÓGICA DE RESPAWN (LO QUE PEDÍA EL ENEMY SPAWNER) ---
    [Server]
    public void Respawn()
    {
        if (!isDead) return;

        isDead = false;
        currentHits = maxHits; // Restaurar la vida al máximo
        
        // Mover al jugador de vuelta a un punto seguro al revivir (Descomentar si es necesario)
        // transform.position = new Vector3(0, 2f, 0); 

        Debug.Log("El jugador " + netId + " ha revivido.");
        RpcOnRespawn();
    }

    [ClientRpc]
    private void RpcOnRespawn()
    {
        // Limpiar la pantalla roja de daño por si acaso
        if (damageScreenImage != null)
        {
            damageScreenImage.color = Color.clear;
        }

        // Restaurar la barra de maná visualmente en el cliente local
        if (isLocalPlayer && manaHealthBar != null)
        {
            manaHealthBar.value = maxHits;
        }
    }
}