using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.Diagnostics
{
    public static class FrameProfiler
    {
        public enum SampleKind
        {
            Leaf,
            Inclusive
        }

        private static readonly object _lock = new object();
        private static readonly Queue<Entry> _entries = new Queue<Entry>(2048);
        private static readonly Queue<FrameMarker> _frames = new Queue<FrameMarker>(512);
        private static long _currentFrameId = 0;
        private static bool _gameFrameStarted;

        private struct FrameMarker
        {
            public double TimeSeconds;
            public long FrameId;
        }

        private static readonly Stopwatch _clock = Stopwatch.StartNew();
        public static double WindowSeconds { get; set; } = 1.0;

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

        public struct SystemStats
        {
            public string Name;
            public double MinMs;
            public double MaxMs;
            public double AvgMs;
            public double P95Ms;
            public int SlowFrames;
            public int Frames;
        }

        public struct FrameSummaryStats
        {
            public double MinMs;
            public double MaxMs;
            public double AvgMs;
            public int Frames;
        }

        private static readonly Dictionary<string, long> _currentFrameTotals = new Dictionary<string, long>(64);
        private static long _currentFrameLeafTicks;

#if DEBUG
        private const double SlowFrameThresholdMs = 16.67;
        private const int MinSceneFrames = 60;
        private static readonly string[] DrawBreakdownScopes =
        {
            "Game1.Draw.SceneSetupAndDrawScene",
            "Game1.Draw.ShaderComposite",
            "Game1.Draw.Present"
        };

        private sealed class SessionAccumulator
        {
            public double MinMs;
            public double MaxMs;
            public double SumMs;
            public int Frames;
            public readonly List<double> Samples = new List<double>(64);

            public void Add(double frameMs)
            {
                Samples.Add(frameMs);
                if (Frames == 0)
                {
                    MinMs = frameMs;
                    MaxMs = frameMs;
                    SumMs = frameMs;
                    Frames = 1;
                    return;
                }

                if (frameMs < MinMs) MinMs = frameMs;
                if (frameMs > MaxMs) MaxMs = frameMs;
                SumMs += frameMs;
                Frames++;
            }
        }

        private static readonly Dictionary<string, SessionAccumulator> _sessionStats = new Dictionary<string, SessionAccumulator>(128);
        private static SessionAccumulator _frameTimeStats = new SessionAccumulator();
        private static SessionAccumulator _unaccountedStats = new SessionAccumulator();
        private static readonly Dictionary<SceneId, int> _sceneFrameCounts = new Dictionary<SceneId, int>();
        private static readonly Dictionary<SceneId, Dictionary<string, SessionAccumulator>> _sceneStats = new Dictionary<SceneId, Dictionary<string, SessionAccumulator>>();
        private static readonly Dictionary<SceneId, SessionAccumulator> _sceneFrameTimeStats = new Dictionary<SceneId, SessionAccumulator>();
        private static readonly Dictionary<SceneId, SessionAccumulator> _sceneUnaccountedStats = new Dictionary<SceneId, SessionAccumulator>();
        private static SceneId _activeSceneId = SceneId.None;
        private static int _profiledFrameCount;
        private static double _closingFrameTotalMs;
        private static bool _snapshotSkippedSession;
#endif

        private static double TickToMs => 1000.0 / Stopwatch.Frequency;

        public static void BeginGameFrame(GameTime gameTime, bool skipSessionAccumulation = false)
        {
            lock (_lock)
            {
                double now = _clock.Elapsed.TotalSeconds;
                double frameTotalMs = gameTime.ElapsedGameTime.TotalMilliseconds;

#if DEBUG
                if (_gameFrameStarted && !skipSessionAccumulation && !_snapshotSkippedSession)
                {
                    double totalForCompletedFrame = _closingFrameTotalMs > 0 ? _closingFrameTotalMs : frameTotalMs;
                    CommitCurrentFrameTotalsLocked(totalForCompletedFrame);
                }

                _snapshotSkippedSession = skipSessionAccumulation;
                _closingFrameTotalMs = 0;
#endif

                if (_gameFrameStarted)
                {
                    _currentFrameId++;
                }
                else
                {
                    _gameFrameStarted = true;
                    _currentFrameId++;
                }

                _frames.Enqueue(new FrameMarker { TimeSeconds = now, FrameId = _currentFrameId });
                _currentFrameTotals.Clear();
                _currentFrameLeafTicks = 0L;
                PruneOld(now);
            }
        }

        public static void EndGameFrame(GameTime gameTime)
        {
#if DEBUG
            _closingFrameTotalMs = gameTime.ElapsedGameTime.TotalMilliseconds;
#endif
        }

#if DEBUG
        public static void SetActiveScene(SceneId sceneId)
        {
            _activeSceneId = sceneId;
        }
#endif

        [Obsolete("Use BeginGameFrame at the start of Game1.Update.")]
        public static void BeginFrame()
        {
            lock (_lock)
            {
                double now = _clock.Elapsed.TotalSeconds;
                _currentFrameId++;
                _frames.Enqueue(new FrameMarker { TimeSeconds = now, FrameId = _currentFrameId });
                _currentFrameTotals.Clear();
                _currentFrameLeafTicks = 0L;
                PruneOld(now);
            }
        }

        public static void AddSample(string name, long elapsedTicks, SampleKind kind = SampleKind.Leaf)
        {
            lock (_lock)
            {
                double now = _clock.Elapsed.TotalSeconds;
                _entries.Enqueue(new Entry { Name = name, Ticks = elapsedTicks, TimeSeconds = now, FrameId = _currentFrameId });
                if (!_currentFrameTotals.TryGetValue(name, out var frameTotal)) frameTotal = 0L;
                frameTotal += elapsedTicks;
                _currentFrameTotals[name] = frameTotal;
                if (kind == SampleKind.Leaf)
                {
                    _currentFrameLeafTicks += elapsedTicks;
                }
                PruneOld(now);
            }
        }

        public static void Measure(string name, Action action)
        {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            AddSample(name, sw.ElapsedTicks, SampleKind.Leaf);
        }

        public static T Measure<T>(string name, Func<T> func)
        {
            var sw = Stopwatch.StartNew();
            T result = func();
            sw.Stop();
            AddSample(name, sw.ElapsedTicks, SampleKind.Leaf);
            return result;
        }

        [Conditional("DEBUG")]
        public static void MeasureInclusive(string name, Action action)
        {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            AddSample(name, sw.ElapsedTicks, SampleKind.Inclusive);
        }

        public static IDisposable Scope(string name)
        {
            return new ScopeTimer(name, SampleKind.Leaf);
        }

        private sealed class ScopeTimer : IDisposable
        {
            private readonly string _name;
            private readonly SampleKind _kind;
            private readonly Stopwatch _sw;

            public ScopeTimer(string name, SampleKind kind)
            {
                _name = name;
                _kind = kind;
                _sw = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _sw.Stop();
                AddSample(_name, _sw.ElapsedTicks, _kind);
            }
        }

#if DEBUG
        private static Dictionary<string, SessionAccumulator> GetSceneScopeMap(SceneId sceneId)
        {
            if (!_sceneStats.TryGetValue(sceneId, out var map))
            {
                map = new Dictionary<string, SessionAccumulator>(64);
                _sceneStats[sceneId] = map;
            }
            return map;
        }

        private static SessionAccumulator GetSceneFrameTimeStats(SceneId sceneId)
        {
            if (!_sceneFrameTimeStats.TryGetValue(sceneId, out var acc))
            {
                acc = new SessionAccumulator();
                _sceneFrameTimeStats[sceneId] = acc;
            }
            return acc;
        }

        private static SessionAccumulator GetSceneUnaccountedStats(SceneId sceneId)
        {
            if (!_sceneUnaccountedStats.TryGetValue(sceneId, out var acc))
            {
                acc = new SessionAccumulator();
                _sceneUnaccountedStats[sceneId] = acc;
            }
            return acc;
        }

        private static void CommitCurrentFrameTotalsLocked(double totalFrameMs)
        {
            double tickToMs = TickToMs;
            SceneId sceneId = _activeSceneId;

            if (!_sceneFrameCounts.TryGetValue(sceneId, out var sceneFrames)) sceneFrames = 0;
            _sceneFrameCounts[sceneId] = sceneFrames + 1;

            var sceneScopeMap = GetSceneScopeMap(sceneId);
            var sceneFrameTime = GetSceneFrameTimeStats(sceneId);
            var sceneUnaccounted = GetSceneUnaccountedStats(sceneId);

            foreach (var kv in _currentFrameTotals)
            {
                if (kv.Value <= 0) continue;
                double frameMs = kv.Value * tickToMs;
                AccumulateSession(_sessionStats, kv.Key, frameMs);
                AccumulateSession(sceneScopeMap, kv.Key, frameMs);
            }

            _frameTimeStats.Add(totalFrameMs);
            sceneFrameTime.Add(totalFrameMs);

            double leafMs = _currentFrameLeafTicks * tickToMs;
            double unaccountedMs = Math.Max(0.0, totalFrameMs - leafMs);
            _unaccountedStats.Add(unaccountedMs);
            sceneUnaccounted.Add(unaccountedMs);
            _profiledFrameCount++;
        }

        private static void AccumulateSession(Dictionary<string, SessionAccumulator> map, string name, double frameMs)
        {
            if (!map.TryGetValue(name, out var acc))
            {
                acc = new SessionAccumulator();
                map[name] = acc;
            }
            acc.Add(frameMs);
        }

        private static SystemStats ToSystemStats(string name, SessionAccumulator acc)
        {
            return new SystemStats
            {
                Name = name,
                MinMs = acc.MinMs,
                MaxMs = acc.MaxMs,
                AvgMs = acc.Frames > 0 ? acc.SumMs / acc.Frames : 0,
                P95Ms = ComputePercentile(acc.Samples, 0.95),
                SlowFrames = CountSlowFrames(acc.Samples),
                Frames = acc.Frames
            };
        }

        private static List<SystemStats> BuildSystemStatsList(Dictionary<string, SessionAccumulator> map, double minAvgMs)
        {
            return map
                .Select(kv => ToSystemStats(kv.Key, kv.Value))
                .Where(s => s.AvgMs >= minAvgMs)
                .OrderByDescending(s => s.AvgMs)
                .ToList();
        }

        private static int CountSlowFrames(List<double> samples)
        {
            int count = 0;
            foreach (var ms in samples)
            {
                if (ms > SlowFrameThresholdMs) count++;
            }
            return count;
        }

        /// <summary>Nearest-rank percentile (ceil(p*n) - 1, clamped).</summary>
        private static double ComputePercentile(List<double> samples, double p)
        {
            if (samples == null || samples.Count == 0) return 0;
            var sorted = samples.ToArray();
            Array.Sort(sorted);
            int index = (int)Math.Ceiling(p * sorted.Length) - 1;
            if (index < 0) index = 0;
            if (index >= sorted.Length) index = sorted.Length - 1;
            return sorted[index];
        }

        private static FrameSummaryStats ToSummary(SessionAccumulator acc)
        {
            if (acc.Frames == 0)
            {
                return new FrameSummaryStats();
            }

            return new FrameSummaryStats
            {
                MinMs = acc.MinMs,
                MaxMs = acc.MaxMs,
                AvgMs = acc.SumMs / acc.Frames,
                Frames = acc.Frames
            };
        }

        [Conditional("DEBUG")]
        public static void WriteReport(string path, SceneId sceneAtQuit, bool shadersEnabled)
        {
            lock (_lock)
            {
                if (_gameFrameStarted && !_snapshotSkippedSession && _currentFrameTotals.Count > 0)
                {
                    double totalForCompletedFrame = _closingFrameTotalMs > 0 ? _closingFrameTotalMs : 0;
                    CommitCurrentFrameTotalsLocked(totalForCompletedFrame);
                    _currentFrameTotals.Clear();
                    _currentFrameLeafTicks = 0L;
                }
            }

            const double minReportAvgMs = 0.01;
            var globalSystems = BuildSystemStatsList(_sessionStats, minReportAvgMs);
            var updateRows = globalSystems.Where(s => s.Name.EndsWith(".Update", StringComparison.Ordinal)).ToList();
            var drawRows = globalSystems.Where(s => !s.Name.EndsWith(".Update", StringComparison.Ordinal)).ToList();

            var sb = new StringBuilder(8192);
            sb.AppendLine("Crusaders30XX Performance Report");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("Note: CPU time in instrumented scopes only (not GPU). P95 uses nearest-rank on per-active-frame samples.");
            sb.AppendLine();

            AppendSessionFooter(sb, sceneAtQuit, shadersEnabled);
            sb.AppendLine();

            sb.AppendLine("=== Frame (global) ===");
            AppendSummaryLine(sb, "Total frame (ms)", ToSummary(_frameTimeStats));
            AppendSummaryLine(sb, "Unaccounted (ms)", ToSummary(_unaccountedStats));
            sb.AppendLine("  Unaccounted = total frame minus leaf scopes (excludes inclusive parents).");
            AppendDrawBreakdown(sb);
            sb.AppendLine();

            AppendGlobalSystemSections(sb, updateRows, drawRows);
            AppendSpikeHotspots(sb, globalSystems);
            AppendPerSceneSections(sb, minReportAvgMs);

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "logs");
            File.WriteAllText(path, sb.ToString());
        }

        private static void AppendSessionFooter(StringBuilder sb, SceneId sceneAtQuit, bool shadersEnabled)
        {
            sb.AppendLine("=== Session ===");
            sb.AppendLine($"Scene at quit: {sceneAtQuit}");
            sb.AppendLine($"Profiled game frames: {_profiledFrameCount}");
            sb.AppendLine($"Shaders enabled: {(shadersEnabled ? "yes" : "no")}");
            sb.AppendLine("Frame counts by scene:");
            foreach (SceneId sceneId in Enum.GetValues(typeof(SceneId)))
            {
                _sceneFrameCounts.TryGetValue(sceneId, out int count);
                sb.AppendLine($"  {sceneId}: {count}");
            }
        }

        private static void AppendDrawBreakdown(StringBuilder sb)
        {
            sb.AppendLine("Draw breakdown (leaf):");
            foreach (var scopeName in DrawBreakdownScopes)
            {
                if (!_sessionStats.TryGetValue(scopeName, out var acc) || acc.Frames == 0)
                {
                    sb.AppendLine($"  {scopeName,-40} (no samples)");
                    continue;
                }
                var stats = ToSystemStats(scopeName, acc);
                sb.AppendLine($"  {scopeName,-40} min={stats.MinMs,6:0.00}  max={stats.MaxMs,6:0.00}  avg={stats.AvgMs,6:0.00}");
            }
            sb.AppendLine("  Remaining unaccounted includes Update phase and time outside these Draw leaf scopes.");
        }

        private static void AppendGlobalSystemSections(StringBuilder sb, List<SystemStats> updateRows, List<SystemStats> drawRows)
        {
            sb.AppendLine("=== Update (global, ms per active frame) ===");
            AppendSystemTable(sb, updateRows);
            sb.AppendLine();

            sb.AppendLine("=== Draw (global, ms per active frame) ===");
            AppendSystemTable(sb, drawRows);
            sb.AppendLine();
        }

        private static void AppendSpikeHotspots(StringBuilder sb, List<SystemStats> allRows)
        {
            var spikes = allRows
                .Where(s => s.SlowFrames > 0)
                .OrderByDescending(s => s.SlowFrames)
                .ThenByDescending(s => s.P95Ms)
                .Take(10)
                .ToList();
            if (spikes.Count == 0) return;

            sb.AppendLine($"=== Spike hotspots (global, slow > {SlowFrameThresholdMs:0.00} ms) ===");
            AppendSystemTable(sb, spikes);
            sb.AppendLine();
        }

        private static void AppendPerSceneSections(StringBuilder sb, double minReportAvgMs)
        {
            foreach (SceneId sceneId in Enum.GetValues(typeof(SceneId)))
            {
                if (!_sceneFrameCounts.TryGetValue(sceneId, out int frameCount) || frameCount < MinSceneFrames)
                {
                    continue;
                }

                if (!_sceneStats.TryGetValue(sceneId, out var scopeMap))
                {
                    continue;
                }

                var sceneSystems = BuildSystemStatsList(scopeMap, minReportAvgMs);
                var sceneUpdate = sceneSystems.Where(s => s.Name.EndsWith(".Update", StringComparison.Ordinal)).ToList();
                var sceneDraw = sceneSystems.Where(s => !s.Name.EndsWith(".Update", StringComparison.Ordinal)).ToList();

                sb.AppendLine($"=== Scene: {sceneId} ({frameCount} frames) ===");
                sb.AppendLine($"=== Frame ({sceneId}) ===");
                _sceneFrameTimeStats.TryGetValue(sceneId, out var frameTime);
                _sceneUnaccountedStats.TryGetValue(sceneId, out var unaccounted);
                AppendSummaryLine(sb, "Total frame (ms)", ToSummary(frameTime ?? new SessionAccumulator()));
                AppendSummaryLine(sb, "Unaccounted (ms)", ToSummary(unaccounted ?? new SessionAccumulator()));
                sb.AppendLine();

                sb.AppendLine($"=== Update ({sceneId}) ===");
                AppendSystemTable(sb, sceneUpdate);
                sb.AppendLine();

                sb.AppendLine($"=== Draw ({sceneId}) ===");
                AppendSystemTable(sb, sceneDraw);
                sb.AppendLine();
            }
        }

        private static void AppendSummaryLine(StringBuilder sb, string label, FrameSummaryStats stats)
        {
            if (stats.Frames == 0)
            {
                sb.AppendLine($"{label,-22} min= n/a   max= n/a   avg= n/a");
                return;
            }

            sb.AppendLine($"{label,-22} min={stats.MinMs,6:0.00}  max={stats.MaxMs,6:0.00}  avg={stats.AvgMs,6:0.00}");
        }

        private static void AppendSystemTable(StringBuilder sb, List<SystemStats> rows)
        {
            sb.AppendLine($"{"Name",-40} {"Min",8} {"Max",8} {"Avg",8} {"P95",8} {"Slow",6} {"Frames",8}");
            sb.AppendLine(new string('-', 92));
            foreach (var s in rows)
            {
                sb.AppendLine($"{s.Name,-40} {s.MinMs,8:0.00} {s.MaxMs,8:0.00} {s.AvgMs,8:0.00} {s.P95Ms,8:0.00} {s.SlowFrames,6} {s.Frames,8}");
            }
        }
#endif

        public static List<Sample> GetTopSamples(int count)
        {
            lock (_lock)
            {
                double now = _clock.Elapsed.TotalSeconds;
                double cutoff = now - WindowSeconds;
                double tickToMs = TickToMs;

                int framesInWindow = 0;
                foreach (var f in _frames)
                {
                    if (f.TimeSeconds >= cutoff) framesInWindow++;
                }
                framesInWindow = Math.Max(framesInWindow, 1);

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
            double pruneBefore = now - Math.Max(1.0, WindowSeconds) * 1.5;
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
