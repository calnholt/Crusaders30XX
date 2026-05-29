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
        private static readonly Queue<FrameMarker> _frames = new Queue<FrameMarker>(512);
        private static long _currentFrameId = 0;

        private struct FrameMarker
        {
            public double TimeSeconds;
            public long FrameId;
        }
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
                _frames.Enqueue(new FrameMarker { TimeSeconds = now, FrameId = _currentFrameId });
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

                int framesInWindow = 0;
                foreach (var f in _frames)
                {
                    if (f.TimeSeconds >= cutoff) framesInWindow++;
                }
                framesInWindow = Math.Max(framesInWindow, 1);

                // Sum ticks per name per frame, then average across draw frames in the window.
                var perNamePerFrame = new Dictionary<string, Dictionary<long, (long ticks, int calls)>>(64);
                foreach (var e in _entries)
                {
                    if (e.TimeSeconds < cutoff) continue;
                    if (!perNamePerFrame.TryGetValue(e.Name, out var byFrame))
                    {
                        byFrame = new Dictionary<long, (long ticks, int calls)>(8);
                        perNamePerFrame[e.Name] = byFrame;
                    }
                    if (!byFrame.TryGetValue(e.FrameId, out var frameTotal)) frameTotal = (0L, 0);
                    frameTotal.ticks += e.Ticks;
                    frameTotal.calls += 1;
                    byFrame[e.FrameId] = frameTotal;
                }

                return perNamePerFrame
                    .Select(kv =>
                    {
                        long windowTicks = 0L;
                        int calls = 0;
                        foreach (var frame in kv.Value.Values)
                        {
                            windowTicks += frame.ticks;
                            calls += frame.calls;
                        }
                        double totalMs = windowTicks * tickToMs;
                        return new Sample
                        {
                            Name = kv.Key,
                            Calls = calls,
                            TotalMs = totalMs,
                            AvgMs = calls > 0 ? totalMs / calls : 0.0,
                            FrameAvgMs = totalMs / framesInWindow
                        };
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
            while (_frames.Count > 0 && _frames.Peek().TimeSeconds < pruneBefore)
            {
                _frames.Dequeue();
            }
        }
    }
}
