using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    // --- LÓGICA DE ESCENAS ---

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
        ConfigurarCursor(true);
        Debug.Log("CLIENTE: Escena cambiada y cursor bloqueado.");
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        ConfigurarCursor(true);
        Debug.Log($"SERVIDOR: Escena cambiada a {sceneName}");
    }

    private void ConfigurarCursor(bool bloqueado)
    {
        Cursor.visible = !bloqueado;
        Cursor.lockState = bloqueado ? CursorLockMode.Locked : CursorLockMode.None;
    }

    // --- CONTROL DE CONEXIÓN Y LIMPIEZA (Anti-NullReference) ---

    public override void OnStopServer()
    {
        base.OnStopServer();
        ConfigurarCursor(false); // Liberamos el cursor al cerrar el server
        LimpiarTransporte();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        ConfigurarCursor(false); // Liberamos el cursor al salir
    }

    private void LimpiarTransporte()
    {
        // Esto evita que FizzySteam intente cerrar sockets nulos o ya cerrados
        if (transport is Mirror.FizzySteam.FizzyFacepunch fizzy)
        {
            try
            {
                fizzy.Shutdown();
                Debug.Log("SERVIDOR: Transporte Steam cerrado correctamente.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Limpieza de transporte omitida: {e.Message}");
            }
        }
    }

    // --- CARGA DE JUEGO ---

    public void ChangeScene()
    {
        if (NetworkServer.active)
        {
            // Usamos Invoke para dar un margen de tiempo antes de la transición
            Invoke(nameof(ChangingScene), 3f);
        }
    }

    private void ChangingScene()
    {
        // Cambia el nombre "GameScene" por el nombre exacto de tu escena de juego
        ServerChangeScene("GameScene");
    }

    // --- GESTIÓN DE JUGADORES ---

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // Previene el error "Already a player for this connection"
        if (conn.identity != null)
        {
            Debug.LogWarning($"[Mirror] El cliente {conn.connectionId} ya tiene un jugador. Saltando spawn.");
            return;
        }

        base.OnServerAddPlayer(conn);
    }

    // --- LOGS DE DEPURACIÓN ---

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log("<color=green>SERVIDOR: Cliente conectado.</color>");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("<color=blue>CLIENTE: Conectado al Host.</color>");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        ConfigurarCursor(false);
        Debug.Log("<color=red>CLIENTE: Desconectado del servidor.</color>");
    }
}