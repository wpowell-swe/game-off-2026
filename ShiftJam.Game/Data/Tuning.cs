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
        public const int HeavyDamage = 34;
        public const int LightChainMax = 3;

        // Attack timing in frames (startup / active / recovery)
        public const int LightStartupFrames = 3;
        public const int LightActiveFrames = 3;
        public const int LightRecoveryFrames = 11;
        public const int HeavyStartupFrames = 22;   // long, planted wind-up: the swing is a commitment
        public const int HeavyActiveFrames = 6;
        public const int HeavyRecoveryFrames = 36;  // long recovery: a whiffed heavy is punished

        // Attack hitboxes (pixels, in front of the body)
        public const int LightReachX = 14;
        public const int LightHeight = 12;
        public const int LightOffsetY = 6;             // from top of body
        public const int HeavyReachX = 22;
        public const int HeavyHeight = 20;
        public const int HeavyOffsetY = 3;
        public const float LightKnockback = 40f;
        public const float HeavyKnockback = 260f;
        public const float LightLunge = 40f;
        public const float HeavyLunge = 95f;          // the step forward happens on release, not at the start
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
        public const float HeavyCooldown = 0.45f;      // extra seconds after a heavy ends before another can start
        public const float ShiftBufferTime = 0.25f;   // a shift pressed mid-attack fires when the attack ends

        // Ki (see GDD: large up-front shift cost, small upkeep)
        public const float MaxKi = 100f;
        public const float ShiftKiCost = 40f;
        public const float AstralKiDrainPerSecond = 4f;
        public const float MaterialKiRegenPerSecond = 10f;

        public const int PlayerMaxHealth = 100;

        // ---- Game feel (all seconds / pixels unless noted) ----
        /// <summary>World freeze when a hit lands: HitStopLight at damage 0 up to HitStopHeavy at HitStopMaxDamage.</summary>
        public const float HitStopLight = 0.04f;
        public const float HitStopHeavy = 0.17f;
        public const int HitStopMaxDamage = 20;
        /// <summary>Hits with Damage at or above this count as heavy (shake, heavy sound).</summary>
        public const int HeavyHitDamageThreshold = 15;
        /// <summary>Freeze at the moment of a plane shift.</summary>
        public const float ShiftHitStop = 0.05f;

        // Screen shake: magnitude in pixels (peak), duration in seconds.
        public const float ShakeHeavyHit = 5f;
        public const float ShakeHeavyHitTime = 0.22f;
        public const float ShakeHurt = 4f;
        public const float ShakeHurtTime = 0.25f;
        public const float ShakeShift = 2f;
        public const float ShakeShiftTime = 0.12f;

        // Shift flash and ring
        public const float ShiftFlashTime = 0.18f;
        public const float ShiftFlashAlpha = 0.7f;      // peak opacity of the full-screen flash
        public const float ShiftRingTime = 0.3f;
        public const float ShiftRingRadius = 56f;       // final radius in pixels
        public const int ShiftRingDots = 20;

        // Hit sparks
        public const int SparksLight = 6;
        public const int SparksHeavy = 14;
        public const float SparkLife = 0.22f;
        public const float SparkSpeed = 110f;           // pixels per second, max initial

        // Audio
        public const float SfxMasterVolume = 0.7f;
    }
}
