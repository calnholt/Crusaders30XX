using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Crusaders30XX.ECS.Services
{
    public static class LoggingService
    {
        private static Dictionary<string, List<JsonNode>> _buffer = new();
        private static int _callCount = 0;
        private static int _frameCount = 0;
        private static int _seqCounter = 0;
        private const int FlushEveryFrames = 300;
        private const int FlushEveryNCalls = 50;
        private const string LogPath = "logs/session.json";

        private static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Initialize()
        {
            _buffer = new();
            _callCount = 0;
            _frameCount = 0;
            _seqCounter = 0;
            Directory.CreateDirectory("logs");
            File.WriteAllText(LogPath, "{}");
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Tick()
        {
            _frameCount++;
            if (_frameCount % FlushEveryFrames == 0 || _callCount >= FlushEveryNCalls)
            {
                FlushInternal();
                _callCount = 0;
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Flush()
        {
            FlushInternal();
        }

        // Not marked [Conditional] — called from within [Conditional] methods only,
        // so it is already unreachable in release builds.
        internal static void Append(string context, JsonObject entry)
        {
            entry["seq"] = ++_seqCounter;
            entry["frame"] = _frameCount;

            if (!_buffer.TryGetValue(context, out var list))
            {
                list = new List<JsonNode>();
                _buffer[context] = list;
            }
            list.Add(entry);
            _callCount++;
        }

        private static void FlushInternal()
        {
            try
            {
                string json = JsonSerializer.Serialize(_buffer, _writeOptions);
                File.WriteAllText(LogPath, json);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LoggingService] Flush failed: {ex.Message}"); }
        }
    }
}
