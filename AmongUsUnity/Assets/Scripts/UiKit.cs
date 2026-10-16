using System;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>
    /// Sprite-based UI like the original: a dedicated orthographic UI camera (size 3), elements are sprites and
    /// TextMeshes on a UI layer parked far from the map, buttons are colliders hit-tested from the mouse.
    /// </summary>
    public class UiKit : MonoBehaviour
    {
        public const int Layer = 5;                     // built-in "UI" layer
        static readonly Vector3 Origin = new Vector3(0, 5000, 0);

        public static UiKit I;
        public Camera Cam;
        public Transform Root;

        public float HalfH => Cam.orthographicSize;
        public float HalfW => Cam.orthographicSize * Cam.aspect;

        public static UiKit Create(Camera worldCam)
        {
            var go = new GameObject("UiKit");
            var k = go.AddComponent<UiKit>();
            I = k;
            worldCam.cullingMask &= ~(1 << Layer);

            var camGo = new GameObject("UI Camera");
            camGo.transform.SetParent(go.transform, false);
            camGo.transform.position = Origin + new Vector3(0, 0, -10);
            k.Cam = camGo.AddComponent<Camera>();
            k.Cam.orthographic = true;
            k.Cam.orthographicSize = 3f;
            k.Cam.clearFlags = CameraClearFlags.Depth;
            k.Cam.cullingMask = 1 << Layer;
            k.Cam.depth = 10;
            k.Root = new GameObject("UI Root").transform;
            k.Root.SetParent(go.transform, false);
            k.Root.position = Origin;
            SetLayer(k.Root.gameObject);
            return k;
        }

        static void SetLayer(GameObject g) { g.layer = Layer; }

        public Transform Group(string name, Transform parent = null)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent != null ? parent : Root, false);
            SetLayer(g);
            return g.transform;
        }

        public SpriteRenderer Sprite(string spriteName, Transform parent, Vector2 pos, int order = 0, float scale = 1f)
        {
            var g = new GameObject(spriteName);
            g.transform.SetParent(parent != null ? parent : Root, false);
            g.transform.localPosition = pos;
            g.transform.localScale = new Vector3(scale, scale, 1);
            SetLayer(g);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = UiSprites.Get(spriteName);
            sr.sortingOrder = order;
            return sr;
        }

        public SpriteRenderer Sliced(string spriteName, Transform parent, Vector2 pos, Vector2 size, int order = 0)
        {
            var sr = Sprite(spriteName, parent, pos, order);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            return sr;
        }

        static Material _playerMat;

        /// <summary>A crewmate icon (mask-coloured sprite) for meeting lists, ejection and intro screens.</summary>
        public SpriteRenderer PlayerIcon(Transform parent, Vector2 pos, float scale, int colorIndex, int order = 5, string sprite = "idleNoshadow")
        {
            var sr = Sprite(sprite, parent, pos, order, scale);
            if (_playerMat == null) _playerMat = Resources.Load<Material>("PlayerMaterial");
            sr.sharedMaterial = _playerMat;
            var mpb = new MaterialPropertyBlock();
            sr.GetPropertyBlock(mpb);
            mpb.SetColor("_BodyColor", Palette.Body(colorIndex));
            mpb.SetColor("_BackColor", Palette.Shade(colorIndex));
            mpb.SetColor("_VisorColor", Palette.Visor);
            sr.SetPropertyBlock(mpb);
            return sr;
        }

        public static void Tint(SpriteRenderer sr, Color body, Color shade)
        {
            var mpb = new MaterialPropertyBlock();
            sr.GetPropertyBlock(mpb);
            mpb.SetColor("_BodyColor", body);
            mpb.SetColor("_BackColor", shade);
            mpb.SetColor("_VisorColor", Palette.Visor);
            sr.SetPropertyBlock(mpb);
        }

        /// <summary>Flat colour rectangle (size in UI units), for panels and bars.</summary>
        public SpriteRenderer Rect(Transform parent, Vector2 pos, Vector2 size, Color color, int order = 0)
        {
            var sr = Sprite("blank", parent, pos, order);
            sr.sprite = WhiteSprite();
            sr.transform.localScale = new Vector3(size.x, size.y, 1);
            sr.color = color;
            return sr;
        }

        static Sprite _white;
        public static Sprite WhiteSprite()
        {
            if (_white == null)
            {
                var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                t.SetPixel(0, 0, Color.white);
                t.Apply();
                _white = UnityEngine.Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return _white;
        }

        public TextMesh Text(string text, Transform parent, Vector2 pos, float size = 0.3f, Color? color = null,
                             TextAnchor anchor = TextAnchor.MiddleCenter, int order = 10, bool black = false)
        {
            var g = new GameObject("Text");
            g.transform.SetParent(parent != null ? parent : Root, false);
            g.transform.localPosition = pos;
            SetLayer(g);
            var tm = g.AddComponent<TextMesh>();
            var font = black ? UiSprites.Instance.FontBlack : UiSprites.Instance.Font;
            tm.font = font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = size / 8f;       // ~ size in world units for a 64pt font
            tm.anchor = anchor;
            tm.alignment = anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.UpperLeft || anchor == TextAnchor.LowerLeft
                ? TextAlignment.Left
                : anchor == TextAnchor.MiddleRight || anchor == TextAnchor.UpperRight || anchor == TextAnchor.LowerRight ? TextAlignment.Right : TextAlignment.Center;
            tm.color = color ?? Color.white;
            var mr = g.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            mr.sortingOrder = order;
            return tm;
        }

        public UiButton Button(SpriteRenderer sr, Action onClick, Vector2? colliderSize = null)
        {
            var b = sr.gameObject.AddComponent<UiButton>();
            b.Sprite = sr;
            b.OnClick = onClick;
            var col = sr.gameObject.AddComponent<BoxCollider2D>();
            var size = colliderSize ?? (sr.sprite != null ? (Vector2)sr.sprite.bounds.size : Vector2.one);
            col.size = size;
            return b;
        }

        // ---- input ----
        UiButton _hover;

        void Update()
        {
            var wp = (Vector2)Cam.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.OverlapPoint(wp, 1 << Layer);
            var btn = hit != null ? hit.GetComponent<UiButton>() : null;
            if (btn != null && (!btn.gameObject.activeInHierarchy || !btn.Interactable)) btn = null;
            if (btn != _hover)
            {
                if (_hover != null) _hover.SetHover(false);
                _hover = btn;
                if (_hover != null) _hover.SetHover(true);
            }
            if (_hover != null && Input.GetMouseButtonDown(0)) _hover.OnClick?.Invoke();
        }
    }

    public class UiButton : MonoBehaviour
    {
        public SpriteRenderer Sprite;
        public Action OnClick;
        public bool Interactable = true;
        Color _base = Color.white;

        public void SetHover(bool on)
        {
            if (Sprite == null) return;
            Sprite.color = on ? new Color(1.15f, 1.15f, 1.15f, _base.a) : _base;
        }

        public void SetTint(Color c) { _base = c; if (Sprite != null) Sprite.color = c; }
    }
}
