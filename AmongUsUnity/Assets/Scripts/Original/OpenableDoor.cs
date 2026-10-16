using UnityEngine;

public abstract class OpenableDoor : MonoBehaviour
{
    public int Id;
    public SystemTypes Room;
    public abstract bool IsOpen { get; }
    public abstract void SetDoorway(bool open);
}
