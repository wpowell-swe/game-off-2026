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
        public static float WalkSpeed = Cfg.F("Tuning.WalkSpeed", 80f, v => WalkSpeed = v);
        public static float Gravity = Cfg.F("Tuning.Gravity", 600f, v => Gravity = v);
        public static float JumpVelocity = Cfg.F("Tuning.JumpVelocity", 260f, v => JumpVelocity = v);     // apex about 56 px: just clears a 3-tile (48 px) ledge, the tallest step in the game
        public static float GroundAcceleration = Cfg.F("Tuning.GroundAcceleration", 700f, v => GroundAcceleration = v);
        public static float GroundFriction = Cfg.F("Tuning.GroundFriction", 800f, v => GroundFriction = v);
        public static float AirAcceleration = Cfg.F("Tuning.AirAcceleration", 450f, v => AirAcceleration = v);
        public static float AirFriction = Cfg.F("Tuning.AirFriction", 150f, v => AirFriction = v);
        public static float MaxFallSpeed = Cfg.F("Tuning.MaxFallSpeed", 320f, v => MaxFallSpeed = v);
        public static float JumpCutVelocity = Cfg.F("Tuning.JumpCutVelocity", 80f, v => JumpCutVelocity = v);      // upward speed cap once jump is released early
        public static float CoyoteTime = Cfg.F("Tuning.CoyoteTime", 0.08f, v => CoyoteTime = v);
        public static float JumpBufferTime = Cfg.F("Tuning.JumpBufferTime", 0.1f, v => JumpBufferTime = v);
        public static float MoveDeadzone = Cfg.F("Tuning.MoveDeadzone", 0.2f, v => MoveDeadzone = v);

        // Combat
        public static int LightDamage = Cfg.I("Tuning.LightDamage", 8, v => LightDamage = v);
        public static int HeavyDamage = Cfg.I("Tuning.HeavyDamage", 34, v => HeavyDamage = v);
        public static int LightChainMax = Cfg.I("Tuning.LightChainMax", 3, v => LightChainMax = v);

        // Attack timing in frames (startup / active / recovery)
        public static int LightStartupFrames = Cfg.I("Tuning.LightStartupFrames", 3, v => LightStartupFrames = v);
        public static int LightActiveFrames = Cfg.I("Tuning.LightActiveFrames", 3, v => LightActiveFrames = v);
        public static int LightRecoveryFrames = Cfg.I("Tuning.LightRecoveryFrames", 11, v => LightRecoveryFrames = v);
        public static int HeavyStartupFrames = Cfg.I("Tuning.HeavyStartupFrames", 22, v => HeavyStartupFrames = v);   // long, planted wind-up: the swing is a commitment
        public static int HeavyActiveFrames = Cfg.I("Tuning.HeavyActiveFrames", 6, v => HeavyActiveFrames = v);
        public static int HeavyRecoveryFrames = Cfg.I("Tuning.HeavyRecoveryFrames", 36, v => HeavyRecoveryFrames = v);  // long recovery: a whiffed heavy is punished

        // Attack hitboxes (pixels, in front of the body)
        public static int LightReachX = Cfg.I("Tuning.LightReachX", 14, v => LightReachX = v);
        public static int LightHeight = Cfg.I("Tuning.LightHeight", 12, v => LightHeight = v);
        public static int LightOffsetY = Cfg.I("Tuning.LightOffsetY", 0, v => LightOffsetY = v);             // from top of body
        public static int HeavyReachX = Cfg.I("Tuning.HeavyReachX", 22, v => HeavyReachX = v);
        public static int HeavyHeight = Cfg.I("Tuning.HeavyHeight", 20, v => HeavyHeight = v);
        public static int HeavyOffsetY = Cfg.I("Tuning.HeavyOffsetY", -6, v => HeavyOffsetY = v);
        public static float LightKnockback = Cfg.F("Tuning.LightKnockback", 40f, v => LightKnockback = v);
        public static float HeavyKnockback = Cfg.F("Tuning.HeavyKnockback", 260f, v => HeavyKnockback = v);
        public static float LightLunge = Cfg.F("Tuning.LightLunge", 40f, v => LightLunge = v);
        public static float HeavyLunge = Cfg.F("Tuning.HeavyLunge", 95f, v => HeavyLunge = v);          // the step forward happens on release, not at the start
        public static float AttackFriction = Cfg.F("Tuning.AttackFriction", 400f, v => AttackFriction = v);

        // Block
        public static float BlockDamageFraction = Cfg.F("Tuning.BlockDamageFraction", 0.2f, v => BlockDamageFraction = v);
        public static float BlockKnockbackFraction = Cfg.F("Tuning.BlockKnockbackFraction", 0.3f, v => BlockKnockbackFraction = v);
        public static float BlockInvulnTime = Cfg.F("Tuning.BlockInvulnTime", 0.1f, v => BlockInvulnTime = v);

        // Being hit
        public static float HurtStunTime = Cfg.F("Tuning.HurtStunTime", 0.3f, v => HurtStunTime = v);
        public static float HurtInvulnTime = Cfg.F("Tuning.HurtInvulnTime", 0.9f, v => HurtInvulnTime = v);
        public static float HurtPopVelocity = Cfg.F("Tuning.HurtPopVelocity", 70f, v => HurtPopVelocity = v);
        public static float MinHurtKnockback = Cfg.F("Tuning.MinHurtKnockback", 80f, v => MinHurtKnockback = v);
        public static float FlickerInterval = Cfg.F("Tuning.FlickerInterval", 0.06f, v => FlickerInterval = v);
        public static float KiDepletedStunTime = Cfg.F("Tuning.KiDepletedStunTime", 0.5f, v => KiDepletedStunTime = v);
        public static float HeavyCooldown = Cfg.F("Tuning.HeavyCooldown", 0.45f, v => HeavyCooldown = v);      // extra seconds after a heavy ends before another can start
        public static float ShiftBufferTime = Cfg.F("Tuning.ShiftBufferTime", 0.25f, v => ShiftBufferTime = v);   // a shift pressed mid-attack fires when the attack ends

        // Ki (see GDD: large up-front shift cost, small upkeep)
        public static float MaxKi = Cfg.F("Tuning.MaxKi", 100f, v => MaxKi = v);
        public static float ShiftKiCost = Cfg.F("Tuning.ShiftKiCost", 40f, v => ShiftKiCost = v);
        public static float AstralKiDrainPerSecond = Cfg.F("Tuning.AstralKiDrainPerSecond", 4f, v => AstralKiDrainPerSecond = v);
        public static float MaterialKiRegenPerSecond = Cfg.F("Tuning.MaterialKiRegenPerSecond", 10f, v => MaterialKiRegenPerSecond = v);

        public static int PlayerMaxHealth = Cfg.I("Tuning.PlayerMaxHealth", 100, v => PlayerMaxHealth = v);

        // ---- Game feel (all seconds / pixels unless noted) ----
        /// <summary>World freeze when a hit lands: HitStopLight at damage 0 up to HitStopHeavy at HitStopMaxDamage.</summary>
        public static float HitStopLight = Cfg.F("Tuning.HitStopLight", 0.04f, v => HitStopLight = v);
        public static float HitStopHeavy = Cfg.F("Tuning.HitStopHeavy", 0.17f, v => HitStopHeavy = v);
        public static int HitStopMaxDamage = Cfg.I("Tuning.HitStopMaxDamage", 20, v => HitStopMaxDamage = v);
        /// <summary>Hits with Damage at or above this count as heavy (shake, heavy sound).</summary>
        public static int HeavyHitDamageThreshold = Cfg.I("Tuning.HeavyHitDamageThreshold", 15, v => HeavyHitDamageThreshold = v);
        /// <summary>Freeze at the moment of a plane shift.</summary>
        public static float ShiftHitStop = Cfg.F("Tuning.ShiftHitStop", 0.05f, v => ShiftHitStop = v);

        // Screen shake: magnitude in pixels (peak), duration in seconds.
        public static float ShakeHeavyHit = Cfg.F("Tuning.ShakeHeavyHit", 5f, v => ShakeHeavyHit = v);
        public static float ShakeHeavyHitTime = Cfg.F("Tuning.ShakeHeavyHitTime", 0.22f, v => ShakeHeavyHitTime = v);
        public static float ShakeHurt = Cfg.F("Tuning.ShakeHurt", 4f, v => ShakeHurt = v);
        public static float ShakeHurtTime = Cfg.F("Tuning.ShakeHurtTime", 0.25f, v => ShakeHurtTime = v);
        public static float ShakeShift = Cfg.F("Tuning.ShakeShift", 2f, v => ShakeShift = v);
        public static float ShakeShiftTime = Cfg.F("Tuning.ShakeShiftTime", 0.12f, v => ShakeShiftTime = v);

        // Shift flash and ring
        public static float ShiftFlashTime = Cfg.F("Tuning.ShiftFlashTime", 0.18f, v => ShiftFlashTime = v);
        public static float ShiftFlashAlpha = Cfg.F("Tuning.ShiftFlashAlpha", 0.7f, v => ShiftFlashAlpha = v);      // peak opacity of the full-screen flash
        public static float ShiftRingTime = Cfg.F("Tuning.ShiftRingTime", 0.3f, v => ShiftRingTime = v);
        public static float ShiftRingRadius = Cfg.F("Tuning.ShiftRingRadius", 56f, v => ShiftRingRadius = v);       // final radius in pixels
        public static int ShiftRingDots = Cfg.I("Tuning.ShiftRingDots", 20, v => ShiftRingDots = v);

        // Hit sparks
        public static int SparksLight = Cfg.I("Tuning.SparksLight", 6, v => SparksLight = v);
        public static int SparksHeavy = Cfg.I("Tuning.SparksHeavy", 14, v => SparksHeavy = v);
        public static float SparkLife = Cfg.F("Tuning.SparkLife", 0.22f, v => SparkLife = v);
        public static float ShakeLightHit = Cfg.F("Tuning.ShakeLightHit", 1.5f, v => ShakeLightHit = v);        // small shake on a light hit (pixels)
        public static float ShakeLightHitTime = Cfg.F("Tuning.ShakeLightHitTime", 0.08f, v => ShakeLightHitTime = v);
        /// <summary>The bright burst at the point of contact: size in pixels and how long it lasts.</summary>
        public static float ImpactSizeLight = Cfg.F("Tuning.ImpactSizeLight", 24f, v => ImpactSizeLight = v);
        public static float ImpactSizeHeavy = Cfg.F("Tuning.ImpactSizeHeavy", 40f, v => ImpactSizeHeavy = v);
        public static float ImpactTime = Cfg.F("Tuning.ImpactTime", 0.2f, v => ImpactTime = v);
        public static float SparkSpeed = Cfg.F("Tuning.SparkSpeed", 110f, v => SparkSpeed = v);           // pixels per second, max initial

        // Audio
        public static float SfxMasterVolume = Cfg.F("Tuning.SfxMasterVolume", 0.7f, v => SfxMasterVolume = v);
    }
}
