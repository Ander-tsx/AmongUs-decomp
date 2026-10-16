using UnityEngine;

public class AmbientSoundPlayer : MonoBehaviour
{
    public AudioClip AmbientSound;
    public Collider2D[] HitAreas;
    public float MaxVolume;
    public float DistanceFallOff;
    public float FallOffRate;
}
