using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Halloween theme: tinted ship, carved pumpkins, cobwebs, tombstones, spiders, drifting bats and ghosts, spooky audio.</summary>
    public partial class Game
    {
        static readonly Color HalloweenTint = new Color(0.8f, 0.68f, 0.95f, 1f);
        static readonly Color HalloweenSky = new Color(0.07f, 0.02f, 0.12f, 1f);

        class Flicker { public SpriteRenderer Glow; public float Phase, Base; }
        class Swinger { public Transform Pivot; public float Phase, Amp; }
        class Flyer { public Transform T; public SpriteRenderer R; public Vector2 Dir; public float Speed, Age, Life, Wobble, Phase, Alpha; public bool Bat; public Vector2 Base; }

        Transform _halloween;
        readonly List<Flicker> _flickers = new List<Flicker>();
        readonly List<Swinger> _swingers = new List<Swinger>();
        readonly List<Flyer> _flyers = new List<Flyer>();
        float _batTimer = 3f, _ghostTimer = 6f, _chimeTimer = 14f;
        AudioSource _spook;
        AudioClip _droneClip, _chimeClip;
        SpriteRenderer _vignette;
        bool _halloweenOn = true;

        void InitHalloween()
        {
            if (!_halloweenOn) return;
            _halloween = new GameObject("Halloween").transform;
            var rng = new System.Random(31337);

            TintTree(GameObject.Find("SkeldShip"));
            TintTree(_lobbyGo);

            // ship rooms
            if (WorldMap.Rooms != null)
                foreach (var room in WorldMap.Rooms)
                {
                    if (room == null || room.roomArea == null) continue;
                    DecorateRoom(room, rng);
                }

            // dropship lobby
            if (_lobby != null && _lobby.SpawnPositions != null && _lobby.SpawnPositions.Length > 0)
            {
                var pts = _lobby.SpawnPositions.Select(p => LobbyOffset + p - new Vector2(0, 1.6f)).ToArray();
                float minX = pts.Min(p => p.x), maxX = pts.Max(p => p.x), minY = pts.Min(p => p.y), maxY = pts.Max(p => p.y);
                Pumpkin(new Vector2(minX - 1.3f, minY - 0.1f), 0.55f);
                Pumpkin(new Vector2(maxX + 1.3f, minY - 0.1f), 0.55f);
                Pumpkin(new Vector2(maxX + 1.0f, maxY + 0.9f), 0.4f);
                Candle(new Vector2(minX - 0.8f, minY - 0.05f));
                Candle(new Vector2(maxX + 0.8f, minY - 0.05f));
                Candle(new Vector2((minX + maxX) / 2f + 0.4f, maxY + 1.3f));
                Web(new Vector2(minX - 2.0f, maxY + 2.2f), false, 1.6f);
                Web(new Vector2(maxX + 2.0f, maxY + 2.2f), true, 1.6f);
                Spider(new Vector2(minX + 1.2f, maxY + 2.4f), 0.9f, 1.5f);
            }

            // audio
            _spook = gameObject.AddComponent<AudioSource>();
            _spook.loop = true; _spook.volume = 0.08f; _spook.playOnAwake = false; _spook.spatialBlend = 0f;
            _droneClip = MakeDrone(); _chimeClip = MakeChime();
            _spook.clip = _droneClip; _spook.Play();

            if (_cam != null) _cam.backgroundColor = HalloweenSky;
        }

        void TintTree(GameObject root)
        {
            if (root == null) return;
            foreach (var r in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var c = r.color;
                r.color = new Color(c.r * HalloweenTint.r, c.g * HalloweenTint.g, c.b * HalloweenTint.b, c.a);
            }
        }

        // ------------------------------------------------------------ props

        SpriteRenderer Prop(Sprite s, Vector2 pos, int order, float scale = 1f)
        {
            var g = new GameObject(s.name);
            g.transform.SetParent(_halloween, false);
            g.transform.position = new Vector3(pos.x, pos.y, 0);
            g.transform.localScale = new Vector3(scale, scale, 1);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.sortingOrder = order;
            return sr;
        }

        static int FloorOrder(Vector2 p) => 100 - Mathf.RoundToInt(p.y * 10f);

        void Glow(Vector2 pos, float size, float baseAlpha, int order)
        {
            var g = Prop(HalloweenArt.Glow, pos, order, size);
            g.color = new Color(1f, 1f, 1f, baseAlpha);
            _flickers.Add(new Flicker { Glow = g, Phase = Random.value * 20f, Base = baseAlpha });
        }

        void Pumpkin(Vector2 pos, float scale)
        {
            Glow(pos + new Vector2(0, 0.05f), scale * 3.4f, 0.8f, FloorOrder(pos) - 2);
            Prop(HalloweenArt.Pumpkin, pos, FloorOrder(pos) - 1, scale);
        }

        void Candle(Vector2 pos)
        {
            Glow(pos + new Vector2(0, 0.25f), 1.2f, 0.9f, FloorOrder(pos) - 2);
            Prop(HalloweenArt.Candle, pos, FloorOrder(pos) - 1, 0.6f);
        }

        void Web(Vector2 corner, bool flipX, float units)
        {
            // the sprite's web is anchored at its top-left texel; the sprite is 2 world units wide at scale 1
            float k = units / 2f;
            var w = Prop(HalloweenArt.Web, corner + new Vector2((flipX ? -1 : 1) * units / 2f, -units / 2f), 40, k);
            w.transform.localScale = new Vector3((flipX ? -1 : 1) * k, k, 1);
            w.color = new Color(1f, 1f, 1f, 0.8f);
        }

        void Spider(Vector2 anchor, float length, float scale)
        {
            var pivot = new GameObject("SpiderPivot").transform;
            pivot.SetParent(_halloween, false);
            pivot.position = new Vector3(anchor.x, anchor.y, 0);
            var thread = new GameObject("Thread").AddComponent<SpriteRenderer>();
            thread.transform.SetParent(pivot, false);
            thread.sprite = UiKit.WhiteSprite();
            thread.transform.localPosition = new Vector3(0, -length / 2f, 0);
            thread.transform.localScale = new Vector3(0.012f, length, 1);
            thread.color = new Color(0.9f, 0.9f, 1f, 0.55f); thread.sortingOrder = 41;
            var sp = new GameObject("Spider").AddComponent<SpriteRenderer>();
            sp.transform.SetParent(pivot, false);
            sp.sprite = HalloweenArt.Spider; sp.sortingOrder = 42;
            sp.transform.localPosition = new Vector3(0, -length, 0);
            sp.transform.localScale = Vector3.one * 0.45f * scale;
            _swingers.Add(new Swinger { Pivot = pivot, Phase = Random.value * 6f, Amp = 7f });
        }

        void DecorateRoom(PlainShipRoom room, System.Random rng)
        {
            var area = room.roomArea;
            var b = area.bounds;
            int h = (int)room.RoomId;
            float inset = 0.5f;
            // find floor spots near the four corners
            Vector2? Corner(float fx, float fy)
            {
                var c = new Vector2(Mathf.Lerp(b.min.x, b.max.x, fx), Mathf.Lerp(b.min.y, b.max.y, fy));
                var centre = (Vector2)b.center;
                for (int i = 0; i <= 14; i++)
                {
                    var p = Vector2.Lerp(c, centre, i / 14f);
                    if (area.OverlapPoint(p) && area.OverlapPoint(p + (centre - p).normalized * inset) && Physics2D.OverlapCircle(p, 0.3f) == null) return p;
                }
                return null;
            }
            var tl = Corner(0.02f, 0.98f); var tr = Corner(0.98f, 0.98f); var bl = Corner(0.02f, 0.02f); var br = Corner(0.98f, 0.02f);
            if (tl.HasValue) Web(tl.Value + new Vector2(-0.3f, 0.5f), false, 1.2f);
            if (tr.HasValue) Web(tr.Value + new Vector2(0.3f, 0.5f), true, 1.2f);
            if (bl.HasValue && (h & 1) == 0) Pumpkin(bl.Value, 0.5f);
            if (br.HasValue && (h & 1) == 1) Pumpkin(br.Value, 0.5f);
            if (bl.HasValue && (h % 3) == 0) Candle(bl.Value + new Vector2(0.55f, 0f));
            if (br.HasValue && (h % 3) == 1) { Prop(HalloweenArt.Tomb, br.Value, FloorOrder(br.Value) - 1, 0.55f); }
            if (tl.HasValue && (h % 4) == 2) Prop(HalloweenArt.Skull, tl.Value + new Vector2(0.6f, -0.15f), FloorOrder(tl.Value) - 1, 0.45f);
            if (tr.HasValue && (h % 2) == 0) Spider(tr.Value + new Vector2(-0.8f, 0.75f), 0.6f + (float)rng.NextDouble() * 0.5f, 1f);
            if (br.HasValue && (h % 5) == 3) Prop(HalloweenArt.Cat, br.Value + new Vector2(-0.6f, 0f), FloorOrder(br.Value) - 1, 0.45f);
        }

        // ------------------------------------------------------------ flyers

        void Fly(bool bat)
        {
            var centre = _me != null && _phase == Phase.Playing ? _me.Pos : (Vector2)_cam.transform.position;
            float ang = Random.value * Mathf.PI * 2f;
            var from = centre + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (bat ? 8f : 6f);
            var to = centre + Random.insideUnitCircle * 3f;
            var dir = (to - from).normalized;
            var g = new GameObject(bat ? "Bat" : "Ghost");
            g.transform.SetParent(_halloween, false);
            g.transform.position = from;
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = bat ? HalloweenArt.Bat(0) : HalloweenArt.Ghost;
            sr.sortingOrder = bat ? 700 : 600;
            g.transform.localScale = Vector3.one * (bat ? 0.5f : 0.6f);
            _flyers.Add(new Flyer { T = g.transform, R = sr, Dir = dir, Speed = bat ? Random.Range(2.6f, 3.8f) : Random.Range(0.6f, 1.0f), Life = bat ? 6f : 12f, Wobble = Random.Range(0.4f, 0.9f), Phase = Random.value * 6f, Bat = bat, Base = from, Alpha = bat ? 0.95f : 0.5f });
        }

        void UpdateHalloween()
        {
            if (!_halloweenOn || _halloween == null) return;
            float t = Time.time;

            foreach (var f in _flickers)
            {
                if (f.Glow == null) continue;
                float n = Mathf.PerlinNoise(f.Phase, t * 3.2f) * 0.5f + Mathf.PerlinNoise(f.Phase + 9f, t * 9f) * 0.5f;
                var c = f.Glow.color; c.a = f.Base * (0.55f + 0.7f * n); f.Glow.color = c;
            }
            foreach (var s in _swingers)
                if (s.Pivot != null) s.Pivot.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.3f + s.Phase) * s.Amp);

            // spawn bats / ghosts near whoever is looking
            bool playing = _phase == Phase.Playing || _phase == Phase.Lobby;
            if (playing)
            {
                _batTimer -= Time.deltaTime; _ghostTimer -= Time.deltaTime;
                if (_batTimer <= 0f) { _batTimer = Random.Range(4f, 9f); int n = Random.Range(1, 4); for (int i = 0; i < n; i++) Fly(true); }
                if (_ghostTimer <= 0f) { _ghostTimer = Random.Range(14f, 24f); if (_flyers.Count(f => !f.Bat) < 2) Fly(false); }
            }
            for (int i = _flyers.Count - 1; i >= 0; i--)
            {
                var f = _flyers[i];
                f.Age += Time.deltaTime;
                if (f.T == null || f.Age > f.Life) { if (f.T != null) Destroy(f.T.gameObject); _flyers.RemoveAt(i); continue; }
                var perp = new Vector2(-f.Dir.y, f.Dir.x);
                f.Base += f.Dir * f.Speed * Time.deltaTime;
                float wob = Mathf.Sin(f.Age * (f.Bat ? 5f : 1.5f) + f.Phase) * f.Wobble;
                f.T.position = new Vector3(f.Base.x + perp.x * wob, f.Base.y + perp.y * wob + (f.Bat ? 0f : Mathf.Sin(f.Age * 2f) * 0.1f), 0);
                if (f.Bat)
                {
                    f.R.sprite = HalloweenArt.Bat(((int)(f.Age * 9f)) & 1);
                    f.R.flipX = f.Dir.x < 0;
                }
                else f.R.flipX = f.Dir.x < 0;
                float fade = Mathf.Min(1f, f.Age / 0.8f) * Mathf.Min(1f, (f.Life - f.Age) / 0.8f);
                var c = f.R.color; c.a = f.Alpha * fade; f.R.color = c;
            }

            // eerie chime
            _chimeTimer -= Time.deltaTime;
            if (_chimeTimer <= 0f && _spook != null && playing)
            {
                _chimeTimer = Random.Range(16f, 32f);
                _spook.PlayOneShot(_chimeClip, 1.2f);
            }
            if (_spook != null) _spook.volume = _phase == Phase.Meeting ? 0.03f : 0.08f;

            // purple vignette over the HUD
            if (_hud != null && _vignette == null)
            {
                _vignette = _ui.Rect(_hud, Vector2.zero, new Vector2(_ui.HalfW * 2 + 2, _ui.HalfH * 2 + 2), new Color(0.35f, 0.05f, 0.5f, 0.09f), 0);
                _vignette.gameObject.name = "HalloweenTint";
            }
        }

        // ------------------------------------------------------------ audio

        static AudioClip MakeDrone()
        {
            const int rate = 22050, secs = 6;
            var data = new float[rate * secs];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float lfo = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * t / secs);
                data[i] = (Mathf.Sin(2f * Mathf.PI * 55f * t) + Mathf.Sin(2f * Mathf.PI * 55.5f * t) * 0.8f + 0.35f * Mathf.Sin(2f * Mathf.PI * 82.5f * t) * lfo
                           + 0.2f * Mathf.Sin(2f * Mathf.PI * 110.5f * t) * (1f - lfo)) * 0.3f;
            }
            var c = AudioClip.Create("halloween_drone", data.Length, 1, rate, false);
            c.SetData(data, 0);
            return c;
        }

        static AudioClip MakeChime()
        {
            const int rate = 22050;
            var data = new float[(int)(rate * 2.2f)];
            float[] notes = { 523.25f, 622.25f, 466.16f };
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate, v = 0;
                for (int n = 0; n < notes.Length; n++)
                {
                    float st = n * 0.45f; if (t < st) continue;
                    float tt = t - st;
                    v += Mathf.Sin(2f * Mathf.PI * notes[n] * tt) * Mathf.Exp(-tt * 3.2f) * 0.25f + Mathf.Sin(2f * Mathf.PI * notes[n] * 2.01f * tt) * Mathf.Exp(-tt * 6f) * 0.08f;
                }
                data[i] = v;
            }
            var c = AudioClip.Create("halloween_chime", data.Length, 1, rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
