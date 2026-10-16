using UnityEngine;

public class ShipStatus : MonoBehaviour
{
    public static ShipStatus Instance;

    public Color CameraColor;
    public float MaxLightRadius;
    public float MinLightRadius;
    public float MapScale;
    public SystemConsole EmergencyButton;
    public Vector2 InitialSpawnCenter;
    public Vector2 MeetingSpawnCenter;
    public Vector2 MeetingSpawnCenter2;
    public float SpawnRadius;
    public SurvCamera[] AllCameras;
    public OpenableDoor[] AllDoors;
    public Console[] AllConsoles;
    public float EmergencyCooldown;

    void Awake() { Instance = this; }
}
