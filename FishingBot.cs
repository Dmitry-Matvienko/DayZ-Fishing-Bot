using OpenCvSharp;
using System;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

class FishingBot
{
    private readonly ConfigData cfg;
    private readonly TemplateCache itemsCache;
    private readonly TemplateCache generalCache;
    private bool running = false;
    private readonly Random rnd = Random.Shared;

    public FishingBot(ConfigData config)
    {
        cfg = config;
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        itemsCache = new TemplateCache(System.IO.Path.Combine(baseDir, cfg.TemplatesFolder));
        generalCache = new TemplateCache(System.IO.Path.Combine(baseDir, cfg.GeneralFolder));
    }

    // Main loop of the bot
    public void Run()
    {
        running = true;
        int cycle = 0;
        Console.WriteLine("Fishing bot started. Press Ctrl+C to stop.");

        while (running)
        {
            cycle++;
            Console.WriteLine($"\n--- Cycle #{cycle} ---");

            // Hold left mouse button for a random duration
            int holdMs = rnd.Next(cfg.HoldLeftMinMs, cfg.HoldLeftMaxMs + 1);
            Console.WriteLine($"Holding left button for {holdMs} ms...");
            InputSimulator.LeftDown();

            try
            {
                var sw = Stopwatch.StartNew();
                while (running && sw.ElapsedMilliseconds < holdMs)
                {
                    int remaining = (int) (holdMs - sw.ElapsedMilliseconds);
                    int chunk = rnd.Next(200, 701);
                    Thread.Sleep(Math.Min(chunk, remaining));
                }
            }
            finally
            {
                InputSimulator.LeftUp();
            }
            if (!running) break;

            Thread.Sleep(rnd.Next(cfg.DelayAfterHoldMinMs, cfg.DelayAfterHoldMaxMs + 1));
            if (!running) break;
            Thread.Sleep(rnd.Next(400, 800));

            // Open inventory (Mouse4 / XBUTTON1)
            Console.WriteLine("Opening inventory (Mouse4)...");
            InputSimulator.XButtonDown(InputSimulator.XBUTTON1);
            Thread.Sleep(cfg.OpenInvClickDownMs + rnd.Next(10, 30));
            InputSimulator.XButtonUp(InputSimulator.XBUTTON1);

            Thread.Sleep(rnd.Next(200, 600));
            MoveCursorOrThrowBrokenBait(rnd, cfg, doPickup: false);

            if (!running) break;

            // Update template caches
            itemsCache.EnsureUpToDate();
            generalCache.EnsureUpToDate();

            // If no item templates, close inventory and wait
            if (itemsCache.IsEmpty())
            {
                Console.WriteLine("No item templates found. Closing inventory and waiting.");
                InputSimulator.XButtonDown(InputSimulator.XBUTTON1);
                Thread.Sleep(cfg.OpenInvClickDownMs + rnd.Next(10, 30));
                InputSimulator.XButtonUp(InputSimulator.XBUTTON1);
                Thread.Sleep(rnd.Next(cfg.DelayBetweenCyclesMinMs, cfg.DelayBetweenCyclesMaxMs + 1));
                continue;
            }

            // Search for fish under the player (iterate item templates)
            using (var screenMat = TemplateMatcher.CaptureScreen())
            {
                double bestScore = 0.0;
                System.Drawing.Point? bestPos = null;
                string bestTemplateName = null;

                foreach (var (name, mat) in itemsCache.GetAll())
                {
                    var (pos, score) = TemplateMatcher.FindBestMatch(screenMat, mat);
                    if (pos != null && score > bestScore)
                    {
                        bestScore = score;
                        bestPos = pos;
                        bestTemplateName = name;
                    }
                }

                if (bestPos == null || bestScore < cfg.MatchThreshold)
                {
                    Console.WriteLine($"No fish found (bestScore={bestScore:F3}). Checking rod/bait...");
                    CheckingRod();
                    Thread.Sleep(rnd.Next(150, 300));

                    // Workaround: sometimes holding right-click fixes a game bug
                    int fixHoldMs = rnd.Next(1000, 1601);
                    Console.WriteLine($"Applying fix: hold right button for {fixHoldMs} ms.");
                    InputSimulator.RightDown();
                    Thread.Sleep(fixHoldMs);
                    InputSimulator.RightUp();
                    Thread.Sleep(rnd.Next(cfg.DelayBetweenCyclesMinMs, cfg.DelayBetweenCyclesMaxMs + 1));
                    continue;
                }

                // Move to fish and double-click
                int jitterX = rnd.Next(-cfg.JitterAmpPx, cfg.JitterAmpPx + 1);
                int jitterY = rnd.Next(-cfg.JitterAmpPx, cfg.JitterAmpPx + 1);
                var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                int targetX = Math.Clamp(bestPos.Value.X + jitterX, screenBounds.Left + 2, screenBounds.Right - 2);
                int targetY = Math.Clamp(bestPos.Value.Y + jitterY, screenBounds.Top + 2, screenBounds.Bottom - 2);

                Console.WriteLine($"Found '{bestTemplateName}' @ {bestPos.Value.X},{bestPos.Value.Y} (score={bestScore:F3}) -> clicking at {targetX},{targetY}.");
                MouseMovement.DoubleClickAt(targetX, targetY, cfg);
                Thread.Sleep(rnd.Next(100, 220));

                // Inventory is still open. handle rod state after picking fish
                CheckingRod();
            }
        }

        Console.WriteLine("Fishing bot stopped.");
    }

