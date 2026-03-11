using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour 
{
    private CharacterController controller;

    [Header("Referencias")]
    public Transform playerCamera;
    private Camera camComponent; // Referencia al componente de cámara real

    [Header("Stats de Movimiento")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float acceleration = 4f; 
    public float Gravity = -30f;
    public float JumpHeight = 2f;

    [Header("Stats de Cámara (Ratón)")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;
    
    [Header("Efectos de Velocidad (FOV)")]
    public float normalFOV = 60f;     // FOV normal (El que tienes en tu escena actual)
    public float sprintFOV = 75f;     // FOV al correr (aleja la cámara un poco)
    public float fovChangeSpeed = 8f; // Qué tan rápido hace la transición visual

    private Vector3 currentMoveVelocity; 
    private float verticalVelocity;      
    private bool groundedPlayer;
    private float cameraPitch = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        
        // Buscamos el componente Camera dentro del Transform que asignaste
        if (playerCamera != null)
        {
            camComponent = playerCamera.GetComponent<Camera>();
        }
    }

    private void Start()
    {
        if (isLocalPlayer && SceneManager.GetActiveScene().name == "GameScene")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        if (SceneManager.GetActiveScene().name != "GameScene") return;

        ManejarCamaraFPS();
        ManejarMovimiento();
    }

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
        
        if (groundedPlayer && verticalVelocity < 0)
        {
            verticalVelocity = -2f; 
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Detectar si estamos sprintando (hacia adelante)
        bool isMovingForward = v > 0;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && isMovingForward;
        
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // --- EFECTO DE VELOCIDAD (CÁMARA FOV) ---
        if (camComponent != null)
        {
            // Solo aplicamos el FOV de sprint si el jugador realmente se está moviendo a velocidad de sprint
            float targetFOV = isSprinting ? sprintFOV : normalFOV;
            camComponent.fieldOfView = Mathf.Lerp(camComponent.fieldOfView, targetFOV, fovChangeSpeed * Time.deltaTime);
        }
        // ----------------------------------------

        Vector3 moveDirection = (transform.right * h + transform.forward * v).normalized;
        Vector3 targetVelocity = moveDirection * targetSpeed;

        currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, acceleration * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            verticalVelocity = Mathf.Sqrt(JumpHeight * -3.0f * Gravity);
        }

        verticalVelocity += Gravity * Time.deltaTime;

        Vector3 finalVelocity = currentMoveVelocity + (Vector3.up * verticalVelocity);
        controller.Move(finalVelocity * Time.deltaTime);
    }
}