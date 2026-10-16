using UnityEngine;

/// <summary>The dropship lobby (spawn points and music come from the original prefab).</summary>
public class LobbyBehaviour : MonoBehaviour
{
    public AudioClip SpawnSound;
    public AnimationClip SpawnInClip;
    public Vector2[] SpawnPositions;
    public AudioClip DropShipSound;
    public AudioClip MapTheme;
}
