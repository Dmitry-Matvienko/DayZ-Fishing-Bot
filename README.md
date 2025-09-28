# DayZ Fishing Bot — Unofficial (C# with OpenCvSharp)
# This project is unofficial and not affiliated with Bohemia Interactive. Use at your own risk. The author is not responsible for bans.

## What the bot does

- Automatically “catches” fish: holds down the left mouse button, then opens the inventory, searches for templates of fish on the screen, selects an item, checks the condition of the fishing rod, hooks the bait if necessary, and continues the cycle.
- Mouse movements are simulated smoothly **(Bezier + jitter)** and timings are __randomized__ to **mimic human behavior**.

## Features

- **Template Matching:** Detects fish and items using image templates.
- **Automatic Fishing:** Simulates mouse actions to catch fish repeatedly.
- **Rod & Bait Handling:** Checks rod status and automatically re-baits if needed.
- **Configurable Parameters:** All timings, thresholds, and template paths can be configured via `config.json`.
- **Human-like Movements:** Smooth Bezier curve cursor movements with random jitter and randomized actions

## Quick Start Guide (for users)

1. Download the release from GitHub → **Releases** → select the archive `dayz-fishing-bot-win-x64.zip`.
2. Unzip it into any folder.
3. Edit `config.json` if necessary. See `CONFIG_EXPLAIN.txt` to read about each parameter.
4. There are `templates/` and `general/` folders. 
	- **Replace the screenshots there** if necessary.
	- Add new screenshots in `templates/` folder if the server uses other fish. The name of the screenshot of fish does not matter. Name of screenshots of `general/` are important.
5. Change the inventory button to `Mouse button 4` instead of `Tab`.
6. Run `DayzFishingBot.exe`. Make sure the game window is active on the main monitor and the cursor is visible in it.
7. To stop, press `Ctrl+C` in the console or close the console window.
8. If you’re experiencing problems, see below `Important` and `Known issues and solutions`

## Important (Graphics / Settings / Pre-flight checks)  
  
Before starting, make sure:

- **Game graphics settings:** the bot is very sensetive to screen resolution. You should have `1920x1080x32` and screen scalability `on your PC = 100%` (usually the default setting). Preferable: windowed/borderless mode (Borderless Window) or “Windowed” — this makes it easier to take screenshots.
- **Mouse/bindings:** the bot uses specific keystrokes (`Mouse button 4`). Make sure that this button are assigned as expected by the bot.
- **Run as administrator:** sometimes needed if the game is running with administrator privileges.
- **Screen overlap mode:** the bot works with the main monitor. Make sure the game window is visible on the main screen.

## Known issues and solutions

**Templates have a small "score"/false positives**  
  
Make sure that:
- The template is a screenshot from the same graphics configuration (resolution, UI scale).
  - `config.json` has an right `MatchThreshold`(0.82) (decrease it if the bot cannot find anythingю. Increase it if there are false positives).
  - When changing the game interface or resolution/quality screen, **you need to reshoot the templates**.

**Inventory does not open automatically**
- Check that in the game settings, `Inventory` is bound to the same button that the bot uses (`Mouse4` instead of `Tab`).
- If the inventory opens manually but not with the bot, try running it as an administrator and/or setting the game to windowed/frameless mode.

**Errors due to missing DLLs (vcruntime140.dll, etc.)**
- Install `Microsoft Visual C++ Redistributable (x86/x64)` from the Microsoft website.

**The bot doesn't press LMB**  
  
In rare cases, the bot may not click the left mouse button. To resolve this issue:
- Restart `DayzFishingBot.exe`
- Most often, this is due to the fishing rod being in a “heavily damaged” state. Replace the fishing rod with a new one.

## Usage

- **Stopping:** Press `Ctrl+C` in the console window to stop the bot gracefully.
- **Viewing Logs:** The console shows status messages for each cycle (e.g. fish found, actions performed).
- **Adjusting Settings:** Edit `config.json` to fine-tune behavior. For example, increase `MatchThreshold` if the bot misses fish or lower it if false matches occur.

## Setup for developers

1. **Requirements:** .NET 8.0 (or later) runtime installed. OpenCvSharp library is required (add via NuGet).
2. **Templates Folders:** Create two folders in the application directory:
   - `templates/` – Put all fish/item template images here (`.png`, `.jpg`, etc.).
   - `general/` – Put general templates here (rod/bait states and specific bait image).
3. **Configuration:** On first run, the bot will generate a `config.json` file with default settings. You can edit this file to adjust thresholds, delays, and template names.
4. **Run the Bot:** Launch the application while the game is running and visible on the primary monitor. The bot starts after a 5-second delay (see console output).