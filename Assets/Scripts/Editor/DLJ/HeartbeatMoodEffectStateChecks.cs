using System;
using BlueComplex.Core.Stability;
using BlueComplex.UI.Effects.DLJ;

namespace BlueComplex.Editor.DLJ
{
    /// <summary>Unity를 실행하지 않고도 검증 가능한 상태 수명/전환 회귀 체크.</summary>
    public static class HeartbeatMoodEffectStateChecks
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/BlueComplex/DLJ/Validate Mood State Transitions")]
        public static void RunInEditor() => UnityEngine.Debug.Log($"[DLJ Mood] {Run()} state checks passed.");
#endif

        public static int Run()
        {
            var checks = 0;
            void Near(float expected, float actual, string reason)
            {
                if (Math.Abs(expected - actual) > 0.0001f)
                    throw new InvalidOperationException($"{reason}: expected {expected}, got {actual}");
                checks++;
            }
            var zone = new HeartbeatZone();
            var fx = new HeartbeatMoodEffectState();

            // 안정 구간 전체는 중립. 시작값 80 기준 보간으로 71/100에 효과가 새면 실패.
            foreach (var bpm in new[] { 71, 80, 100 })
            {
                fx.Show(bpm, zone, true);
                fx.Advance(0.1f);
                Near(0, fx.Depressed, "Stable must not be blue");
                Near(0, fx.Excited, "Stable must not have aberration");
            }

            fx.Show(55, zone, false);
            fx.Advance(0.65f);
            Near(0.75f, fx.Depressed, "Depressed holds after fade");
            fx.Advance(10f);
            Near(0.75f, fx.Depressed, "Depressed persists beyond two seconds");
            fx.Show(25, zone, false);
            fx.Advance(0.65f);
            Near(1, fx.Depressed, "Very depressed uses full intensity");

            fx.Show(125, zone, false);
            Near(2, fx.GlitchRemaining, "Entering excitement starts two seconds");
            Near(3.5f, fx.ChromaticRemaining, "Entering excitement starts 3.5 seconds of chromatic effects");
            Near(1, fx.ChromaticBurst, "Chromatic entry starts at full strength");
            fx.Advance(0.75f);
            Near(0, fx.Depressed, "Switching to excitement clears blue");
            Near(0.75f, fx.Excited, "Excitement remains visible");
            fx.Show(140, zone, false);
            Near(1.25f, fx.GlitchRemaining, "A turn inside excitement cannot restart glitch");
            fx.Show(170, zone, false);
            Near(1.25f, fx.GlitchRemaining, "Increasing severity cannot extend glitch");
            Near(2.75f, fx.ChromaticRemaining, "Turns and severity changes cannot extend chromatic effects");
            fx.Advance(1.24f);
            Near(0.01f, fx.GlitchRemaining, "Glitch survives until its deadline");
            fx.Advance(0.02f);
            Near(0, fx.GlitchRemaining, "Glitch expires after two seconds");
            Near(1, fx.Excited, "Excited mood remains after glitch expires");
            Near(1, fx.ChromaticBurst, "Chromatic effects outlast the glitch");
            fx.Advance(0.49f);
            Near(1, fx.ChromaticBurst, "Chromatic fade starts at 2.5 seconds");
            fx.Advance(0.5f);
            Near(0.5f, fx.ChromaticBurst, "Chromatic fade blends halfway to pink at three seconds");
            fx.Advance(0.5f);
            Near(0, fx.ChromaticRemaining, "Chromatic timer ends at 3.5 seconds");
            Near(0, fx.ChromaticBurst, "Chromatic effects fully stop at 3.5 seconds");
            fx.Show(125, zone, false);
            fx.Advance(30f);
            Near(0, fx.ChromaticBurst, "Staying excited cannot restart chromatic effects");
            Near(0.75f, fx.Excited, "Pastel pink mood persists after the burst");

            fx.Show(80, zone, false);
            fx.Advance(0.65f);
            Near(0, fx.Excited, "Returning stable clears aberration");
            fx.Show(125, zone, false);
            Near(2, fx.GlitchRemaining, "Re-entry starts a fresh burst");
            Near(3.5f, fx.ChromaticRemaining, "Re-entry restarts chromatic effects");
            fx.Advance(0.2f);
            fx.Show(55, zone, false);
            Near(0, fx.GlitchRemaining, "Leaving excitement cancels the burst immediately");
            Near(0, fx.ChromaticBurst, "Leaving excitement cancels chromatic effects");

            // 스냅/재시작/재활성화 시 한 프레임 뒤 과거 상태로 되돌아가면 실패.
            fx.Show(80, zone, true);
            fx.Advance(0.016f);
            Near(0, fx.Depressed, "Snap stays neutral next frame");
            Near(0, fx.Excited, "Snap cannot resume old transition");
            fx.Show(170, zone, true);
            fx.Advance(0.016f);
            Near(1, fx.Excited, "Rebinding an excited session snaps correctly");
            Near(0, fx.GlitchRemaining, "Rebinding cannot replay entry burst");
            Near(0, fx.ChromaticBurst, "Rebinding directly uses the settled pink tone");
            fx.Show(80, zone, true);
            fx.Show(125, zone, false);
            fx.Reset();
            fx.Advance(1);
            Near(0, fx.Excited, "Disable reset clears effect");
            Near(0, fx.GlitchRemaining, "Disable reset clears timer");
            Near(0, fx.ChromaticRemaining, "Disable reset clears chromatic timer");

            foreach (var bpm in new[] { 0, 9, 10, 39 })
            {
                fx.Show(bpm, zone, true);
                Near(1, fx.Depressed, "Low fatal and very depressed retain underwater effect");
            }
            foreach (var bpm in new[] { 151, 190, 191, 200 })
            {
                fx.Show(bpm, zone, true);
                Near(1, fx.Excited, "High fatal and very excited retain the pink tone");
                Near(0, fx.ChromaticBurst, "High heartbeat snap does not replay chromatic effects");
            }

            fx.Show(80, zone, true);
            fx.Show(125, zone, false);
            fx.Advance(-1f);
            Near(3.5f, fx.ChromaticRemaining, "Negative time cannot extend the burst");
            fx.Advance(60f);
            Near(0, fx.ChromaticBurst, "A long frame still expires chromatic effects");

            fx.ChromaticSeconds = 0f;
            fx.Show(80, zone, true);
            fx.Show(125, zone, false);
            Near(0, fx.ChromaticBurst, "Zero duration immediately uses pink tone");
            fx.ChromaticSeconds = 0.5f;
            fx.Show(80, zone, true);
            fx.Show(125, zone, false);
            Near(1, fx.ChromaticBurst, "Fade longer than duration is clamped to duration");
            fx.Advance(0.25f);
            Near(0.5f, fx.ChromaticBurst, "Short duration fades smoothly");
            fx.ChromaticFadeSeconds = 0f;
            Near(1, fx.ChromaticBurst, "Zero fade holds the burst until its deadline");
            fx.Advance(0.25f);
            Near(0, fx.ChromaticBurst, "Zero fade still stops at the deadline");
            return checks;
        }
    }
}
