using PowerTools;
using UnityEngine;

/// <summary>Colours and animates a player body using the original clips (idle/walk/ghost/spawn/vent).</summary>
public class PlayerVisual : MonoBehaviour
{
    public SpriteRenderer Body;
    public SpriteAnim Anim;
    public AnimationClip IdleClip, WalkClip, GhostClip, SpawnClip, EnterVentClip, ExitVentClip;

    static readonly int BodyColor = Shader.PropertyToID("_BodyColor");
    static readonly int BackColor = Shader.PropertyToID("_BackColor");
    static readonly int VisorColor = Shader.PropertyToID("_VisorColor");
    static readonly int Outline = Shader.PropertyToID("_Outline");
    static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");

    MaterialPropertyBlock _mpb;
    AnimationClip _want;
    bool _moving, _ghost;

    public void SetColor(Color body, Color shade, Color visor)
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        Body.GetPropertyBlock(_mpb);
        _mpb.SetColor(BodyColor, body);
        _mpb.SetColor(BackColor, shade);
        _mpb.SetColor(VisorColor, visor);
        Body.SetPropertyBlock(_mpb);
    }

    public void SetOutline(bool on, Color color)
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        Body.GetPropertyBlock(_mpb);
        _mpb.SetFloat(Outline, on ? 1f : 0f);
        _mpb.SetColor(OutlineColor, color);
        Body.SetPropertyBlock(_mpb);
    }

    public void SetState(bool moving, bool ghost, bool flip)
    {
        _moving = moving;
        _ghost = ghost;
        Body.flipX = flip;
        var clip = ghost ? GhostClip : (moving ? WalkClip : IdleClip);
        if (clip != null && clip != _want) { _want = clip; Anim.Play(clip); }
    }

    public void PlayOnce(AnimationClip clip)
    {
        if (clip == null) return;
        _want = clip;
        Anim.Play(clip);
    }
}
