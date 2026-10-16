using UnityEngine;

public class Vent : MonoBehaviour
{
    public int Id;
    public Vent Left;
    public Vent Right;
    public Vent Center;
    public GameObject[] CleaningIndicators;
    public AnimationClip EnterVentAnim;
    public AnimationClip ExitVentAnim;
    public Vector3 Offset;
}
