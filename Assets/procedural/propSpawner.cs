using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class propSpawner : MonoBehaviour
{
    [SerializeField]
    List<GameObject> props = new List<GameObject>();
    [SerializeField]
    Vector3 boxDimension;
    [SerializeField]
    int propAmount = 3;
    int maxAttmepts = 30 ;
    int trys = 0;
    int spawned;
    [SerializeField]
    float checkdistance;
    [SerializeField]
    LayerMask proplayer;

   

    private void Start()
    {
        SpawnProp();
    }
    private void SpawnProp()
    {

        while (spawned < propAmount && trys < maxAttmepts)
        {
            trys++;
            int randomPropIndex = Random.Range(0, props.Count);
            Vector3 randPos = transform.position + new Vector3((Random.Range(-boxDimension.x / 2, boxDimension.x / 2)), 0, (Random.Range(-boxDimension.z / 2, boxDimension.z / 2)));

            Collider[] hits = Physics.OverlapSphere(randPos,checkdistance,proplayer );

            if (hits.Length == 0)
            {
                GameObject prop = Instantiate(props[randomPropIndex], randPos, Quaternion.identity);
                prop.transform.SetParent(transform);
                spawned++;
            }

        }
        
    }
}
