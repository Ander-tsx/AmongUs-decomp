using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Clear Asteroids: shoot the incoming asteroids with the reticle.</summary>
public class WeaponsMinigame : Minigame
{
    public FloatRange XSpan;
    public FloatRange YSpan;
    public FloatRange TimeToSpawn;
    public AudioClip ShootSound;
    public AudioClip[] ExplodeSounds;

    const int Goal = 20;
    Transform _reticle;
    TextMesh _score;
    int _hits;
    float _spawnIn;
    bool _done;

    class Rock { public Transform T; public Vector2 Vel; public float Spin; public SpriteRenderer Sr; public Asteroid A; public int Img; public bool Dead; }
    readonly List<Rock> _rocks = new List<Rock>();

    protected override void OnBegin()
    {
        _reticle = Find("TargetReticle");
        _score = MakeText(transform, "Asteroids Destroyed: 0", 0.28f, Color.white);
        _score.transform.localPosition = new Vector3(0f, -2.5f, -4f);
        _spawnIn = 0.5f;
        if (Bot) { TimeToSpawn.min = TimeToSpawn.max = 0.05f; }
    }

    protected override void Tick()
    {
        if (_done) return;
        var m = Mouse;
        if (_reticle != null)
        {
            var lp = transform.InverseTransformPoint(new Vector3(m.x, m.y, 0));
            _reticle.localPosition = new Vector3(Mathf.Clamp(lp.x, XSpan.min, XSpan.max), Mathf.Clamp(lp.y, YSpan.min, YSpan.max), _reticle.localPosition.z);
        }
        _spawnIn -= Time.deltaTime;
        if (_spawnIn <= 0f) { Spawn(); _spawnIn = Random.Range(TimeToSpawn.min, TimeToSpawn.max); }

        for (int i = _rocks.Count - 1; i >= 0; i--)
        {
            var r = _rocks[i];
            if (r.Dead) continue;
            r.T.localPosition += (Vector3)(r.Vel * Time.deltaTime);
            r.T.Rotate(0, 0, r.Spin * Time.deltaTime);
            var p = r.T.localPosition;
            if (Mathf.Abs(p.x) > 3.4f || Mathf.Abs(p.y) > 3.1f) { Destroy(r.T.gameObject); _rocks.RemoveAt(i); }
        }
        if (MouseDown && !Bot)
        {
            Sfx(ShootSound);
            foreach (var c in AmongUsLan.UiKit.PointAll(m))
            {
                var r = _rocks.Find(x => !x.Dead && x.T == c.transform);
                if (r != null) { Explode(r); break; }
            }
        }
        if (Bot) foreach (var r in _rocks.ToArray()) if (!r.Dead && Mathf.Abs(r.T.localPosition.x) < 2.2f) { Explode(r); break; }
    }

    void Spawn()
    {
        var prefab = PrefabCatalog.Get("Asteroid1");
        if (prefab == null) return;
        var go = Instantiate(prefab, transform);
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = AmongUsLan.UiKit.Layer;
        foreach (var rr in go.GetComponentsInChildren<Renderer>(true)) rr.sortingOrder += 110;
        var a = go.GetComponent<Asteroid>();
        var sr = go.GetComponent<SpriteRenderer>();
        var expl = go.transform.Find("weapons_explosion");
        if (expl != null) expl.gameObject.SetActive(false);
        var rock = new Rock { T = go.transform, Sr = sr, A = a };
        rock.Img = Random.Range(0, a != null && a.AsteroidImages != null ? a.AsteroidImages.Length : 1);
        if (a != null && a.AsteroidImages != null && a.AsteroidImages.Length > 0) sr.sprite = a.AsteroidImages[rock.Img];
        // enter from the right, aim across the panel
        float y = Random.Range(YSpan.min, YSpan.max);
        go.transform.localPosition = new Vector3(3.2f, y, -3f);
        float speed = a != null ? Random.Range(a.MoveSpeed.min, a.MoveSpeed.max) : 2.5f;
        var dir = (new Vector2(-2.5f, Random.Range(YSpan.min, YSpan.max)) - new Vector2(3.2f, y)).normalized;
        rock.Vel = dir * speed;
        rock.Spin = a != null ? Random.Range(a.RotateSpeed.min, a.RotateSpeed.max) : 40f;
        _rocks.Add(rock);
    }

    void Explode(Rock r)
    {
        r.Dead = true;
        Sfx(ExplodeSounds);
        if (r.A != null && r.A.BrokenImages != null && r.Img < r.A.BrokenImages.Length) r.Sr.sprite = r.A.BrokenImages[r.Img];
        var expl = r.T.Find("weapons_explosion");
        if (expl != null) expl.gameObject.SetActive(true);
        StartCoroutine(Remove(r));
        _hits++;
        _score.text = "Asteroids Destroyed: " + _hits;
        if (_hits >= Goal) { _done = true; Win(0.6f); }
    }

    IEnumerator Remove(Rock r) { yield return new WaitForSeconds(0.25f); if (r.T != null) Destroy(r.T.gameObject); _rocks.Remove(r); }

    public override bool HasTest => true;
    public override IEnumerator TestPlay()
    {
        float until = Time.time + 60f;
        while (!_done && Time.time < until)
        {
            Rock target = null;
            foreach (var r in _rocks) if (!r.Dead && r.T != null && Mathf.Abs(r.T.localPosition.x) < 2.4f && Mathf.Abs(r.T.localPosition.y) < 2.2f) { target = r; break; }
            if (target != null) yield return StartCoroutine(TClick(Center(target.T)));
            else yield return null;
        }
    }
}
