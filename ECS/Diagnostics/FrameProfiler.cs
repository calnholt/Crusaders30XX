using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Crusaders30XX.Diagnostics
{
    public static class FrameProfiler
    {
        private static readonly object _lock = new object();
        private static readonly Queue<Entry> _entries = new Queue<Entry>(2048);
        private static readonly Queue<double> _frameTimes = new Queue<double>(512);
        private static long _currentFrameId = 0;
        private static readonly Stopwatch _clock = Stopwatch.StartNew();
        public static double WindowSeconds { get; set; } = 1.0; // rolling window size

        private struct Entry
        {
            public string Name;
            public long Ticks;
            public double TimeSeconds;
            public long FrameId;
        }

        public struct Sample
        {
            public string Name { get; set; }
            public int Calls { get; set; }
            public double TotalMs { get; set; }
            public double AvgMs { get; set; }
            public double FrameAvgMs { get; set; }
        }

        public static void BeginFrame()
        {
            lock (_lock)
            {
                double now = _clock.Elapsed.TotalSeconds;
                _currentFrameId++;
                _frameTimes.Enqueue(now);
                PruneOld(now);
            }
        }

        public static void AddSample(string name, long elapsedTicks)
        {
            lock (_lock)
            {
                double now = _clock.Elapsed.TotalSeconds;
                _entries.Enqueue(new Entry { Name = name, Ticks = elapsedTicks, TimeSeconds = now, FrameId = _currentFrameId });
                PruneOld(now);
            }
        }

        public static void Measure(string name, Action action)
        {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            AddSample(name, sw.ElapsedTicks);
        }

        public static T Measure<T>(string name, Func<T> func)
        {
            var sw = Stopwatch.StartNew();
            T result = func();
            sw.Stop();
            AddSample(name, sw.ElapsedTicks);
            return result;
        }

        public static IDisposable Scope(string name)
        {
            return new ScopeTimer(name);
        }

        private sealed class ScopeTimer : IDisposable
        {
            private readonly string _name;
            private readonly Stopwatch _sw;

            public ScopeTimer(string name)
            {
                _name = name;
                _sw = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _sw.Stop();
                AddSample(_name, _sw.ElapsedTicks);
            }
        }

        public static List<Sample> GetTopSamples(int count)
        {
            lock (_lock)
            {
                double now = _clock.Elapsed.TotalSeconds;
                double cutoff = now - WindowSeconds;
                double tickToMs = 1000.0 / Stopwatch.Frequency;

                // Determine number of frames within the window
                int framesInWindow = 0;
                foreach (var ft in _frameTimes)
                {
                    if (ft >= cutoff) framesInWindow++;
                }
                framesInWindow = Math.Max(framesInWindow, 1);

                // Aggregate per name within the window
                var map = new Dictionary<string, (long ticks, int calls)>(64);
                foreach (var e in _entries)
                {
                    if (e.TimeSeconds < cutoff) continue;
                    if (!map.TryGetValue(e.Name, out var v)) v = (0L, 0);
                    v.ticks += e.Ticks;
                    v.calls += 1;
                    map[e.Name] = v;
                }

                return map
                    .Select(kv => new Sample
                    {
                        Name = kv.Key,
                        Calls = kv.Value.calls,
                        TotalMs = kv.Value.ticks * tickToMs,
                        AvgMs = kv.Value.calls > 0 ? (kv.Value.ticks * tickToMs) / kv.Value.calls : 0.0
                    })
                    .Select(s => new Sample
                    {
                        Name = s.Name,
                        Calls = s.Calls,
                        TotalMs = s.TotalMs,
                        AvgMs = s.AvgMs,
                        FrameAvgMs = s.TotalMs / framesInWindow
                    })
                    .OrderByDescending(s => s.FrameAvgMs)
                    .Take(count)
                    .ToList();
            }
        }

        private static void PruneOld(double now)
        {
            double pruneBefore = now - Math.Max(1.0, WindowSeconds) * 1.5; // keep a bit extra
            while (_entries.Count > 0 && _entries.Peek().TimeSeconds < pruneBefore)
            {
                _entries.Dequeue();
            }
            while (_frameTimes.Count > 0 && _frameTimes.Peek() < pruneBefore)
            {
                _frameTimes.Dequeue();
            }
        }
    }
}
