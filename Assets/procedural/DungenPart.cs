using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungenPart : MonoBehaviour
{
    public enum DungenPartType
    {
        Room,
        Halway
    }

    [SerializeField]
    LayerMask roomslayer;
    [SerializeField]
    DungenPartType dungenPartType;
    [SerializeField]
    GameObject fillerwall;

    public List<Transform> entrypointsSL = new List<Transform>();

    public new Collider collider;

    public bool HasAvialableEntryPoint(out Transform entrypoint)
    {
        Transform resultEntry = null;
        bool result = false;

        int totalReties = 100;
        int retryIndex = 0;

        //Debug.Log("Checking entry points for: " + gameObject.name);

        if (entrypointsSL.Count == 1)
        {
            Transform entry = entrypointsSL[0];
            if (entry == null)
            {
                Debug.LogError("Single entry point is NULL in " + gameObject.name);
            }

            if (entry.TryGetComponent<entrypoint>(out entrypoint resComp))
            {
              //  Debug.Log("Found entrypoint component on single entry");
                if (!resComp.IsOccupied())
                {
                    result = true;
                    resultEntry = entry;
                    resComp.SetOccupied();
                }
            }
            else
            {
                Debug.LogError("Missing entrypoint component on: " + entry.name);
            }

            entrypoint = resultEntry;
            return result;
        }

        while (retryIndex < totalReties && resultEntry == null)
        {
            int randomEntryIndex = Random.Range(0, entrypointsSL.Count);
            Transform entry = entrypointsSL[randomEntryIndex];

            if (entry == null)
            {
                Debug.LogError("Random entrypoint is NULL in list for " + gameObject.name);
            }

            if (entry.TryGetComponent<entrypoint>(out entrypoint resComp))
            {
                if (!resComp.IsOccupied())
                {
                    resultEntry = entry;
                    result = true;
                    resComp.SetOccupied();
                    break;
                }
            }

            retryIndex++;
        }

        if (resultEntry == null)
        {
            Debug.LogWarning("No free entrypoint found for " + gameObject.name);
        }

        entrypoint = resultEntry;
        return result;

    }


    public void UnuseEntryPoint(Transform entrypoint)
    {
        if (entrypoint.TryGetComponent<entrypoint>(out entrypoint entry))
        {
            entry.SetOccupied(false);
        }
    }

    public void FillEmptyWalls()
    {
        entrypointsSL.ForEach((entry) =>

        {
            if (entry.TryGetComponent<entrypoint>(out entrypoint res))
            {
                if (!res.IsOccupied())
                {
                    GameObject wall = Instantiate(fillerwall);
                    wall.transform.position = entry.transform.position;
                    wall.transform.rotation = entry.transform.rotation;

                }
            }
        }

        );
    }
}