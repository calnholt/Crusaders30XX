using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Centralized sound effect manager. Listens for PlaySfxEvent
    /// and plays the corresponding SoundEffect. Supports multiple simultaneous instances.
    /// </summary>
    [DebugTab("Sound Effects")]
    public class SoundEffectManagerSystem : Core.System
    {
        private readonly ContentManager _content;
        private readonly Dictionary<SfxTrack, SoundEffect> _soundCache = new();
        private readonly List<SoundEffectInstance> _activeInstances = new();
        [DebugEditable(DisplayName = "Mute")]
        public bool Mute { get; set; } = false;

        public SoundEffectManagerSystem(EntityManager entityManager, ContentManager content) : base(entityManager)
        {
            _content = content;
            EventManager.Subscribe<PlaySfxEvent>(OnPlaySfx);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return Enumerable.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            // Clean up stopped instances
            for (int i = _activeInstances.Count - 1; i >= 0; i--)
            {
                var instance = _activeInstances[i];
                if (instance.State == SoundState.Stopped)
                {
                    instance.Dispose();
                    _activeInstances.RemoveAt(i);
                }
            }
        }

        private void OnPlaySfx(PlaySfxEvent evt)
        {
            if (Mute) return;
            if (evt == null) return;
            
            try
            {
                var track = evt.Track;
                if (track == SfxTrack.None) return;

                var soundEffect = ResolveSoundEffect(track);
                if (soundEffect == null) return;

                // Create and configure instance
                var instance = soundEffect.CreateInstance();
                instance.Volume = MathHelper.Clamp(evt.Volume, 0f, 1f);
                instance.Pitch = MathHelper.Clamp(evt.Pitch, -1f, 1f);
                instance.Pan = MathHelper.Clamp(evt.Pan, -1f, 1f);
                
                instance.Play();
                _activeInstances.Add(instance);
            }
            catch { }
        }

        private SoundEffect ResolveSoundEffect(SfxTrack track)
        {
            if (track == SfxTrack.None) return null;
            if (_soundCache.TryGetValue(track, out var cached) && cached != null) return cached;
            
            string assetName = track switch
            {
                SfxTrack.SwordAttack => "SFX/Sword Attack 2",
                SfxTrack.SwordImpact => "SFX/Sword Impact Hit 2",
                SfxTrack.SwordUnsheath => "SFX/Sword Unsheath 2",
                SfxTrack.SwordWhoosh => "SFX/SFX_Whoosh_Sword_01",
                SfxTrack.Equip => "SFX/SFX_Equip_01",
                SfxTrack.BashShield => "SFX/SFX_Bash_Shield_01",
                SfxTrack.CardHover => "SFX/card_hand_hover",
                SfxTrack.ApplyCard => "SFX/apply-card",
                SfxTrack.CoinBag => "SFX/Coin Bag 3-1",
                SfxTrack.CashRegister => "SFX/Cash Register 1-2",
                SfxTrack.Firebuff => "SFX/Firebuff 1",
                SfxTrack.BagHandle => "SFX/Bag Handle 1-5",
                SfxTrack.Interface => "SFX/Interface",
                SfxTrack.Confirm => "SFX/Confirm",
                SfxTrack.PhaseChange => "SFX/Confirm",
                SfxTrack.Transition => "SFX/Transition",
                SfxTrack.Prayer => "SFX/Prayer",
                SfxTrack.GainAegis => "SFX/GainAegis",
                _ => null
            };
            
            if (string.IsNullOrEmpty(assetName)) return null;
            
            try
            {
                var soundEffect = _content.Load<SoundEffect>(assetName);
                _soundCache[track] = soundEffect;
                return soundEffect;
            }
            catch
            {
                return null;
            }
        }
    }
}

