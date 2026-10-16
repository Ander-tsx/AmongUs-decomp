using PowerTools;
using UnityEngine;

public class PlainDoor : OpenableDoor
{
    public bool Open;
    public BoxCollider2D myCollider;
    public Collider2D shadowCollider;
    public SpriteAnim animator;
    public AnimationClip OpenDoorAnim;
    public AnimationClip CloseDoorAnim;
    public AudioClip OpenSound;
    public AudioClip CloseSound;

    public override bool IsOpen => Open;

    public override void SetDoorway(bool open)
    {
        Open = open;
        if (myCollider != null) myCollider.enabled = !open;
        if (shadowCollider != null) shadowCollider.enabled = !open;
        var clip = open ? OpenDoorAnim : CloseDoorAnim;
        if (animator != null && clip != null) animator.Play(clip);
    }

    protected virtual void Start() { SetDoorway(Open); }
}
