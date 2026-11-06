using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerSurfaceDetector : MonoBehaviour
{
    [Header("Bölge Dışındaki Varsayılan")]
    [SerializeField] private SurfaceType defaultSurface = SurfaceType.Ground;
    [SerializeField, Min(0f)] private float defaultSoundMultiplier = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    public SurfaceType CurrentSurface { get; private set; }
    public float CurrentSoundMultiplier { get; private set; } = 1f;

    private PlayerMovement movement;

    // Aynı bölgeye birden fazla player collider'ı girebilir, bu yüzden tekrar kabul ediyoruz
    private readonly List<SurfaceZone> zones = new();

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        Evaluate();
    }

    public void Enter(SurfaceZone zone)
    {
        zones.Add(zone);
        Evaluate();
    }

    public void Exit(SurfaceZone zone)
    {
        zones.Remove(zone); // tek bir kaydı siler
        Evaluate();
    }

    private void Update()
    {
        // İçindeyken kapanan/yok edilen bölgeleri temizle
        bool changed = zones.RemoveAll(z => z == null || !z.isActiveAndEnabled) > 0;
        if (changed) Evaluate();
    }

    private void Evaluate()
    {
        SurfaceZone best = null;

        foreach (var z in zones)
        {
            // Öncelik büyük olan kazanır, eşitse sonradan girilen
            if (best == null || z.priority >= best.priority)
                best = z;
        }

        CurrentSurface = best != null ? best.surface : defaultSurface;
        CurrentSoundMultiplier = best != null ? best.soundMultiplier : defaultSoundMultiplier;

        movement.SetSurface(CurrentSurface);

        if (debugLog)
            Debug.Log($"Zemin: {CurrentSurface} | Ses çarpanı: {CurrentSoundMultiplier}");
    }
}