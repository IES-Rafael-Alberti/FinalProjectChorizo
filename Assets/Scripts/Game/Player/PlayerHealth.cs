using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class PlayerHealth : NetworkBehaviour
{
    [Header("UI Barra de Vida/Maná")]
    public Slider manaHealthBar;

    [Header("Estadísticas")]
    public int maxHealth = 100;

    // SyncVar asegura que cuando el servidor cambie la vida, los clientes actualicen su barra
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth;

    public override void OnStartServer()
    {
        // Al empezar, el servidor establece la vida al máximo
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // Configurar la barra en el cliente
        if (manaHealthBar != null)
        {
            manaHealthBar.maxValue = maxHealth;
            manaHealthBar.value = currentHealth;
            
            // Opcional: Ocultar el HUD_Canvas o el Slider si NO somos el jugador local
            // para no ver la barra de vida flotando en la pantalla de otros.
            if (!isLocalPlayer) 
            {
                manaHealthBar.gameObject.SetActive(false);
            }
        }
    }

    // Este método se ejecuta automáticamente cuando currentHealth cambia
    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (manaHealthBar != null)
        {
            manaHealthBar.value = newHealth;
        }
    }

    // --- LÓGICA DE DAÑO (SOLO SERVIDOR) ---
    [Server]
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    [Server]
    private void Die()
    {
        Debug.Log("El jugador " + netId + " ha muerto.");
        // Aquí puedes añadir la lógica de muerte (respawn, animaciones, etc.)
    }
}