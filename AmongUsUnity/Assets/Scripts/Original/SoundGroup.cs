using UnityEngine;

/// <summary>A set of interchangeable clips (footsteps per floor material).</summary>
public class SoundGroup : ScriptableObject
{
    public AudioClip[] Clips;
    public AudioClip Random() => Clips != null && Clips.Length > 0 ? Clips[UnityEngine.Random.Range(0, Clips.Length)] : null;
}
