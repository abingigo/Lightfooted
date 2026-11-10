using UnityEngine;

public class PurpleLightParticles : MonoBehaviour
{
    ParticleSystem ps;
    Collider2D lightBox;
    ParticleSystem.Particle[] dots;
    float spawnTime;
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        lightBox = GetComponentInParent<Collider2D>();

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
         var emission = ps.emission;
         emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = false;

        ps.Clear();
        ps.Play();
        dots = new ParticleSystem.Particle[main.maxParticles];
        for (int i = 0; i < 6; i++)
        {
            SpawnDot();
        }
    }

    void LateUpdate()
    {
            int dotCount = ps.GetParticles(dots);

        for (int i = 0; i < dotCount; i++)
        {
            if (!lightBox.OverlapPoint(dots[i].position))
            {
                dots[i].remainingLifetime = -1f;
            }
        }

        ps.SetParticles(dots, dotCount);

        spawnTime += Time.deltaTime;
        while (spawnTime >= 0.2f)
        {
        spawnTime = spawnTime - 0.2f;
            SpawnDot();
        }
    }

    void SpawnDot()
    {
        Bounds box = lightBox.bounds;

        for (int i = 0; i  < 20; i++)
        {
            float x = Random.Range(box.min.x,box.max.x);
            float y = Random.Range(box.min.y, box.max.y);
            Vector3 pos = new Vector3(x, y, transform.position.z);

            if (lightBox.OverlapPoint(pos))
            {
                var dot = new ParticleSystem.EmitParams();
                dot.position = pos;
                ps.Emit(dot, 1);
                break;
            }
        }
    }
}