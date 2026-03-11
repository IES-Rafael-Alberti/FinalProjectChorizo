using TMPro;
using UnityEngine;
using Steamworks;
using Mirror;
using Unity.Services.Vivox;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections;

public class PlayerNameTag : NetworkBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI nameTag;

    [Header("Voz Proximidad")]
    public string channelName = "MisionGlobal";
    public int distanciaMaxima = 30;

    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    private static bool _estaInicializandoServicios = false;
    private static bool _estaLogueandoVivox = false;

    private float nextSpatialUpdate;
    private const float spatialUpdateRate = 0.1f;
    private Transform mainCameraTransform;

    private void Start()
    {
        // Guardamos la referencia de la cámara para optimizar el LateUpdate
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;

        // Ocultamos nuestro propio tag para nosotros mismos
        if (isLocalPlayer && nameTag != null)
        {
            // Desactivamos el Canvas padre del texto para que no use recursos
            nameTag.transform.parent.gameObject.SetActive(false); 
        }
    }

    public override void OnStartLocalPlayer()
    {
        StartCoroutine(SetupPlayerRoutine());
    }

    private IEnumerator SetupPlayerRoutine()
    {
        while (!NetworkClient.ready || !NetworkClient.isConnected)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        string steamName = SteamClient.IsValid ? SteamClient.Name : "Jugador_" + UnityEngine.Random.Range(100, 999);
        CmdSetPlayerName(steamName);
        _ = IniciarVoz(steamName);
    }

    [Command]
    void CmdSetPlayerName(string name) { playerName = name; }

    void OnNameChanged(string old, string n)
    {
        if (nameTag != null) nameTag.text = n;
    }

    private void Update()
    {
        if (VivoxService.Instance == null || !VivoxService.Instance.IsLoggedIn || string.IsNullOrEmpty(playerName) || nameTag == null)
            return;

        bool estaHablando = false;
        foreach (var channel in VivoxService.Instance.ActiveChannels.Values)
        {
            var participant = channel.FirstOrDefault(p => p.DisplayName == playerName.Replace(" ", "_"));
            if (participant != null && participant.SpeechDetected)
            {
                estaHablando = true;
                break;
            }
        }
        nameTag.color = estaHablando ? Color.green : Color.white;
    }

    private void LateUpdate()
    {
        // Si no es nuestro jugador, hacemos que su tag mire SIEMPRE a nuestra cámara
        if (!isLocalPlayer && nameTag != null)
        {
            if (mainCameraTransform == null && Camera.main != null) 
                mainCameraTransform = Camera.main.transform;

            if (mainCameraTransform != null)
            {
                // El tag mira a la cámara pero se mantiene verticalmente derecho
                Vector3 targetPostion = new Vector3(mainCameraTransform.position.x, 
                                                    nameTag.transform.parent.position.y, 
                                                    mainCameraTransform.position.z);
                
                nameTag.transform.parent.LookAt(targetPostion);
                // Invertimos 180 grados porque los Canvas suelen mirar hacia atrás por defecto
                nameTag.transform.parent.Rotate(0, 180, 0);
            }
        }

        // Lógica de Vivox para el jugador local
        if (isLocalPlayer && VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn && Time.time >= nextSpatialUpdate)
        {
            nextSpatialUpdate = Time.time + spatialUpdateRate;
            if (VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
            {
                VivoxService.Instance.Set3DPosition(transform.position, transform.position, transform.forward, transform.up, channelName, true);
            }
        }
    }

    async Task IniciarVoz(string displayName)
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                if (_estaInicializandoServicios) return;
                _estaInicializandoServicios = true;
                await UnityServices.InitializeAsync();
                _estaInicializandoServicios = false;
            }

            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();

            if (!VivoxService.Instance.IsLoggedIn)
            {
                if (_estaLogueandoVivox) return;
                _estaLogueandoVivox = true;
                var options = new LoginOptions { DisplayName = displayName.Replace(" ", "_"), PlayerId = AuthenticationService.Instance.PlayerId };
                await VivoxService.Instance.LoginAsync(options);
                _estaLogueandoVivox = false;
            }

            if (VivoxService.Instance.IsLoggedIn && !VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
            {
                var properties = new Channel3DProperties(distanciaMaxima, 1, 1.0f, AudioFadeModel.InverseByDistance);
                await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, properties);
            }
        }
        catch (Exception e)
        {
            _estaInicializandoServicios = false;
            _estaLogueandoVivox = false;
            Debug.LogError($"Error Vivox: {e.Message}");
        }
    }

    private async void OnDestroy()
    {
        if (isLocalPlayer && VivoxService.Instance != null)
        {
            try { if (VivoxService.Instance.IsLoggedIn) await VivoxService.Instance.LogoutAsync(); }
            catch { }
        }
    }
}