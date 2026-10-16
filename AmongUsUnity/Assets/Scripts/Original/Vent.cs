using PowerTools;
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

    public Vent[] Neighbours => new[] { Left, Right, Center };

    /// <summary>Show the arrows towards the connected vents (only while a player sits in this vent).</summary>
    public void ShowArrows(bool on)
    {
        var arrows = new[] { transform.Find("Arrow"), transform.Find("Arrow (1)"), transform.Find("Arrow (2)") };
        var targets = Neighbours;
        for (int i = 0; i < arrows.Length; i++)
        {
            if (arrows[i] == null) continue;
            bool has = on && targets[i] != null;
            arrows[i].gameObject.SetActive(has);
            if (!has) continue;
            var d = (Vector2)(targets[i].transform.position - transform.position);
            arrows[i].position = transform.position + (Vector3)(d.normalized * 0.75f) + new Vector3(0, 0, -0.1f);
            arrows[i].rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
            var cleaning = arrows[i].Find("CleaningIndicator");
            if (cleaning != null) cleaning.gameObject.SetActive(false);
        }
    }

    public Vent ArrowAt(Vector2 world)
    {
        var arrows = new[] { transform.Find("Arrow"), transform.Find("Arrow (1)"), transform.Find("Arrow (2)") };
        var targets = Neighbours;
        for (int i = 0; i < arrows.Length; i++)
            if (arrows[i] != null && arrows[i].gameObject.activeSelf && targets[i] != null && Vector2.Distance(arrows[i].position, world) < 0.7f) return targets[i];
        return null;
    }

    public void PlayAnim(bool enter)
    {
        var a = GetComponent<SpriteAnim>();
        var clip = enter ? EnterVentAnim : ExitVentAnim;
        if (a != null && clip != null) a.Play(clip);
    }
}
