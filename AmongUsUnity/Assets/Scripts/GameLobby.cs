using System.Linq;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Dropship lobby: spawn points, music, colour wardrobe and the host's start button.</summary>
    public partial class Game
    {
        GameObject _lobbyGo;
        LobbyBehaviour _lobby;
        static readonly Vector2 LobbyOffset = new Vector2(120f, 0f);
        AudioSource _music;
        string _musicName = "";

        void BuildLobby()
        {
            var prefab = PrefabCatalog.Get("Lobby");
            if (prefab == null) return;
            _lobbyGo = Instantiate(prefab, new Vector3(LobbyOffset.x, LobbyOffset.y, 0), Quaternion.identity);
            _lobbyGo.name = "Lobby";
            _lobby = _lobbyGo.GetComponent<LobbyBehaviour>();
            // keep the lobby in front of the ship in the sorting order
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true; _music.volume = 0.35f; _music.playOnAwake = false;
        }

        Vector2 LobbySpawn(int index)
        {
            var sp = _lobby != null && _lobby.SpawnPositions != null && _lobby.SpawnPositions.Length > 0 ? _lobby.SpawnPositions : new[] { new Vector2(0, 2.5f) };
            return LobbyOffset + sp[((index % sp.Length) + sp.Length) % sp.Length] - new Vector2(0, 1.6f);
        }

        void ResetLobbyPositions()
        {
            int i = 0;
            foreach (var p in _players.Values.OrderBy(x => x.Id))
            {
                p.Pos = p.TargetPos = LobbySpawn(i++);
                if (p.Go != null) p.Go.transform.position = p.Pos;
                if (p.Body != null) p.Body.position = p.Pos;
            }
        }

        void PlayMusic(string clip)
        {
            if (_music == null) return;
            if (_musicName == clip) return;
            _musicName = clip;
            if (string.IsNullOrEmpty(clip)) { _music.Stop(); return; }
            if (_clips.TryGetValue(clip, out var c)) { _music.clip = c; _music.Play(); }
        }

        void UpdateLobbyAudio()
        {
            PlayMusic(_phase == Phase.Lobby ? "Maiden_Voyage__Theme_of_Skeld" : "");
        }

        // ---- wardrobe (colour picker) ----
        Transform _wardrobe;

        Vector2 WardrobePos => LobbyOffset + new Vector2(1.03f, 2.17f);

        bool NearWardrobe => _phase == Phase.Lobby && _me != null && Vector2.Distance(_me.Pos, WardrobePos) < 1.4f;

        void ToggleWardrobe()
        {
            if (_wardrobe != null) { Destroy(_wardrobe.gameObject); _wardrobe = null; return; }
            if (_ui == null) _ui = UiKit.I;
            _wardrobe = _ui.Group("Wardrobe");
            _ui.Rect(_wardrobe, Vector2.zero, new Vector2(5.4f, 3.6f), new Color(0.06f, 0.08f, 0.14f, 0.95f), 150);
            _ui.Text(Loc.T("Choose your colour"), _wardrobe, new Vector2(0, 1.45f), 0.34f, Color.white, TextAnchor.MiddleCenter, 152, true);
            var used = _players.Values.Where(p => p != _me).Select(p => p.Color).ToHashSet();
            for (int i = 0; i < Palette.Colors.Length; i++)
            {
                int ci = i;
                var pos = new Vector2(-2.2f + (i % 6) * 0.88f, 0.55f - (i / 6) * 1.25f);
                var icon = _ui.PlayerIcon(_wardrobe, pos, 0.45f, i, 153);
                if (used.Contains(i)) { var c = icon.color; c.a = 0.25f; icon.color = c; }
                else _ui.Button(icon, () => { Cmd("COLOR|" + ci); ToggleWardrobe(); }, new Vector2(0.7f, 0.9f));
            }
            var close = _ui.Sliced("buttonClick", _wardrobe, new Vector2(0, -1.55f), new Vector2(1.6f, 0.45f), 153);
            _ui.Button(close, ToggleWardrobe, new Vector2(1.6f, 0.45f));
            _ui.Text(Loc.T("Close"), _wardrobe, new Vector2(0, -1.55f), 0.22f, Color.white, TextAnchor.MiddleCenter, 155, true);
        }

        void HostColor(int from, int color)
        {
            if (_phase != Phase.Lobby) return;
            var p = Get(from);
            if (p == null || color < 0 || color >= Palette.Colors.Length) return;
            if (_players.Values.Any(x => x != p && x.Color == color)) return;
            p.Color = color;
            BroadcastLobby();
        }

        // ---- lobby HUD: use wardrobe, host start button, room info ----
        Transform _lobbyHud;
        TextMesh _lobbyInfo;
        UiButton _startUi;
        TextMesh _startText;

        void BuildLobbyHud()
        {
            float w = _ui.HalfW, h = _ui.HalfH;
            _lobbyHud = _ui.Group("LobbyHUD");
            _lobbyInfo = _ui.Text("", _lobbyHud, new Vector2(0, h - 0.45f), 0.3f, Color.white, TextAnchor.MiddleCenter, 5, true);
            var start = _ui.Sliced("buttonClick", _lobbyHud, new Vector2(0, -h + 0.55f), new Vector2(2.6f, 0.65f), 5);
            _startUi = _ui.Button(start, HostStartButton, new Vector2(2.6f, 0.65f));
            _startText = _ui.Text(Loc.T("START"), _lobbyHud, new Vector2(0, -h + 0.55f), 0.34f, Color.white, TextAnchor.MiddleCenter, 7, true);
            var use = _ui.Sprite("use", _lobbyHud, new Vector2(w - 0.75f, -h + 0.75f), 5, 0.9f);
            _ui.Button(use, () => { if (NearWardrobe) ToggleWardrobe(); else if (NearSettingsPanel) ToggleSettings(); }, new Vector2(1.1f, 1.1f));
            _lobbyUse = use;
            _lobbyHud.gameObject.SetActive(false);
        }

        SpriteRenderer _lobbyUse;
        float _startCountdown = -1f;

        void HostStartButton()
        {
            if (!_isHost || _phase != Phase.Lobby || _startCountdown >= 0f) return;
            _startCountdown = 3f;
            Emit("COUNT|3");
        }

        void UpdateLobbyHud()
        {
            bool show = _phase == Phase.Lobby && _me != null && Minigame.Instance == null;
            if (_lobbyHud.gameObject.activeSelf != show) _lobbyHud.gameObject.SetActive(show);
            if (!show) { if (_wardrobe != null) { Destroy(_wardrobe.gameObject); _wardrobe = null; } if (_settingsUi != null) ToggleSettings(); return; }
            float w = _ui.HalfW, h = _ui.HalfH;
            _lobbyUse.transform.localPosition = new Vector2(w - 0.75f, -h + 0.75f);
            _lobbyUse.color = NearWardrobe || NearSettingsPanel ? Color.white : Disabled;
            _startUi.gameObject.SetActive(_isHost);
            _startText.gameObject.SetActive(_isHost);
            if (_isHost)
            {
                _startText.text = _startCountdown >= 0f ? Mathf.CeilToInt(_startCountdown).ToString() : Loc.T(_players.Count >= 2 ? "START" : "START (solo)");
                _startUi.SetTint(_startCountdown >= 0f ? Disabled : Color.white);
            }
            string ip = _isHost ? Loc.T("Your LAN address") + ": " + LocalAddress() : Loc.T("Waiting for the host");
            _lobbyInfo.text = ip + "   -   " + _players.Count + "/" + Palette.Colors.Length + " " + Loc.T("players");
            if (Input.GetKeyDown(KeyCode.E) && NearWardrobe) ToggleWardrobe();
            else if (Input.GetKeyDown(KeyCode.E) && NearSettingsPanel) ToggleSettings();
            if (_isHost && _startCountdown >= 0f)
            {
                _startCountdown -= Time.deltaTime;
                if (_startCountdown <= 0f) { _startCountdown = -1f; HostStartGame(); }
            }
        }

        static string LocalAddress()
        {
            try
            {
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up || ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                        if (ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) return ua.Address.ToString();
                }
            }
            catch { }
            return "127.0.0.1";
        }

        // ---- main menu backdrop ----
        Transform _menuUi;

        void BuildMenuUi()
        {
            _menuUi = _ui.Group("MenuUI");
            _ui.Rect(_menuUi, Vector2.zero, new Vector2(_ui.HalfW * 2 + 2, _ui.HalfH * 2 + 2), new Color(0f, 0f, 0.05f, 0.55f), 0);
            _ui.Sprite("LOGO-AU", _menuUi, new Vector2(0, _ui.HalfH - 0.85f), 5, 0.55f);
            BuildMenuControls();
        }

        void UpdateMenuUi()
        {
            bool show = _phase == Phase.Menu;
            if (_menuUi.gameObject.activeSelf != show) _menuUi.gameObject.SetActive(show);
            if (show) UpdateMenuControls();
        }
    }
}
