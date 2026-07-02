using UnityEngine;

/// <summary>
/// A procedural VFX system that creates particle bursts without needing prefabs.
/// Spawns particle bursts tinted to match the color of the cleared tiles.
/// </summary>
public class TileVFX : MonoBehaviour
{
    public static TileVFX Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Spawns a particle burst at a position using a specified tint color.
    /// </summary>
    public void SpawnBurst(Vector3 position, Color color)
    {
        GameObject vfxObject = new GameObject("ProceduralBurstVFX");
        vfxObject.transform.position = position;

        ParticleSystem ps = vfxObject.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = vfxObject.GetComponent<ParticleSystemRenderer>();

        // Set rendering material to use a simple sprite shader to allow flat color rendering
        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
        {
            renderer.material = new Material(spriteShader);
        }

        // Configure main settings
        var main = ps.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.gravityModifier = 0.5f;
        main.startColor = color; // Tinted particle color
        main.stopAction = ParticleSystemStopAction.Destroy; // Clean up on finish

        // Configure burst emission
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

        // Configure shape (small circular ring spreading outwards)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        // Size decay over time
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        ps.Play();
    }
}
