using UnityEngine;

public class SplashBounds : MonoBehaviour
{
    public ParticleSystem splashPS;
    public PolygonCollider2D mapBounds;     

    private void LateUpdate()       //check to ensure that only particles falling on the map create splashes, otherwise eliminate if outside map bounds
    {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[splashPS.particleCount];
        int count = splashPS.GetParticles(particles);

        for(int i = 0; i < count; i++)
        {
            Vector3 worldPos = splashPS.transform.TransformPoint(particles[i].position);
            Vector2 worldpos2D = new Vector2(worldPos.x, worldPos.y);
            if (!mapBounds.OverlapPoint(worldpos2D))
            {
                particles[i].remainingLifetime = 0;
            }
        }
        splashPS.SetParticles(particles, count);
    }
}
