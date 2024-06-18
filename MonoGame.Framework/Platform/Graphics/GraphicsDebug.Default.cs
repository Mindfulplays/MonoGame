// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class GraphicsDebug
    {
        /// <summary>
        /// Set this to true to output internal graphics messages to the console stdout.
        /// Note: Only applicable when DEBUG is set.
        /// </summary>
        internal static bool OutputDebugMessagesToConsole { get; set; }

        /// <summary>
        /// Device specific information (graphics adapter) that can be used by the application
        /// for logging or debugging purposes.
        /// </summary>
        public static string DeviceInfo { get; set; } = "";

        /// <summary>
        /// Used to show the spammy log messages via <see cref="Spam"/>.
        /// Do not use this unless you are debugging a very specific problem.
        /// Frame-rate will reduce significantly as the messages are spewed to Console each frame.
        /// </summary>
        private const bool _shouldLogSpammyMessages = false;

        private bool PlatformTryDequeueMessage(out GraphicsDebugMessage message)
        {
            message = null;
            return false;
        }

#if DEBUG
        // Used to limit the spammy messages and prevent them from slowing the app too much in DEBUG mode.
        private record MessageCount(int Count)
        {
            public int Count { get; set; } = Count;
        }

        /// <summary>
        /// Counts the number of times a message was logged to prevent too much spamminess - especially
        /// as graphics messages may be output several times per-frame, 60 times a second which will
        /// cause a significant slowdown.
        /// </summary>
        private static readonly ConcurrentDictionary<string, MessageCount> MESSAGES_COUNTS_ = new();
#endif

        /// <summary>
        /// Logs a console message (and hence the shortform <code>C</code>).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("DEBUG")]
        public static void C(string message)
        {
#if DEBUG
            // Only output if we are in DEBUG mode and the developer intentionally required debug messages.
            if (!GraphicsDebug.OutputDebugMessagesToConsole) { return; }

            System.Console.WriteLine($"MGDBG: {message}");
#endif
        }

        /// <summary>
        /// Always writes to console - use with care and infrequently.
        /// </summary>
        public static void I(string message)
        {
            Console.WriteLine($"MGDBG: {message}");
        }

        /// <summary>
        /// Logs an error message regardless of DEBUG mode (and hence the shortform <code>E</code>).
        /// </summary>
        public static void E(string message)
        {
            if (!GraphicsDebug.OutputDebugMessagesToConsole) { return; }

            System.Console.WriteLine($"****ERROR*****:MGDBG: {message} - \n\n");
        }

        /// <summary>
        /// For certain logging such as GraphicsDebug.C(), under debug mode, the messages will be spewed
        /// to Console.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("DEBUG")]
        public static void EnableOutputDebugMessagesToConsole()
        {
#if DEBUG
            OutputDebugMessagesToConsole = true;
            MESSAGES_COUNTS_.Clear();
            _timers.Clear();
#endif
        }

        /// <summary>
        /// Logs a spammy message - if a message appears repeatedly over and over again, limit it to
        /// <param name="maxNumTimes"/>.
        ///
        /// <remarks>
        /// Remember to set <see cref="_shouldLogSpammyMessages"/> to true as the messages are
        /// very spammy and drag a debug build down to a crawl at times. Very useful to debug
        /// a specific, narrowed-down repro test using the InteractiveTests.
        /// </remarks>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("DEBUG")]
        public static void Spam(string message, int maxNumTimes = 10)
        {
#if DEBUG
            // Only output if we are in DEBUG mode and the developer intentionally required debug messages.
            if (!GraphicsDebug.OutputDebugMessagesToConsole) { return; }

            // Extra level of spam check - only turn this on when debugging specific narrowed-down repro cases.
            if (!_shouldLogSpammyMessages) { return;}

            if (MESSAGES_COUNTS_.Count >= 500) { return; }

            if (!MESSAGES_COUNTS_.TryGetValue(message, out var numTimes))
            {
                numTimes = new(0);
                MESSAGES_COUNTS_.TryAdd(message, numTimes);
            }

            if (numTimes.Count >= maxNumTimes) { return; }

            System.Console.WriteLine($"MGDBG: {message}");
            numTimes.Count++;
#endif
        }

#if DEBUG
        private class DebugTimer
        {
            internal string Name = "";
            internal long FrameCount = 0;
            internal long TimeTicks = -1;
            internal long StartTimeTicks = -1;

            internal double FrameTimeMs => TotalTimeMs / ((double)FrameCount);

            internal double TotalTimeMs => (((double)TimeTicks * 1000) / ((double)Stopwatch.Frequency));

            internal double FPS => 1000.0 / FrameTimeMs;
            public bool IsValid => TimeTicks >= 0 && FrameCount > 0;

            public void Reset()
            {
                FrameCount = 0;
                TimeTicks = -1;
                StartTimeTicks = -1;
            }
        }

        private static readonly ConcurrentDictionary<string, DebugTimer> _timers = new();
        private static readonly Stopwatch _stopWatch = Stopwatch.StartNew();
#endif
        public static string StartTimer(string name)
        {
#if DEBUG
            if (!_timers.TryGetValue(name, out var timer))
            {
                timer = new DebugTimer() { Name = name };
                _timers.TryAdd(name, timer);
            }

            if (timer.StartTimeTicks < 0) { timer.StartTimeTicks = _stopWatch.ElapsedTicks; }

            return name;
#else
            return "";
#endif
        }

        [Conditional("DEBUG")]
        public static void StopTimer(string name)
        {
#if DEBUG
            if (_timers.TryGetValue(name, out var timer))
            {
                if (timer.StartTimeTicks > 0)
                {
                    timer.TimeTicks += (_stopWatch.ElapsedTicks - timer.StartTimeTicks);
                    timer.StartTimeTicks = -1;
                    timer.FrameCount++;
                }
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void PrintTimers()
        {
#if DEBUG
            foreach (var timer in _timers.Values)
            {
                if (timer.IsValid)
                {
                    GraphicsDebug.C(
                        $"{timer.Name}: \t\t{timer.FPS:#0.00} fps\t\t total {timer.TotalTimeMs} ms \t\t" +
                        $"frame {timer.FrameTimeMs:#0.00} ms \t\t({timer.FrameCount} frames)");
                }

                timer.Reset();
            }
#endif
        }
    }
}
