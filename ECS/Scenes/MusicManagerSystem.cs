using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Centralized background music manager. Listens for ChangeMusicTrack events
    /// and plays the corresponding Song. Uses MediaPlayer for playback.
    /// </summary>
    [DebugTab("Music Manager")]
    public class MusicManagerSystem : Core.System
    {
        private readonly ContentManager _content;
        private readonly Dictionary<MusicTrack, Song> _songCache = new();
        private MusicTrack _current = MusicTrack.None;
        private float _targetVolume = 0.2f;
        private bool _fading = false;
        private float _fadeElapsed = 0f;
        private float _fadeDuration = 0.5f;
        private float _startVolume = 0.2f;
        private Song _pendingSong;
        private bool _pendingLoop;
        private float _expectedVolume = 0.2f;
        [DebugEditable(DisplayName = "Mute")]
        public bool Mute { get; set; } = false;

        public MusicManagerSystem(EntityManager entityManager, ContentManager content) : base(entityManager)
        {
            _content = content;
            EventManager.Subscribe<ChangeMusicTrack>(OnChangeMusicTrack);
            EventManager.Subscribe<StopMusic>(OnStopMusic);
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = _targetVolume;
            _expectedVolume = _targetVolume;
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return Enumerable.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Mute) return;
            
            // Guard against external volume modification (e.g. platform/driver quirks)
            if (!_fading && MediaPlayer.State == MediaState.Playing)
            {
                if (Math.Abs(MediaPlayer.Volume - _expectedVolume) > 0.001f)
                {
                    MediaPlayer.Volume = _expectedVolume;
                }
            }
            
            if (_fading)
            {
                _fadeElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
                float t = Math.Clamp(_fadeElapsed / Math.Max(0.0001f, _fadeDuration), 0f, 1f);
                // Two-phase fade: first fade-out current to 0, then swap and fade-in
                if (_pendingSong != null)
                {
                    // Fade out
                    MediaPlayer.Volume = MathHelper.Lerp(_startVolume, 0f, t);
                    _expectedVolume = MediaPlayer.Volume;
                    if (t >= 1f)
                    {
                        // Swap to pending and start fade-in
                        MediaPlayer.Stop();
                        MediaPlayer.IsRepeating = _pendingLoop;
                        MediaPlayer.Play(_pendingSong);
                        _current = _songCache.FirstOrDefault(kv => kv.Value == _pendingSong).Key;
                        _startVolume = 0f;
                        _fadeElapsed = 0f;
                        _pendingSong = null;
                    }
                }
                else
                {
                    // Fade to target (supports volume-only fades when staying on same track)
                    MediaPlayer.Volume = MathHelper.Lerp(_startVolume, _targetVolume, t);
                    _expectedVolume = MediaPlayer.Volume;
                    if (t >= 1f)
                    {
                        _fading = false;
                        MediaPlayer.Volume = _targetVolume;
                        _expectedVolume = _targetVolume;
                        // If target volume is 0 (stop requested), stop playback now
                        if (_targetVolume <= 0f)
                        {
                            MediaPlayer.Stop();
                            _current = MusicTrack.None;
                        }
                    }
                }
            }
        }

        private void OnChangeMusicTrack(ChangeMusicTrack evt)
        {
            try
            {
                _targetVolume = MathHelper.Clamp(evt?.Volume ?? 0.5f, 0f, 1f);
                bool loop = evt?.Loop ?? true;
                var track = evt?.Track ?? MusicTrack.None;

                if (track == _current && MediaPlayer.State == MediaState.Playing)
                {
                    MediaPlayer.IsRepeating = loop;
                    // Support fading volume when staying on the same track
                    if (evt?.Fade == true)
                    {
                        _fadeDuration = Math.Max(0.01f, evt.FadeSeconds);
                        _fadeElapsed = 0f;
                        _startVolume = MediaPlayer.Volume;
                        _pendingSong = null; // volume-only fade
                        _fading = true;
                    }
                    else
                    {
                        _fading = false;
                        _pendingSong = null;
                        MediaPlayer.Volume = _targetVolume;
                        _expectedVolume = _targetVolume;
                    }
                    return;
                }

                var song = ResolveSong(track);
                if (song == null)
                {
                    MediaPlayer.Stop();
                    _current = MusicTrack.None;
                    return;
                }

                if (evt?.Fade == true)
                {
                    // Setup fade
                    _fadeDuration = Math.Max(0.01f, evt.FadeSeconds);
                    _fadeElapsed = 0f;
                    _startVolume = MediaPlayer.Volume;
                    _pendingSong = song;
                    _pendingLoop = loop;
                    _fading = true;
                }
                else
                {
                    MediaPlayer.Stop();
                    MediaPlayer.IsRepeating = loop;
                    MediaPlayer.Volume = _targetVolume;
                    _expectedVolume = _targetVolume;
                    MediaPlayer.Play(song);
                    _current = track;
                }
            }
            catch { }
        }

        private Song ResolveSong(MusicTrack track)
        {
            if (track == MusicTrack.None) return null;
            if (_songCache.TryGetValue(track, out var cached) && cached != null) return cached;
            string assetName = track switch
            {
                MusicTrack.Menu => "Music/clash_of_shadows", // .ogg in Content
                MusicTrack.Battle => "Music/desert_theme_battle",
                MusicTrack.Customize => "Music/customize",
                MusicTrack.Map => "Music/desert_map",
                MusicTrack.QuestComplete => "Music/quest_complete",
                _ => null
            };
            if (string.IsNullOrEmpty(assetName)) return null;
            try
            {
                var song = _content.Load<Song>(assetName);
                _songCache[track] = song;
                return song;
            }
            catch
            {
                return null;
            }
        }

        private void OnStopMusic(StopMusic evt)
        {
            try
            {
                if (evt?.Fade == true)
                {
                    // Fade out current, then stop
                    _pendingSong = null; // signal fade-out only
                    _fadeDuration = Math.Max(0.01f, evt.FadeSeconds);
                    _fadeElapsed = 0f;
                    _startVolume = MediaPlayer.Volume;
                    _fading = true;
                    // When Update completes fade-out branch without pending, finalize stop
                    // We piggy-back on the fade-in branch completion by detecting target 0 volume
                    // Ensure target is 0 for fade-out only
                    _targetVolume = 0f;
                }
                else
                {
                    MediaPlayer.Stop();
                    _current = MusicTrack.None;
                }
            }
            catch { }
        }
    }
}


