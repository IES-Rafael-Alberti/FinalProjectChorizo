using UnityEngine;
using Mirror;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : NetworkBehaviour
{
    private NavMeshAgent agent;
    
    [Header("Stats del Enemigo")]
    [SerializeField] private int enemyHealth = 3;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    [ServerCallback]
    void Update()
    {
        GameObject target = FindClosestPlayer();
        if (target != null)
        {
            agent.SetDestination(target.transform.position);
        }
        else
        {
            // Si todos están muertos, el enemigo se queda quieto
            agent.SetDestination(transform.position);
        }
    }

    [Server]
    private GameObject FindClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject closest = null;
        float minDistance = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        foreach (GameObject p in players)
        {
            PlayerController pc = p.GetComponent<PlayerController>();
            // IGNORA a los jugadores muertos
            if (pc != null && pc.isDead) continue; 

            float dist = Vector3.Distance(p.transform.position, currentPos);
            if (dist < minDistance)
            {
                closest = p;
                minDistance = dist;
            }
        }
        return closest;
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            // Solo ataca si el jugador no está muerto
            if (player != null && !player.isDead)
            {
                player.TakeDamage(); 
            }
        }
    }

    // --- NUEVO: MÉTODO PARA QUE MATES AL ENEMIGO ---
    [Server]
    public void TakeDamage(int damage)
    {
        enemyHealth -= damage;
        if (enemyHealth <= 0)
        {
            Die();
        }
    }

    [Server]
    private void Die()
    {
        // 1. Destruimos al enemigo de la red
        NetworkServer.Destroy(gameObject);
        
        // 2. Avisamos al Spawner para que verifique si todos los enemigos están muertos
        if (EnemySpawner.instance != null)
        {
            EnemySpawner.instance.CheckEnemiesAndRespawn();
        }
    }
}