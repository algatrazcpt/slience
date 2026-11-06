using UnityEngine;

public class SurfaceZone : MonoBehaviour
{
    public SurfaceType surface = SurfaceType.Metal;

    [Tooltip("Bu zeminde ses yarıçapı çarpanı. 1 = normal, 2 = iki kat, 0.5 = yarı")]
    [Min(0f)] public float soundMultiplier = 1.5f;

    [Tooltip("İç içe bölgelerde büyük olan kazanır (örn. su üstündeki cam kırığı)")]
    public int priority = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerSoundEmitter>(out _)) return; // ses collider'ı zemin sayılmaz

        var detector = other.GetComponentInParent<PlayerSurfaceDetector>();
        if (detector != null) detector.Enter(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerSoundEmitter>(out _)) return;

        var detector = other.GetComponentInParent<PlayerSurfaceDetector>();
        if (detector != null) detector.Exit(this);
    }
}