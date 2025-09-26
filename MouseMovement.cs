using System;
using System.Threading;

static class MouseMovement
{
    // Smoothly move the cursor along a Bezier curve with random jitter
    public static void SmoothMoveTo(int targetX, int targetY, int minDurationMs, int maxDurationMs)
    {
        if (!InputSimulator.TryGetCursorPos(out var start))
        {
            start = new InputSimulator.POINT { X = targetX, Y = targetY };
        }
        double sx = start.X, sy = start.Y;
        double tx = targetX, ty = targetY;
        double dx = tx - sx, dy = ty - sy;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 1.0)
        {
            InputSimulator.TrySetCursorPos(targetX, targetY);
            return;
        }

        // Compute control points for a Bzier curve
        double px = -dy, py = dx;
        double plen = Math.Sqrt(px * px + py * py);
        if (plen > 0.0001) { px /= plen; py /= plen; } else { px = py = 0; }
        double curveFactor = 0.18;
        double maxOffset = Math.Clamp(dist * curveFactor, 8, 220);
        int sign = Random.Shared.Next(0, 2) == 0 ? -1 : 1;
        double offset1 = (Random.Shared.NextDouble() * 0.6 + 0.2) * maxOffset * sign;
        double offset2 = (Random.Shared.NextDouble() * 0.6 + 0.2) * maxOffset * -sign;
        double c1x = sx + dx * 0.25 + px * offset1;
        double c1y = sy + dy * 0.25 + py * offset1;
        double c2x = sx + dx * 0.75 + px * offset2;
        double c2y = sy + dy * 0.75 + py * offset2;

        int duration = Random.Shared.Next(minDurationMs, maxDurationMs + 1);
        int stepMs = Random.Shared.Next(8, 14);
        int steps = Math.Max(6, duration / stepMs);

        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            // Ease-in/ease-out function
            double u = t < 0.5
                ? 4 * t * t * t
                : 1 - Math.Pow(-2 * t + 2, 3) / 2.0;
            double inv = 1 - u;
            double bx = inv * inv * inv * sx
                      + 3 * inv * inv * u * c1x
                      + 3 * inv * u * u * c2x
                      + u * u * u * tx;
            double by = inv * inv * inv * sy
                      + 3 * inv * inv * u * c1y
                      + 3 * inv * u * u * c2y
                      + u * u * u * ty;

            // Add small random jitter decreasing over time
            int jitterAmp = (int)Math.Round((1.0 - t) * Math.Clamp(dist / 120.0, 1.5, 6.0));
            int jx = Random.Shared.Next(-jitterAmp, jitterAmp + 1);
            int jy = Random.Shared.Next(-jitterAmp, jitterAmp + 1);

            InputSimulator.TrySetCursorPos((int)Math.Round(bx) + jx, (int)Math.Round(by) + jy);
            Thread.Sleep(stepMs);
        }

        // Ensure final position and a small random pause
        InputSimulator.TrySetCursorPos(targetX, targetY);
        Thread.Sleep(10 + Random.Shared.Next(0, 50));
    }

    // Move to (x,y) and perform a double-click using randomized delays.
    public static void DoubleClickAt(int x, int y, ConfigData cfg)
    {
        SmoothMoveTo(x, y, cfg.MoveDurationMinMs, cfg.MoveDurationMaxMs);
        Thread.Sleep(Random.Shared.Next(20, 80));
        InputSimulator.LeftClick(Random.Shared.Next(20, 45));
        Thread.Sleep(Random.Shared.Next(80, 150));
        InputSimulator.LeftClick(Random.Shared.Next(20, 45));
    }
}