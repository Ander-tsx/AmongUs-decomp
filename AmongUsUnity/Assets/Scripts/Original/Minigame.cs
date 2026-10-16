using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmongUsLan;
using UnityEngine;

/// <summary>
/// Base for the original task minigames. The original prefab is instantiated as is (sprites, colliders and
/// serialized tuning values come from the build); subclasses reimplement the behaviour with the same field names.
/// </summary>
public abstract class Minigame : MonoBehaviour
{
    public static Minigame Instance;

    public int TransType;
    public Vector2 TargetPosition;
    public AudioClip OpenSound;
    public AudioClip CloseSound;

    public Action<bool> Finished;
    public int Step;                    // index of the task step this instance serves
    public SystemTypes Room;            // console room (distinguishes e.g. upper/lower engine)
    public string ConsoleName;
    public bool Bot;                    // automated solving for smoke tests
    public SystemTypes Target;          // e.g. destination room of a Divert Power task
    public string Arg;                  // free-form argument (e.g. the O2 keypad code)
    public Action<bool> Hold;           // reactor hand: called every frame with the held state

    bool _closing;
    float _openedAt;
    static AudioSource _sfx;

    public static Minigame Open(string prefabName, Action<bool> finished, int step, SystemTypes room, string consoleName, bool bot, SystemTypes target = SystemTypes.Hallway, string arg = null, Action<bool> hold = null)
    {
        if (Instance != null) return null;
        var prefab = PrefabCatalog.Get(prefabName);
        if (prefab == null) return null;
        var go = UnityEngine.Object.Instantiate(prefab);
        go.name = prefabName;
        var ui = UiKit.I;
        go.transform.SetParent(ui.Cam.transform, false);
        go.transform.localPosition = new Vector3(0, 0, 50);
        go.transform.localRotation = Quaternion.identity;
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = UiKit.Layer;
            if (t.name.Contains("Glyph") || t.name.Contains("SelectorArrow") || t.name.Contains("ControllerHighlight")) t.gameObject.SetActive(false);
        }
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sortingOrder += 100;
        var mg = go.GetComponent<Minigame>();
        if (mg == null) { UnityEngine.Object.Destroy(go); return null; }
        mg.Arg = arg; mg.Hold = hold; mg.Target = target; mg.Finished = finished; mg.Step = step; mg.Room = room; mg.ConsoleName = consoleName; mg.Bot = bot;
        Instance = mg;
        UiKit.Blocked = true;
        mg._openedAt = Time.time;
        mg.Sfx(mg.OpenSound);
        mg.OnBegin();
        return mg;
    }

    protected abstract void OnBegin();

    // ---- helpers ----
    protected Camera Cam => UiKit.I.Cam;
    /// <summary>Scripted pointer for automated UI tests (-uitest); null = real mouse.</summary>
    public static Vector2? FakePos;
    public static bool FakeDown, FakeHeld, FakeUp;

    protected Vector2 Mouse => FakePos ?? (Vector2)Cam.ScreenToWorldPoint(Input.mousePosition);
    protected bool MouseDown => FakePos.HasValue ? FakeDown : Input.GetMouseButtonDown(0);
    protected bool MouseHeld => FakePos.HasValue ? FakeHeld : Input.GetMouseButton(0);
    protected bool MouseUp => FakePos.HasValue ? FakeUp : Input.GetMouseButtonUp(0);

    /// <summary>Topmost (closest to camera) collider under the pointer, ignoring the close/background areas.</summary>
    protected Collider2D HitAt(Vector2 p)
    {
        Collider2D best = null; float bz = float.MaxValue;
        foreach (var c in UiKit.PointAll(p))
        {
            if (!c.transform.IsChildOf(transform)) continue;
            if (c.name.Contains("CloseButton")) continue;
            float z = c.transform.position.z;
            if (z < bz) { bz = z; best = c; }
        }
        return best;
    }

    // ---- scripted pointer for automated tests ----
    public virtual IEnumerator TestPlay() { yield break; }
    public virtual bool HasTest => false;
    public bool TestPassed;

    protected IEnumerator TClick(Vector2 p)
    {
        FakePos = p; FakeDown = true; FakeHeld = true; yield return null;
        FakeDown = false; yield return null;
        FakeHeld = false; FakeUp = true; yield return null; FakeUp = false;
    }

    /// <summary>Press at the first point, move along the path at a constant speed (units/s) and release.</summary>
    protected IEnumerator TDrag(float speed, params Vector2[] path)
    {
        FakePos = path[0]; FakeDown = true; FakeHeld = true; yield return null; FakeDown = false;
        for (int i = 1; i < path.Length; i++)
        {
            float len = Vector2.Distance(path[i - 1], path[i]), t = 0f, dur = Mathf.Max(0.02f, len / speed);
            while (t < dur) { t += Time.deltaTime; FakePos = Vector2.Lerp(path[i - 1], path[i], Mathf.Clamp01(t / dur)); yield return null; }
            FakePos = path[i];
        }
        yield return null;
        FakeHeld = false; FakeUp = true; yield return null; FakeUp = false;
    }

    protected IEnumerator THold(Vector2 p, float seconds)
    {
        FakePos = p; FakeDown = true; FakeHeld = true; yield return null; FakeDown = false;
        yield return new WaitForSeconds(seconds);
        FakeHeld = false; FakeUp = true; yield return null; FakeUp = false;
    }

    protected static Vector2 Center(Component c) { var col = c.GetComponent<Collider2D>(); return col != null ? (Vector2)col.bounds.center : (Vector2)c.transform.position; }

    protected Transform Find(string path)
    {
        var t = transform.Find(path);
        if (t != null) return t;
        // fall back to a deep search by leaf name
        var leaf = path.Substring(path.LastIndexOf('/') + 1);
        return GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == leaf);
    }

    protected T FindC<T>(string name) where T : Component
    {
        var t = GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name);
        return t != null ? t.GetComponent<T>() : null;
    }

    protected TextMesh MakeText(Transform at, string text, float size = 0.4f, Color? color = null, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        var tm = UiKit.I.Text(text, at != null ? at : transform, Vector2.zero, size, color ?? Color.white, anchor, 130, true);
        tm.gameObject.layer = UiKit.Layer;
        tm.transform.localPosition = new Vector3(0, 0, -2);
        return tm;
    }

    protected void Sfx(AudioClip c, float v = 1f)
    {
        if (c == null) return;
        if (_sfx == null) { _sfx = new GameObject("MinigameSfx").AddComponent<AudioSource>(); DontDestroyOnLoad(_sfx.gameObject); }
        _sfx.PlayOneShot(c, v);
    }

    protected void Sfx(AudioClip[] cs) { if (cs != null && cs.Length > 0) Sfx(cs[UnityEngine.Random.Range(0, cs.Length)]); }

    public void Close(bool success)
    {
        if (_closing) return;
        _closing = true;
        Sfx(CloseSound);
        if (Instance == this) Instance = null;
        UiKit.Blocked = false;
        Finished?.Invoke(success);
        Destroy(gameObject);
    }

    protected void Win(float delay = 0.6f) { if (!_closing) StartCoroutine(WinAfter(delay)); }

    System.Collections.IEnumerator WinAfter(float d) { yield return new WaitForSeconds(d); Close(true); }

    protected virtual void Update()
    {
        if (_closing) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(false); return; }
        if (MouseDown && Time.time - _openedAt > 0.15f)
        {
            foreach (var c in UiKit.PointAll(Mouse))
                if (c.transform.IsChildOf(transform) && c.name == "CloseButton") { Close(false); return; }
        }
        Tick();
    }

    protected abstract void Tick();

    void OnDestroy()
    {
        if (Instance == this) { Instance = null; UiKit.Blocked = false; }
    }
}