    // Logic for checking rod state and handling bait
    private void CheckingRod()
    {
        try
        {
            generalCache.EnsureUpToDate();

            // Capture screen to detect rod state
            using (var screenAfterPick = TemplateMatcher.CaptureScreen())
            {
                (System.Drawing.Point? pos, double score, string name) bestGeneral = (null, 0.0, null);
                foreach (var (name, mat) in generalCache.GetAll())
                {
                    var (pos, score) = TemplateMatcher.FindBestMatch(screenAfterPick, mat);
                    if (pos != null && score > bestGeneral.score)
                    {
                        bestGeneral = (pos, score, name);
                    }
                }

                if (bestGeneral.pos == null || bestGeneral.score < cfg.MatchThreshold)
                {
                    Console.WriteLine("Rod state not recognizedю. Closing inventory.");
                    InputSimulator.XButtonDown(InputSimulator.XBUTTON1);
                    Thread.Sleep(cfg.OpenInvClickDownMs + rnd.Next(10, 30));
                    InputSimulator.XButtonUp(InputSimulator.XBUTTON1);
                    Thread.Sleep(rnd.Next(cfg.DelayBetweenCyclesMinMs, cfg.DelayBetweenCyclesMaxMs + 1));
                    return;
                }

                Console.WriteLine($"Detected rod state '{bestGeneral.name}' (score={bestGeneral.score:F3}).");

                // If the rod is ready (rod_ready.png). Bait on the rod
                if (bestGeneral.name.Equals(cfg.RodReadyTemplate, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Rod is marked as ready? Verifying...");

                    // Move cursor near the detected position to verify true/false
                    int checkX = bestGeneral.pos.Value.X + rnd.Next(-Math.Min(3, cfg.JitterAmpPx), Math.Min(3, cfg.JitterAmpPx) + 1);
                    int checkY = bestGeneral.pos.Value.Y + rnd.Next(-Math.Min(3, cfg.JitterAmpPx), Math.Min(3, cfg.JitterAmpPx) + 1);
                    MouseMovement.SmoothMoveTo(checkX, checkY, cfg.MoveDurationMinMs, cfg.MoveDurationMaxMs);
                    Thread.Sleep(rnd.Next(120, 190));

                    using (var checkScreen = TemplateMatcher.CaptureScreen())
                    {
                        Mat trueMat = null, falseMat = null;
                        foreach (var (gname, gmat) in generalCache.GetAll())
                        {
                            if (string.Equals(gname, cfg.TrueRodReadyTemplate, StringComparison.OrdinalIgnoreCase)) trueMat = gmat;
                            if (string.Equals(gname, cfg.FalseRodReadyTemplate, StringComparison.OrdinalIgnoreCase)) falseMat = gmat;
                        }

                        bool foundTrue = false;
                        if (trueMat != null)
                        {
                            var (posT, scoreT) = TemplateMatcher.FindBestMatch(checkScreen, trueMat);
                            Console.WriteLine($"   Checking TrueRodReady: score={scoreT:F3}, pos={(posT == null ? "null" : posT.ToString())}");
                            if (posT != null && scoreT >= cfg.MatchThreshold)
                            {
                                Console.WriteLine("TrueRodReady found – rod is confirmed ready. Closing inventory.");
                                InputSimulator.XButtonDown(InputSimulator.XBUTTON1);
                                Thread.Sleep(cfg.OpenInvClickDownMs + rnd.Next(10, 30));
                                InputSimulator.XButtonUp(InputSimulator.XBUTTON1);
                                Thread.Sleep(rnd.Next(cfg.DelayBetweenCyclesMinMs, cfg.DelayBetweenCyclesMaxMs + 1));
                                foundTrue = true;
                            }
                        }
                        if (foundTrue) return;

                        bool foundFalse = false;
                        if (falseMat != null)
                        {
                            var (posF, scoreF) = TemplateMatcher.FindBestMatch(checkScreen, falseMat);
                            Console.WriteLine($"   Checking FalseRodReady: score={scoreF:F3}, pos={(posF == null ? "null" : posF.ToString())}");
                            if (posF != null && scoreF >= cfg.MatchThreshold)
                            {
                                Console.WriteLine("FalseRodReady found – rod is not ready. Will continue handle bait.");
                                foundFalse = true;
                            }
                        }

                        if (foundFalse)
                        {
                            MoveCursorOrThrowBrokenBait(rnd, cfg, doPickup: true);
                        }
                        else
                        {
                            Console.WriteLine("Could not determine True/False rod state. Assuming not ready and changing the bait.");
                            MoveCursorOrThrowBrokenBait(rnd, cfg, doPickup: true);
                        }
                    }
                }
                // Handling hook_no_bait or hook_empty by dragging bait
                Console.WriteLine($"Looking for bait '{cfg.BaitTemplateName}' in inventory...");
                System.Drawing.Point? baitPos = null;
                double baitScore = 0;
                Mat baitMat = null;

                // Find bait template in generalCache
                foreach (var (gname, gmat) in generalCache.GetAll())
                {
                    if (string.Equals(gname, cfg.BaitTemplateName, StringComparison.OrdinalIgnoreCase))
                    {
                        baitMat = gmat;
                        break;
                    }
                }

                if (baitMat == null)
                {
                    Console.WriteLine($"Bait '{cfg.BaitTemplateName}' not found in general folder.");
                }
                else
                {
                    using (var invScreen = TemplateMatcher.CaptureScreen())
                    {
                        var (pos, score) = TemplateMatcher.FindBestMatch(invScreen, baitMat);
                        baitPos = pos;
                        baitScore = score;
                        Console.WriteLine($"Bait search: score={score:F3}, pos={(pos == null ? "null" : pos.ToString())}");
                    }
                }

                if (baitPos == null)
                {
                    Console.WriteLine("Bait not found in inventory. Closing inventory.");
                    InputSimulator.XButtonDown(InputSimulator.XBUTTON1);
                    Thread.Sleep(cfg.OpenInvClickDownMs + rnd.Next(10, 30));
                    InputSimulator.XButtonUp(InputSimulator.XBUTTON1);
                    Thread.Sleep(rnd.Next(cfg.DelayBetweenCyclesMinMs, cfg.DelayBetweenCyclesMaxMs + 1));
                    return;
                }

                Console.WriteLine($"Found bait at {baitPos.Value.X},{baitPos.Value.Y} (score={baitScore:F3}). Вragging to hook.");

                // Calculate drag-and-drop coordinates
                int dragStartX = baitPos.Value.X + rnd.Next(-3, 4);
                int dragStartY = baitPos.Value.Y + rnd.Next(-3, 4);
                int dropX = bestGeneral.pos.Value.X + rnd.Next(-cfg.JitterAmpPx, cfg.JitterAmpPx + 1);
                int dropY = bestGeneral.pos.Value.Y + rnd.Next(-cfg.JitterAmpPx, cfg.JitterAmpPx + 1);

                if (bestGeneral.name.Equals(cfg.HookNoBaitTemplate, StringComparison.OrdinalIgnoreCase)
                 || bestGeneral.name.Equals(cfg.HookNoBaitTemplateDestroyed, StringComparison.OrdinalIgnoreCase))
                {
                    dropY += cfg.HookEmptyYOffset; // adjust drop position for empty hook
                    Console.WriteLine("Hook empty. Using offset to drop bait.");
                }

                // Perform drag-and-drop
                MouseMovement.SmoothMoveTo(dragStartX, dragStartY, cfg.MoveDurationMinMs, cfg.MoveDurationMaxMs);
                Thread.Sleep(Random.Shared.Next(20, 60));
                InputSimulator.LeftDown();
                Thread.Sleep(Random.Shared.Next(80, 160));

                MouseMovement.SmoothMoveTo(dropX, dropY, cfg.MoveDurationMinMs, cfg.MoveDurationMaxMs);
                Thread.Sleep(Random.Shared.Next(60, 150));
                InputSimulator.LeftUp();

                Console.WriteLine("Bait moved. Closing inventory and continuing.");
                InputSimulator.XButtonDown(InputSimulator.XBUTTON1);
                Thread.Sleep(cfg.OpenInvClickDownMs + rnd.Next(10, 30));
                InputSimulator.XButtonUp(InputSimulator.XBUTTON1);
                Thread.Sleep(rnd.Next(cfg.DelayBetweenCyclesMinMs, cfg.DelayBetweenCyclesMaxMs + 1));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HandleRodWhenNoFish: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles throwing away the broken bait (when found FalseRodReady).
    /// Or move the cursor so that when the inventory is opened, information about the item is not displayed
    /// If doPickup==true: press LMB, drag to drop zone and release.
    /// If doPickup==false: only move cursor (no clicks).
    /// </summary>
    static void MoveCursorOrThrowBrokenBait(Random rnd, ConfigData cfg, bool doPickup)
    {
        try
        {
            // Get current cursor (fallback to center if unavailable)
            if (!InputSimulator.TryGetCursorPos(out var cur))
            {
                var scr = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                cur = new InputSimulator.POINT { X = (scr.Left + scr.Right) / 2, Y = (scr.Top + scr.Bottom) / 2 };
            }

            int startX = cur.X;
            int startY = cur.Y;

            var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;

            // Choosing drop coordinates somewhere near the left edge vertically near current Y
            int dropX = screenBounds.Left + rnd.Next(8, 28);
            int dropY = Math.Clamp(startY + rnd.Next(-30, 31), screenBounds.Top + 10, screenBounds.Bottom - 10);

            Thread.Sleep(rnd.Next(40, 120));

            if (doPickup)
            {
                // Press LMB (pickup)
                InputSimulator.LeftDown();
                Thread.Sleep(rnd.Next(70, 150)); // small holding for pickup
            }

            // Smooth move to drop zone
            MouseMovement.SmoothMoveTo(dropX, dropY, cfg.MoveDurationMinMs, cfg.MoveDurationMaxMs);
            Thread.Sleep(rnd.Next(50, 140));

            if (doPickup)
            {
                // Release LMB (drop)
                InputSimulator.LeftUp();
                Console.WriteLine("Broken bait thrown away.");
                Thread.Sleep(rnd.Next(30, 90));
            }
        }
        finally
        {
            // lways release left button in case of exception
            try { InputSimulator.LeftUp(); } catch { }
        }
    }

    // Allow external cancellation (called from Program on Ctrl+C)
    public void Stop()
    {
        running = false;
    }
}