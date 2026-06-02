using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections.Generic;

public class LobbyUI : MonoBehaviour
{
    private string myName = "Jugador";
    private bool isNameSet = false;
    private float winnerMessageTimer = -1f; // -1 = no iniciado
    private bool isWinner = false;

    void OnGUI()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
        if (CloudSpawner.Instance == null || !CloudSpawner.Instance.IsSpawned) return;

        float scale = Mathf.Min(Screen.width / 600f, Screen.height / 800f);
        if (scale < 1f) scale = 1f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        float sw = Screen.width / scale;
        float sh = Screen.height / scale;

        switch (CloudSpawner.Instance.currentState.Value)
        {
            case CloudSpawner.GameState.Lobby:     DrawLobby(sw, sh);    break;
            case CloudSpawner.GameState.Countdown: DrawCountdown(sw, sh); break;
            case CloudSpawner.GameState.Racing:    DrawRacingHUD(sw, sh); break;
            case CloudSpawner.GameState.Finished:  DrawEndScreen(sw, sh); break;
        }
    }

    void Update()
    {
        if (winnerMessageTimer > 0f)
            winnerMessageTimer -= Time.deltaTime;

        // Detectar cuando el estado cambia a Finished para mostrar el popup al ganador
        if (CloudSpawner.Instance != null && CloudSpawner.Instance.IsSpawned &&
            CloudSpawner.Instance.currentState.Value == CloudSpawner.GameState.Finished &&
            !isWinner && winnerMessageTimer < 0f)
        {
            var lc = NetworkManager.Singleton.LocalClient;
            if (lc?.PlayerObject != null && lc.PlayerObject.TryGetComponent<PlayerController>(out var pc))
            {
                if (pc.score.Value >= 50)
                {
                    isWinner = true;
                    winnerMessageTimer = 3f;
                }
            }
        }
    }

    // ─── LOBBY ───────────────────────────────────────────────────────────────
    void DrawLobby(float sw, float sh)
    {
        GUILayout.BeginArea(new Rect(sw * 0.1f, sh * 0.05f, sw * 0.8f, sh * 0.9f), GUI.skin.box);
        GUILayout.Space(10);
        GUILayout.Label("SALA DE ESPERA", new GUIStyle(GUI.skin.label) { fontSize = 30, alignment = TextAnchor.MiddleCenter });
        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Tu Nombre:", GUILayout.Width(100));
        myName = GUILayout.TextField(myName, 15, GUILayout.Height(30));
        if (GUILayout.Button("Aplicar", GUILayout.Width(80), GUILayout.Height(30))) UpdateMyName();
        GUILayout.EndHorizontal();

        if (!isNameSet)
        {
            var lc = NetworkManager.Singleton.LocalClient;
            if (lc?.PlayerObject != null)
            {
                myName = "Jugador" + NetworkManager.Singleton.LocalClientId;
                UpdateMyName();
                isNameSet = true;
            }
        }

        GUILayout.Space(20);
        GUILayout.Label("Jugadores:", new GUIStyle(GUI.skin.label) { fontSize = 20 });
        foreach (var pc in GetAllPlayerControllers())
        {
            string tag = pc.OwnerClientId == NetworkManager.ServerClientId ? " [Anfitrión]" : "";
            GUILayout.Label("  • " + pc.playerName.Value + tag);
        }

        GUILayout.FlexibleSpace();
        if (NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("¡INICIAR CARRERA!", GUILayout.Height(70)))
                CloudSpawner.Instance.RequestStartGame();
        }
        else
        {
            GUILayout.Label("Esperando al anfitrión...",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18 });
            GUILayout.Space(10);
        }

        if (GUILayout.Button("Desconectar", GUILayout.Height(45)))
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        GUILayout.EndArea();
    }

    // ─── COUNTDOWN ───────────────────────────────────────────────────────────
    void DrawCountdown(float sw, float sh)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label)
        {
            fontSize = 160,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.yellow }
        };
        GUI.Label(new Rect(0, 0, sw, sh),
            Mathf.CeilToInt(CloudSpawner.Instance.countdownTimer.Value).ToString(), s);
    }

    // ─── IN-RACE HUD (tabla izquierda) ───────────────────────────────────────
    void DrawRacingHUD(float sw, float sh)
    {
        // Ordenar jugadores por score
        var clients = new List<(string name, int score)>();
        foreach (var pc in GetAllPlayerControllers())
        {
            clients.Add((pc.playerName.Value.ToString(), pc.score.Value));
        }
        clients.Sort((a, b) => b.score.CompareTo(a.score));

        float panelW = 210f;
        float lineH = 30f;
        float panelH = lineH * (clients.Count + 1) + 10f;

        GUI.Box(new Rect(10, 10, panelW, panelH), "");
        GUI.Label(new Rect(15, 12, panelW - 10, 22),
            "TOP  (meta: 50 ☁)",
            new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold });

        for (int i = 0; i < clients.Count; i++)
        {
            float y = 12 + lineH * (i + 1);
            float barW = (panelW - 20) * (clients[i].score / 50f);
            GUI.DrawTexture(new Rect(15, y + 18, barW, 6), Texture2D.whiteTexture);
            GUI.Label(new Rect(15, y, panelW - 20, 22),
                $"{i + 1}. {clients[i].name}  {clients[i].score}/50",
                new GUIStyle(GUI.skin.label) { fontSize = 14 });
        }

        // Popup GANASTE durante 3 s
        if (isWinner && winnerMessageTimer > 0f)
        {
            GUIStyle ws = new GUIStyle(GUI.skin.label)
            {
                fontSize = 110,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.green }
            };
            GUI.Label(new Rect(0, 0, sw, sh), "¡GANASTE!", ws);
        }
    }

    // ─── END SCREEN ──────────────────────────────────────────────────────────
    void DrawEndScreen(float sw, float sh)
    {
        // Mientras el popup esté activo, mostrarlo sobre todo
        if (isWinner && winnerMessageTimer > 0f)
        {
            GUIStyle ws = new GUIStyle(GUI.skin.label)
            {
                fontSize = 110,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.green }
            };
            GUI.Label(new Rect(0, 0, sw, sh), "¡GANASTE!", ws);
            return;
        }

        // Resultados finales
        string winner = CloudSpawner.Instance.winnerName.Value.ToString();
        var clients = new List<(string name, int score)>();
        foreach (var pc in GetAllPlayerControllers())
        {
            clients.Add((pc.playerName.Value.ToString(), pc.score.Value));
        }
        clients.Sort((a, b) => b.score.CompareTo(a.score));

        GUILayout.BeginArea(new Rect(sw * 0.1f, sh * 0.05f, sw * 0.8f, sh * 0.9f), GUI.skin.box);
        GUILayout.Space(10);
        GUILayout.Label($"🏆  GANADOR: {winner}",
            new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter });
        GUILayout.Space(20);
        GUILayout.Label("── RESULTADOS FINALES ──",
            new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter });
        GUILayout.Space(10);

        for (int i = 0; i < clients.Count; i++)
        {
            string medal = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : $"{i + 1}.";
            GUILayout.Label($"  {medal}  {clients[i].name}  —  {clients[i].score} nubes",
                new GUIStyle(GUI.skin.label) { fontSize = 20 });
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Volver al Menú Principal", GUILayout.Height(60)))
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        GUILayout.EndArea();
    }

    void UpdateMyName()
    {
        var lc = NetworkManager.Singleton.LocalClient;
        if (lc?.PlayerObject != null && lc.PlayerObject.TryGetComponent<PlayerController>(out var pc))
            pc.SetPlayerNameServerRpc(myName);
    }

    private IEnumerable<PlayerController> GetAllPlayerControllers()
    {
        if (NetworkManager.Singleton == null)
            yield break;

        // Cliente y servidor pueden acceder a objetos spawnados localmente
        foreach (var spawned in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            if (spawned.Value == null) continue;
            if (spawned.Value.TryGetComponent<PlayerController>(out var pc))
                yield return pc;
        }
    }
}

