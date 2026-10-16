using UnityEngine;

/// <summary>Left-hand wire of the wiring panel (plug head + stretchable cord).</summary>
public class Wire : MonoBehaviour
{
    public SpriteRenderer Liner;
    public SpriteRenderer ColorBase;
    public SpriteRenderer SymbolBase;
    public SpriteRenderer ColorEnd;
    public Collider2D hitbox;
    public SpriteRenderer WireTip;
    public sbyte WireId;
}
