using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Rendering;

public sealed class LayeredHolesOverlay
{
	private readonly Effect _effect;

	public bool IsAvailable => _effect != null;

	public float Time { get; set; }
	public int HoleCount { get; set; } = 30;
	public float HolePeriodMin { get; set; } = 10f;
	public float HolePeriodMax { get; set; } = 20f;
	public float HoleLifeMin { get; set; } = 0.45f;
	public float HoleLifeMax { get; set; } = 0.75f;
	public float HoleOpenFrac { get; set; } = 0.25f;
	public float HoleCloseFrac { get; set; } = 0.30f;
	public float HoleRadiusMin { get; set; } = 0.10f;
	public float HoleRadiusMax { get; set; } = 0.50f;
	public float RadiusFluxAmp { get; set; } = 0.12f;
	public float RadiusFluxRate { get; set; } = 2.20f;
	public float HoleMargin { get; set; } = 0.02f;
	public float HoleFeather { get; set; } = 0.045f;
	public float FeatherVary { get; set; } = 0.70f;
	public float RimWarpAmp { get; set; } = 0.340f;
	public float RimWarpScale { get; set; } = 3.5f;
	public float RimWarpSpeed { get; set; } = 0.35f;
	public float RevealRefract { get; set; } = 0.35f;
	public float LayerSplit { get; set; } = 0.50f;
	public float RevealDarken { get; set; }
	public Texture2D MiddleTexture { get; set; }
	public Texture2D BottomTexture { get; set; }

	public LayeredHolesOverlay(Effect effect)
	{
		_effect = effect;
	}

	public void Begin(SpriteBatch spriteBatch)
	{
		if (_effect == null) return;

		_effect.CurrentTechnique = _effect.Techniques["SpriteDrawing"];

		Viewport viewport = spriteBatch.GraphicsDevice.Viewport;
		Matrix projection = Matrix.CreateOrthographicOffCenter(
			0,
			viewport.Width,
			viewport.Height,
			0,
			0,
			1);

		Set("MatrixTransform", projection);
		Set("ViewportSize", new Vector2(viewport.Width, viewport.Height));
		Set("Time", Time);
		Set("HoleCount", HoleCount);
		Set("HolePeriodMin", HolePeriodMin);
		Set("HolePeriodMax", HolePeriodMax);
		Set("HoleLifeMin", HoleLifeMin);
		Set("HoleLifeMax", HoleLifeMax);
		Set("HoleOpenFrac", HoleOpenFrac);
		Set("HoleCloseFrac", HoleCloseFrac);
		Set("HoleRadiusMin", HoleRadiusMin);
		Set("HoleRadiusMax", HoleRadiusMax);
		Set("RadiusFluxAmp", RadiusFluxAmp);
		Set("RadiusFluxRate", RadiusFluxRate);
		Set("HoleMargin", HoleMargin);
		Set("HoleFeather", HoleFeather);
		Set("FeatherVary", FeatherVary);
		Set("RimWarpAmp", RimWarpAmp);
		Set("RimWarpScale", RimWarpScale);
		Set("RimWarpSpeed", RimWarpSpeed);
		Set("RevealRefract", RevealRefract);
		Set("LayerSplit", LayerSplit);
		Set("RevealDarken", RevealDarken);
		Set("MiddleTexture", MiddleTexture);
		Set("BottomTexture", BottomTexture);

		spriteBatch.Begin(
			SpriteSortMode.Immediate,
			BlendState.Opaque,
			SamplerState.LinearClamp,
			DepthStencilState.None,
			RasterizerState.CullNone,
			_effect);
	}

	public void Draw(SpriteBatch spriteBatch, Texture2D topTexture, Rectangle destination)
	{
		if (_effect == null || topTexture == null) return;
		spriteBatch.Draw(topTexture, destination, Color.White);
	}

	public void End(SpriteBatch spriteBatch)
	{
		if (_effect == null) return;
		spriteBatch.End();
	}

	private void Set(string name, float value)
	{
		_effect.Parameters[name]?.SetValue(value);
	}

	private void Set(string name, int value)
	{
		_effect.Parameters[name]?.SetValue(value);
	}

	private void Set(string name, Vector2 value)
	{
		_effect.Parameters[name]?.SetValue(value);
	}

	private void Set(string name, Matrix value)
	{
		_effect.Parameters[name]?.SetValue(value);
	}

	private void Set(string name, Texture2D value)
	{
		if (value != null) _effect.Parameters[name]?.SetValue(value);
	}
}
