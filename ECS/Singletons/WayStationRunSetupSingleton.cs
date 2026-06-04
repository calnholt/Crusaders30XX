namespace Crusaders30XX.ECS.Singletons
{
	public enum StartingWeapon
	{
		Sword,
		Dagger
	}

	public enum RunDifficulty
	{
		Easy,
		Normal,
		Hard
	}

	public static class WayStationRunSetupSingleton
	{
		public static StartingWeapon SelectedWeapon { get; set; } = StartingWeapon.Sword;
		public static RunDifficulty SelectedDifficulty { get; set; } = RunDifficulty.Easy;

		public static int PlayerMaxHp => SelectedDifficulty switch
		{
			RunDifficulty.Easy => 25,
			RunDifficulty.Normal => 22,
			RunDifficulty.Hard => 20,
			_ => 22
		};

		public static float EnemyHealthModifier => SelectedDifficulty switch
		{
			RunDifficulty.Easy => 0.8f,
			RunDifficulty.Normal => 0.9f,
			RunDifficulty.Hard => 1.0f,
			_ => 0.9f
		};

		public static string WeaponId => SelectedWeapon switch
		{
			StartingWeapon.Sword => "sword",
			StartingWeapon.Dagger => "dagger",
			_ => "sword"
		};
	}
}
