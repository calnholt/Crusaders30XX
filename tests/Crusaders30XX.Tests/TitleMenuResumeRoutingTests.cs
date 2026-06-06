using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class TitleMenuResumeRoutingTests
{
	[Fact]
	public void Fresh_profile_is_inactive_and_routes_to_WayStation()
	{
		SaveCache.DeleteSaveFilesIfPresent();
		_ = SaveCache.GetAll();

		Assert.False(SaveCache.IsRunActive());
		Assert.False(SaveCache.IsStartQuestCompleted());
		Assert.Equal(SceneId.WayStation, TitleMenuResumeService.ResolveDirectTransitionScene());
	}

	[Fact]
	public void Active_run_with_incomplete_start_quest_routes_to_battle_path()
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();

		Assert.True(SaveCache.IsRunActive());
		Assert.False(SaveCache.IsStartQuestCompleted());
		Assert.Null(TitleMenuResumeService.ResolveDirectTransitionScene());
	}

	[Fact]
	public void Active_run_with_completed_start_quest_routes_to_Location()
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();
		SaveCache.SetQuestCompleted(null, SaveCache.GetStartNodeId(), true);

		Assert.True(SaveCache.IsRunActive());
		Assert.True(SaveCache.IsStartQuestCompleted());
		Assert.Equal(SceneId.Location, TitleMenuResumeService.ResolveDirectTransitionScene());
	}

	[Fact]
	public void Inactive_run_routes_to_WayStation_even_when_start_quest_was_completed()
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();
		SaveCache.SetQuestCompleted(null, SaveCache.GetStartNodeId(), true);
		SaveCache.MarkRunInactive();

		Assert.False(SaveCache.IsRunActive());
		Assert.Equal(SceneId.WayStation, TitleMenuResumeService.ResolveDirectTransitionScene());
	}
}
