using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class Prop_enemySPawner : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] List<GameObject> props = new List<GameObject>();
    [SerializeField] Vector3 boxDimension;
    [SerializeField] int propAmount = 3;
    [SerializeField] float checkdistance = 1f;
    [SerializeField] LayerMask proplayer;

    [Header("References")]
    [SerializeField] Transform enemyParent;
    [SerializeField] NavMeshSurface navMeshSurface;

    private int maxAttmepts = 30;
    private int trys = 0;
    private int spawned = 0;

    private void Start()
    {
        // 1. Intentar encontrar o crear el contenedor de props
        if (enemyParent == null)
        {
            GameObject store = GameObject.Find("propsStore");
            if (store == null)
            {
                store = new GameObject("propsStore");
            }
            enemyParent = store.transform;
        }

        // 2. Intentar encontrar el NavMeshSurface (indispensable para NavMesh.SamplePosition)
        if (navMeshSurface == null)
        {
            navMeshSurface = FindFirstObjectByType<NavMeshSurface>();

            // Si falla por tipo, intentamos por el nombre específico que usabas
            if (navMeshSurface == null)
            {
                GameObject baker = GameObject.Find("nabmeshbaker");
                if (baker != null) navMeshSurface = baker.GetComponent<NavMeshSurface>();
            }
        }

        // 3. Verificación de seguridad antes de empezar
        if (props == null || props.Count == 0)
        {
            Debug.LogError($"[Prop_enemySPawner] No hay prefabs asignados en la lista 'props' de {gameObject.name}");
            return;
        }

        SpawnProp();
    }

    private void SpawnProp()
    {
        while (spawned < propAmount && trys < maxAttmepts)
        {
            trys++;

            // Calcular posición aleatoria dentro del área definida
            float randX = Random.Range(-boxDimension.x / 2f, boxDimension.x / 2f);
            float randZ = Random.Range(-boxDimension.z / 2f, boxDimension.z / 2f);
            Vector3 randPos = transform.position + new Vector3(randX, 0, randZ);

            // 1. Verificar que no haya otros props cerca
            Collider[] hits = Physics.OverlapSphere(randPos, checkdistance, proplayer);

            if (hits.Length == 0)
            {
                // 2. Verificar que la posición esté sobre el NavMesh (suelo válido)
                // Usamos un radio de 2f para ser un poco más generosos al detectar el suelo
                if (NavMesh.SamplePosition(randPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    int randomPropIndex = Random.Range(0, props.Count);

                    // Instanciar en la posición exacta del NavMesh encontrada
                    GameObject prop = Instantiate(props[randomPropIndex], hit.position, Quaternion.identity);

                    // Asignar padre
                    if (enemyParent != null)
                        prop.transform.parent = enemyParent;

                    spawned++;
                }
            }
        }

        if (spawned < propAmount)
        {
            Debug.LogWarning($"[Prop_enemySPawner] Solo se pudieron spawnear {spawned}/{propAmount} props después de {trys} intentos.");
        }
    }

    // Dibujar el área en el Editor para que sea fácil de configurar
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(boxDimension.x, 0.1f, boxDimension.z));
    }
}