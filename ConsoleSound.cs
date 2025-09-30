namespace Dayz_Fishing_Bot
{
    public enum SoundType
    {
        Start,
        Warn,
        Error,
        Exit
    }
    public class ConsoleSound
    {
        public static void PlaySound(SoundType type, bool runInBackground = true)
        {
            if (runInBackground)
                _ = Task.Run(() => PlaySoundSync(type));
            else
                PlaySoundSync(type);
        }

        private static void PlaySoundSync(SoundType type)
        {
            switch (type)
            {
                case SoundType.Start:
                    PlayToneSafe(300, 300);
                    Thread.Sleep(40);
                    PlayToneSafe(700, 140);
                    Thread.Sleep(40);
                    PlayToneSafe(900, 200);
                    break;

                case SoundType.Warn:
                    PlayToneSafe(600, 70);
                    PlayToneSafe(600, 70);
                    PlayToneSafe(600, 70);
                    PlayToneSafe(600, 200);
                    break;

                case SoundType.Error:
                    PlayToneSafe(500, 80);
                    PlayToneSafe(400, 80);
                    PlayToneSafe(300, 80);
                    PlayToneSafe(200, 200);
                    break;

                case SoundType.Exit:
                    PlayToneSafe(1400, 120);
                    Thread.Sleep(30);
                    PlayToneSafe(1000, 200);
                    break;
            }
        }

        private static void PlayToneSafe(int frequency, int durationMs)
        {
            try
            {
                // to avoid ArgumentOutOfRangeException
                if (frequency < 37) frequency = 37;
                if (frequency > 32767) frequency = 32767;
                if (durationMs < 1) durationMs = 1;

                Console.Beep(frequency, durationMs);
            }
            catch (Exception)
            {
                Console.WriteLine("Problem with play sound");
            }
        }
    }
}
