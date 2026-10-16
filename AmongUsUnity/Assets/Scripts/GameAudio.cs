using System.Collections.Generic;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Room ambience (volume follows the distance to each room), ship hum and per-room footsteps.</summary>
    public partial class Game
    {
        class Amb { public SkeldShipRoom Room; public AudioSource Src; }
        readonly List<Amb> _ambs = new List<Amb>();
        AudioSource _shipHum;
        AmbientSoundPlayer _hum;
        PlainShipRoom[] _roomObjs = new PlainShipRoom[0];

        void InitAmbience()
        {
            _roomObjs = FindObjectsByType<PlainShipRoom>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var r in FindObjectsByType<SkeldShipRoom>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r.AmbientSound == null) continue;
                var go = new GameObject("Ambience " + r.name);
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.clip = r.AmbientSound; src.loop = true; src.volume = 0f; src.spatialBlend = 0f; src.playOnAwake = false;
                src.Play();
                _ambs.Add(new Amb { Room = r, Src = src });
            }
            _hum = FindFirstObjectByType<AmbientSoundPlayer>();
            if (_hum != null && _hum.AmbientSound != null)
            {
                var go = new GameObject("Ship hum");
                go.transform.SetParent(transform, false);
                _shipHum = go.AddComponent<AudioSource>();
                _shipHum.clip = _hum.AmbientSound; _shipHum.loop = true; _shipHum.volume = 0f; _shipHum.spatialBlend = 0f;
                _shipHum.Play();
            }
        }

        void UpdateAmbience()
        {
            bool on = _me != null && (_phase == Phase.Playing || _phase == Phase.Lobby);
            bool ship = _phase == Phase.Playing && !IntroActive;
            foreach (var a in _ambs)
            {
                float v = 0f;
                if (on && ship)
                {
                    var r = a.Room;
                    float max = Mathf.Max(r.AmbientMaxDist, 1f);
                    float d = r.roomArea != null ? Vector2.Distance(r.roomArea.ClosestPoint(_me.Pos), _me.Pos) : Vector2.Distance(r.transform.position, _me.Pos);
                    v = Mathf.Clamp01(1f - d / max) * Mathf.Max(r.AmbientVolume, 0.3f) * 0.8f;
                }
                a.Src.volume = Mathf.MoveTowards(a.Src.volume, v, Time.deltaTime * 2f);
            }
            if (_shipHum != null) _shipHum.volume = Mathf.MoveTowards(_shipHum.volume, on && ship ? Mathf.Clamp(_hum.MaxVolume, 0.1f, 1f) * 0.5f : 0f, Time.deltaTime);
        }

        AudioClip FootstepAt(Vector2 pos)
        {
            foreach (var r in _roomObjs)
            {
                if (r.roomArea == null || !r.roomArea.OverlapPoint(pos)) continue;
                var sk = r as SkeldShipRoom;
                if (sk != null && sk.FootStepSounds != null) { var c = sk.FootStepSounds.Random(); if (c != null) return c; }
            }
            return null;
        }
    }
}
