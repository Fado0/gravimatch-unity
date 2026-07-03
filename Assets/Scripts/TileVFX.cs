using UnityEngine;


public class TileVFX : MonoBehaviour
{
    private static TileVFX instance;
    public static TileVFX Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("TileVFX");
                instance = go.AddComponent<TileVFX>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    
    public void SpawnBurst(Vector3 position, Color color)
    {
        GameObject vfxObject = new GameObject("ProceduralBurstVFX");
        vfxObject.transform.position = position;

        ParticleSystem ps = vfxObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystemRenderer renderer = vfxObject.GetComponent<ParticleSystemRenderer>();


        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
        {
            renderer.material = new Material(spriteShader);
        }

        
        var main = ps.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.gravityModifier = 0.5f;
        main.startColor = color; 
        main.stopAction = ParticleSystemStopAction.Destroy; // Clean up on finish

       
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        ps.Play();
    }
}
