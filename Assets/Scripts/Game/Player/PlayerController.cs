using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement; // Necesario para leer el nombre de la escena

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour 
{
    private CharacterController controller;

    [Header("Referencias")]
    public Transform playerCamera;

    [Header("Stats de Movimiento")]
    public float MaxSpeed = 8f;
    public float Gravity = -30f;
    public float JumpHeight = 2f;

    [Header("Stats de Cámara (Ratón)")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;

    private Vector3 playerVelocity;
    private bool groundedPlayer;
    private float cameraPitch = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        // Solo bloqueamos el cursor al nacer si ya estamos en la escena de juego.
        // Si estamos en SteamScene (Lobby), lo dejamos libre para poder clickear.
        if (isLocalPlayer && SceneManager.GetActiveScene().name == "GameScene")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // REGLA DEL LOBBY: Si NO estamos en GameScene, no leemos teclado ni ratón.
        // Así evitamos movernos por detrás de la UI del menú.
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

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Movimiento relativo hacia donde mira el jugador
        Vector3 move = (transform.right * h + transform.forward * v).normalized;

        controller.Move(move * Time.deltaTime * MaxSpeed);

        // Salto
        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            playerVelocity.y += Mathf.Sqrt(JumpHeight * -3.0f * Gravity);
        }

        // Gravedad
        playerVelocity.y += Gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }
}