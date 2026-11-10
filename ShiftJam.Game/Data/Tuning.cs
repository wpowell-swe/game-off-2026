namespace ShiftJam
{
    /// <summary>
    /// Every gameplay number in one place so it can be tuned without hunting through code.
    /// STUB: plain constants. Later: load from Content/Generated-style JSON.
    /// Pixels, seconds, and "frames" (1 frame = 1/60 s) as noted.
    /// </summary>
    public static class Tuning
    {
        public const float Frame = 1f / 60f;

        // Movement (pixels and seconds)
        public const float WalkSpeed = 80f;
        public const float Gravity = 600f;
        public const float JumpVelocity = 240f;
        public const float GroundAcceleration = 700f;
        public const float GroundFriction = 800f;
        public const float AirAcceleration = 450f;
        public const float AirFriction = 150f;
        public const float MaxFallSpeed = 320f;
        public const float JumpCutVelocity = 80f;      // upward speed cap once jump is released early
        public const float CoyoteTime = 0.08f;
        public const float JumpBufferTime = 0.1f;
        public const float MoveDeadzone = 0.2f;

        // Combat
        public const int LightDamage = 8;
        public const int HeavyDamage = 20;
        public const int LightChainMax = 3;

        // Attack timing in frames (startup / active / recovery)
        public const int LightStartupFrames = 3;
        public const int LightActiveFrames = 3;
        public const int LightRecoveryFrames = 9;
        public const int HeavyStartupFrames = 12;
        public const int HeavyActiveFrames = 5;
        public const int HeavyRecoveryFrames = 18;

        // Attack hitboxes (pixels, in front of the body)
        public const int LightReachX = 14;
        public const int LightHeight = 12;
        public const int LightOffsetY = 6;             // from top of body
        public const int HeavyReachX = 22;
        public const int HeavyHeight = 20;
        public const int HeavyOffsetY = 3;
        public const float LightKnockback = 40f;
        public const float HeavyKnockback = 190f;
        public const float LightLunge = 40f;
        public const float HeavyLunge = 70f;
        public const float AttackFriction = 400f;

        // Block
        public const float BlockDamageFraction = 0.2f;
        public const float BlockKnockbackFraction = 0.3f;
        public const float BlockInvulnTime = 0.1f;

        // Being hit
        public const float HurtStunTime = 0.3f;
        public const float HurtInvulnTime = 0.9f;
        public const float HurtPopVelocity = 70f;
        public const float MinHurtKnockback = 80f;
        public const float FlickerInterval = 0.06f;
        public const float KiDepletedStunTime = 0.5f;

        // Ki (see GDD: large up-front shift cost, small upkeep)
        public const float MaxKi = 100f;
        public const float ShiftKiCost = 40f;
        public const float AstralKiDrainPerSecond = 4f;
        public const float MaterialKiRegenPerSecond = 10f;

        public const int PlayerMaxHealth = 100;
    }
}
