using UnityEngine;
using Mirror;

public class EnemySpawner : NetworkBehaviour
{
    public static EnemySpawner instance;

    [Header("Configuración del Spawner")]
    [SerializeField] private GameObject enemyPrefab;
    
    [Tooltip("Cantidad máxima de enemigos vivos al mismo tiempo en el mapa")]
    [SerializeField] private int maxEnemies = 10;
    
    [Tooltip("Tiempo en segundos entre la aparición de cada enemigo")]
    [SerializeField] private float spawnRate = 4f; 
    
    [SerializeField] private float spawnRadius = 30f;

    // Temporizador para controlar el spawn en tiempo real
    private float spawnTimer = 0f;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    [ServerCallback]
    private void Update()
    {
        // Solo spawneamos si no hemos llegado al límite máximo de enemigos
        if (GameObject.FindGameObjectsWithTag("Enemy").Length < maxEnemies)
        {
            spawnTimer += Time.deltaTime;
            
            // Si el temporizador supera la tasa de spawneo, creamos un enemigo
            if (spawnTimer >= spawnRate)
            {
                SpawnEnemy();
                spawnTimer = 0f; // Reiniciamos el contador
            }
        }
    }

    [Server]
    private void SpawnEnemy()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = new Vector3(randomCircle.x, 1f, randomCircle.y);

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        NetworkServer.Spawn(enemy);
    }

    [Server]
    public void CheckEnemiesAndRespawn()
    {
        // Retrasamos la comprobación un poco para que a Unity le dé tiempo a destruir el enemigo
        Invoke(nameof(VerifyAndRespawn), 0.1f);
    }

    [Server]
    private void VerifyAndRespawn()
    {
        int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
        
        // Si no quedan enemigos vivos, revivimos a los jugadores caídos
        if (enemyCount == 0)
        {
            PlayerController[] players = FindObjectsOfType<PlayerController>();
            foreach (PlayerController player in players)
            {
                if (player.isDead)
                {
                    player.Respawn();
                }
            }
        }
    }

    [Server]
    public void CheckPlayersState()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        bool everyoneDead = true;

        foreach (PlayerController player in players)
        {
            if (!player.isDead)
            {
                everyoneDead = false;
                break;
            }
        }

        // Si ya no queda nadie vivo (ej: juegas solo y mueres), reiniciamos la ronda
        if (everyoneDead)
        {
            Invoke(nameof(RestartRound), 3f); 
        }
    }

    [Server]
    private void RestartRound()
    {
        // 1. Limpiamos todos los enemigos del mapa
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            NetworkServer.Destroy(enemy);
        }

        // 2. Revivimos a todos los jugadores
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in players)
        {
            player.Respawn();
        }
    }
}