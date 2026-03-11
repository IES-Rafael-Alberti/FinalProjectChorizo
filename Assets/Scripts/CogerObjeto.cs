using UnityEngine;
using Mirror;

public class CogerObjeto : NetworkBehaviour
{
    public GameObject handPoint;
    private GameObject pickedObject = null;
    private GameObject objectInRange = null;

    void Update()
    {
        if (!isLocalPlayer) return;

        // SOLTAR OBJETO
        if (pickedObject != null && Input.GetKeyDown(KeyCode.Q))
        {
            CmdDropObject(pickedObject);
        }

        // COGER OBJETO
        if (objectInRange != null && pickedObject == null && Input.GetKeyDown(KeyCode.E))
        {
            CmdPickUpObject(objectInRange);
        }
    }

    [Command]
    private void CmdPickUpObject(GameObject obj)
    {
        // 1. EL SERVIDOR apaga la física para que deje de chocar o caer
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 2. Avisamos a todos para que lo peguen a la mano
        RpcPickUpObject(obj);
    }

    [ClientRpc]
    private void RpcPickUpObject(GameObject obj)
    {
        // Solo el jugador que pulsó la E se lo guarda como su objeto agarrado
        if (isLocalPlayer && obj == objectInRange)
        {
            pickedObject = obj;
        }

        // Todos los clientes lo emparentan a la mano de tu avatar
        obj.transform.SetParent(handPoint.transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    [Command]
    private void CmdDropObject(GameObject obj)
    {
        // 1. EL SECRETO: El Servidor enciende la física aquí. 
        // Como él manda (Server To Client), empezará a caer y todos lo verán caer.
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // 2. Ordenamos a los clientes soltarlo visualmente
        RpcDropObject(obj);
    }

    [ClientRpc]
    private void RpcDropObject(GameObject obj)
    {
        // Desemparentamos
        obj.transform.SetParent(null);

        // Limpiamos la variable solo en tu pantalla
        if (isLocalPlayer && pickedObject == obj)
        {
            pickedObject = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isLocalPlayer) return;
        if (other.CompareTag("Objets")) objectInRange = other.gameObject;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isLocalPlayer) return;
        if (other.CompareTag("Objets") && objectInRange == other.gameObject)
        {
            objectInRange = null;
        }
    }
}