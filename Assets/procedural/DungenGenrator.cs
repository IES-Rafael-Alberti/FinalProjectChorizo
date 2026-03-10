using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class DungenGenrator : MonoBehaviour
{
    public static DungenGenrator Instance { get; private set; }

    [Header("Prefabs & Settings")]
    [SerializeField] GameObject entrance;
    [SerializeField] List<GameObject> rooms = new List<GameObject>();
    [SerializeField] List<GameObject> specailroom = new List<GameObject>();
    [SerializeField] List<GameObject> alternateEntrance = new List<GameObject>();
    [SerializeField] List<GameObject> hallways = new List<GameObject>();
    [SerializeField] GameObject door;
    [SerializeField] int numberofrooms;
    [Tooltip("Minimum number of rooms before special can appear (after entrance)")]
    [SerializeField] int minRoomsBeforeSpecial = 3;

    [Header("Seed Settings (this script only)")]
    [SerializeField] bool useCustomSeed = false;
    [SerializeField] int seed = 0;

    [SerializeField] LayerMask roomLayer;
    [SerializeField] NavMeshSurface navMeshSurface;

    private List<DungenPart> genratedRoomSL = new List<DungenPart>();
    private bool specialPlaced = false;

    private void Awake()
    {
        Instance = this;

        // CORRECCIÓN: Aseguramos que navMeshSurface sea un objeto de la ESCENA, no un asset
        if (navMeshSurface == null || !navMeshSurface.gameObject.scene.IsValid())
        {
            navMeshSurface = FindFirstObjectByType<NavMeshSurface>();

            if (navMeshSurface == null)
            {
                Debug.LogError("No se encontró un NavMeshSurface en la escena. El generador fallará.");
            }
        }

        if (useCustomSeed)
            UnityEngine.Random.InitState(seed);
    }

    private void Start() => StartGenration();

    public void StartGenration()
    {
        if (navMeshSurface == null) return;

        genratedRoomSL.Clear();
        specialPlaced = false;

        Genrate();
        AlternateEgenrate();
        FillEmptyEntrance();

        // Construye el NavMesh después de generar todo
        navMeshSurface.BuildNavMesh();
    }

    private void Genrate()
    {
        int targetRooms = numberofrooms - alternateEntrance.Count;
        int attempts = 0;
        int maxAttempts = targetRooms * 200;

        while (genratedRoomSL.Count < targetRooms)
        {
            attempts++;
            if (attempts > maxAttempts)
            {
                Debug.LogWarning($"Dungeon generation aborted: exceeded {maxAttempts} attempts.");
                break;
            }

            // Place entrance first
            if (genratedRoomSL.Count == 0)
            {
                if (entrance == null) { Debug.LogError("Prefab de Entrance no asignado"); break; }

                var entryObj = Instantiate(entrance, transform.position, transform.rotation, navMeshSurface.transform);
                if (entryObj.TryGetComponent(out DungenPart entryPart))
                    genratedRoomSL.Add(entryPart);
                continue;
            }

            bool lastSlot = genratedRoomSL.Count == targetRooms - 1;

            DungenPart baseRoom = null;
            Transform entry1 = null;

            for (int i = 0; i < 800; i++)
            {
                int idx = UnityEngine.Random.Range(0, genratedRoomSL.Count);
                if (genratedRoomSL[idx].HasAvialableEntryPoint(out entry1))
                {
                    baseRoom = genratedRoomSL[idx];
                    break;
                }
            }

            if (baseRoom == null) continue;

            var doorObj = Instantiate(door, entry1.position, entry1.rotation, navMeshSurface.transform);

            if (UnityEngine.Random.value > 0.5f && hallways.Count > 0)
            {
                TrySpawnHallway(baseRoom, entry1, doorObj);
                continue;
            }

            TrySpawnRoom(baseRoom, entry1, doorObj, lastSlot);
        }
    }

    private void TrySpawnHallway(DungenPart baseRoom, Transform entry1, GameObject doorObj)
    {
        var hallwayPrefab = hallways[UnityEngine.Random.Range(0, hallways.Count)];
        var hallwayObj = Instantiate(hallwayPrefab, navMeshSurface.transform);

        if (!hallwayObj.TryGetComponent(out DungenPart hallPart)
            || !hallPart.HasAvialableEntryPoint(out Transform entry2))
        {
            Destroy(hallwayObj); Destroy(doorObj); return;
        }

        AlignRoom(baseRoom.transform, hallwayObj.transform, entry1, entry2);

        if (HandelInterscetion(hallPart))
        {
            hallPart.UnuseEntryPoint(entry2);
            baseRoom.UnuseEntryPoint(entry1);
            Destroy(hallwayObj);
            Destroy(doorObj);
            return;
        }

        genratedRoomSL.Add(hallPart);
    }

    private void TrySpawnRoom(DungenPart baseRoom, Transform entry1, GameObject doorObj, bool lastSlot)
    {
        GameObject prefab;
        if (lastSlot && !specialPlaced && specailroom.Count > 0)
            prefab = specailroom[UnityEngine.Random.Range(0, specailroom.Count)];
        else if (!specialPlaced && genratedRoomSL.Count >= minRoomsBeforeSpecial
                 && specailroom.Count > 0 && UnityEngine.Random.value > 0.9f)
            prefab = specailroom[UnityEngine.Random.Range(0, specailroom.Count)];
        else if (rooms.Count > 0)
            prefab = rooms[UnityEngine.Random.Range(0, rooms.Count)];
        else
            return;

        var roomObj = Instantiate(prefab, navMeshSurface.transform);
        if (!roomObj.TryGetComponent(out DungenPart newPart)
            || !newPart.HasAvialableEntryPoint(out Transform entry2))
        {
            Destroy(roomObj); Destroy(doorObj); return;
        }

        AlignRoom(baseRoom.transform, roomObj.transform, entry1, entry2);

        if (HandelInterscetion(newPart))
        {
            newPart.UnuseEntryPoint(entry2);
            baseRoom.UnuseEntryPoint(entry1);
            Destroy(roomObj); Destroy(doorObj); return;
        }

        if (specailroom.Contains(prefab)) specialPlaced = true;
        genratedRoomSL.Add(newPart);
    }

    void AlternateEgenrate() { }

    void FillEmptyEntrance() { foreach (var r in genratedRoomSL) r.FillEmptyWalls(); }

    bool HandelInterscetion(DungenPart part)
    {
        // CORRECCIÓN: Verificamos si el collider existe para evitar NullReferenceException
        Collider col = part.GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"El prefab {part.name} no tiene Collider. No se puede detectar colisión.");
            return false;
        }

        var halfSize = col.bounds.size / 2;
        var hits = Physics.OverlapBox(col.bounds.center, halfSize, Quaternion.identity, roomLayer);

        foreach (var hit in hits)
            if (hit != col)
                return true;
        return false;
    }

    void AlignRoom(Transform parent, Transform toAlign, Transform fromEntry, Transform toEntry)
    {
        float angle = Vector3.SignedAngle(toEntry.forward, -fromEntry.forward, Vector3.up);
        toAlign.Rotate(Vector3.up, angle);
        toAlign.position += fromEntry.position - toEntry.position;
        Physics.SyncTransforms();
    }
}