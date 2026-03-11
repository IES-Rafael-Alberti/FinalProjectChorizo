using UnityEngine;
using Mirror;

public class LocalComponentChecker : NetworkBehaviour
{
    [SerializeField] private GameObject playerCam; 
    
    // Ahora llama al nuevo PlayerController
    private PlayerController playerController; 
    
    private void Start()
    {
        if (!isLocalPlayer)
        {
            playerController = GetComponent<PlayerController>();

            if (playerController != null) playerController.enabled = false;

            if (playerCam != null) playerCam.SetActive(false);
        }
    }
}