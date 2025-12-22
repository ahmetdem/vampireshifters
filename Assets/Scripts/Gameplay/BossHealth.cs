public class BossHealth : Health
{
    // Override the Die method (make sure Die is 'protected virtual' in Health.cs)
    protected override void Die()
    {
        if (IsServer)
        {
            if (BossEventDirector.Instance != null)
            {
                BossEventDirector.Instance.OnBossDefeated();
            }
        }

        base.Die(); // Despawns the boss
    }
}
