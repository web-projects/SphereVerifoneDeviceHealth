using System;
using System.Text;
using System.Threading;

namespace Common.Execution
{
    public class ProgressBar : IDisposable, IProgress<double>
    {
        private readonly int blockCount = Console.WindowWidth - 11;
        private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string animation = @"|/-\";

        private readonly Timer timer;

        private double barProgress = 0;
        private double currentProgress = 0;
        private string currentText = string.Empty;
        private bool disposed = false;
        private int animationIndex = 0;

        public ProgressBar()
        {
            barProgress = 0;
            timer = new Timer(TimerHandler);

            // If the console output is redirected to a file, draw nothing.
            if (!Console.IsOutputRedirected)
            {
                ResetTimer();
            }
        }

        public void Report(double value)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref currentProgress, value);
        }

        public void UpdateBar()
        {
            barProgress += 0.10;
            barProgress = Math.Max(0, Math.Min(1, barProgress));
            Interlocked.Exchange(ref currentProgress, barProgress);
            // Reset progress indication
            if (barProgress == 1)
            {
                barProgress = 0;
            }
        }

        private void TimerHandler(object state)
        {
            lock (timer)
            {
                if (disposed) return;

                int progressBlockCount = (int)(currentProgress * blockCount);
                int percent = (int)(currentProgress * 100);

                //string text = string.Format("[{0}{1}] {2,3}% {3}",
                //    new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount), percent, animation[animationIndex++ % animation.Length]);
                string text = string.Format("[{0}] {1}",
                    new string('#', progressBlockCount),
                    animation[animationIndex++ % animation.Length]);

                UpdateText(text);

                ResetTimer();
            }
        }

        private void UpdateText(string text)
        {
            int cursorTop = Console.CursorTop;
            int cursorLeft = Console.CursorLeft;

            Console.SetCursorPosition(1, Console.WindowHeight - 1);

            // Get length of common portion
            int commonPrefixLength = 0;
            int commonLength = Math.Min(currentText.Length, text.Length);

            while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            // Backtrack to the first differing character
            StringBuilder outputBuilder = new StringBuilder();
            outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.Substring(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            int overlapCount = currentText.Length - text.Length;
            if (overlapCount > 0)
            {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.Write(outputBuilder);
            currentText = text;

            Console.SetCursorPosition(cursorLeft, cursorTop);
        }

        private void ResetTimer()
        {
            timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose()
        {
            lock (timer)
            {
                disposed = true;
                UpdateText(string.Empty);
            }
        }
    }
}