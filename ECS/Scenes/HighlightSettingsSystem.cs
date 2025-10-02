using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Highlight Settings")]
    public class HighlightSettingsSystem : Core.System
    {
        public HighlightSettingsSystem(EntityManager entityManager) : base(entityManager) { }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime) { }

        private EquipmentHighlightSettings Ensure()
        {
            var e = EntityManager.GetEntitiesWithComponent<EquipmentHighlightSettings>().FirstOrDefault();
            if (e == null)
            {
                e = EntityManager.CreateEntity("EquipmentHighlightSettings");
                EntityManager.AddComponent(e, new EquipmentHighlightSettings());
            }
            return e.GetComponent<EquipmentHighlightSettings>();
        }

        [DebugEditable(DisplayName = "Glow Layers", Step = 1, Min = 1, Max = 80)]
        public int GlowLayers { get => Ensure().GlowLayers; set => Ensure().GlowLayers = Math.Max(1, value); }
        [DebugEditable(DisplayName = "Glow Spread", Step = 0.001f, Min = 0f, Max = 0.2f)]
        public float GlowSpread { get => Ensure().GlowSpread; set => Ensure().GlowSpread = Math.Max(0f, value); }
        [DebugEditable(DisplayName = "Glow Spread Speed", Step = 0.1f, Min = 0f, Max = 20f)]
        public float GlowSpreadSpeed { get => Ensure().GlowSpreadSpeed; set => Ensure().GlowSpreadSpeed = Math.Max(0f, value); }
        [DebugEditable(DisplayName = "Glow Spread Amplitude", Step = 0.01f, Min = 0f, Max = 1f)]
        public float GlowSpreadAmplitude { get => Ensure().GlowSpreadAmplitude; set => Ensure().GlowSpreadAmplitude = Math.Max(0f, value); }
        [DebugEditable(DisplayName = "Max Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
        public float MaxAlpha { get => Ensure().MaxAlpha; set => Ensure().MaxAlpha = Math.Max(0f, value); }
        [DebugEditable(DisplayName = "Pulse Speed", Step = 0.1f, Min = 0.1f, Max = 20f)]
        public float GlowPulseSpeed { get => Ensure().GlowPulseSpeed; set => Ensure().GlowPulseSpeed = Math.Max(0.1f, value); }
        [DebugEditable(DisplayName = "Easing Power", Step = 0.1f, Min = 0.2f, Max = 5f)]
        public float GlowEasingPower { get => Ensure().GlowEasingPower; set => Ensure().GlowEasingPower = Math.Max(0.2f, value); }
        [DebugEditable(DisplayName = "Min Pulse Intensity", Step = 0.01f, Min = 0f, Max = 1f)]
        public float GlowMinIntensity { get => Ensure().GlowMinIntensity; set => Ensure().GlowMinIntensity = Math.Max(0f, value); }
        [DebugEditable(DisplayName = "Max Pulse Intensity", Step = 0.01f, Min = 0f, Max = 1f)]
        public float GlowMaxIntensity { get => Ensure().GlowMaxIntensity; set => Ensure().GlowMaxIntensity = Math.Max(0f, value); }
        [DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 64)]
        public int CornerRadius { get => Ensure().CornerRadius; set => Ensure().CornerRadius = Math.Max(0, value); }
        [DebugEditable(DisplayName = "Highlight Border Thickness", Step = 1, Min = 0, Max = 64)]
        public int HighlightBorderThickness { get => Ensure().HighlightBorderThickness; set => Ensure().HighlightBorderThickness = Math.Max(0, value); }
        [DebugEditable(DisplayName = "Glow Color R", Step = 1, Min = 0, Max = 255)]
        public int GlowColorR { get => Ensure().GlowColorR; set => Ensure().GlowColorR = Math.Clamp(value, 0, 255); }
        [DebugEditable(DisplayName = "Glow Color G", Step = 1, Min = 0, Max = 255)]
        public int GlowColorG { get => Ensure().GlowColorG; set => Ensure().GlowColorG = Math.Clamp(value, 0, 255); }
        [DebugEditable(DisplayName = "Glow Color B", Step = 1, Min = 0, Max = 255)]
        public int GlowColorB { get => Ensure().GlowColorB; set => Ensure().GlowColorB = Math.Clamp(value, 0, 255); }
    }
}


