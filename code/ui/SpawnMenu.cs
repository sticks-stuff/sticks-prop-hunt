
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;

public partial class SpawnMenu : Panel
{
	public static SpawnMenu Instance;
	public Label teamNumHunters;
	public Label teamNumProps;

	public SpawnMenu()
	{
		Instance = this;

		StyleSheet.Load( "/ui/SpawnMenu.scss" );

		var left = Add.Panel( "left" );
		{
			var tabs = left.AddChild<ButtonGroup>();
			tabs.AddClass( "tabs" );

			var body = left.Add.Panel( "body" );
			Label chooseateam = body.Add.Label( "Choose A Team", "chooseateam" );
			Panel teamChoice = body.Add.Panel( "teamChoice" );
			Label hunterChoice = teamChoice.Add.Label( "Hunters", "hunterChoice" );
			hunterChoice.AddEventListener( "onclick", () =>
			{
				ConsoleSystem.Run( "prophunt_jointeam Hunters" );
			});

			Label propChoice = teamChoice.Add.Label( "Props", "propChoice" );
			propChoice.AddEventListener( "onclick", () =>
			{
				ConsoleSystem.Run( "prophunt_jointeam Props" );
			});
			
			Label specChoice = body.Add.Label( "Spectators", "specChoice" );
			specChoice.AddEventListener( "onclick", () =>
			{
				ConsoleSystem.Run( "prophunt_jointeam Spectator" );
			});
			
			teamNumHunters = hunterChoice.Add.Label( "(0 currently in team)", "teamNumHunters" );
			teamNumProps = propChoice.Add.Label( "(0 currently in team)", "teamNumProps" );
		}
	}

	public override void Tick()
	{
		base.Tick();
		// ;
		
		Parent.SetClass( "spawnmenuopen", Input.Down( InputButton.Menu ) );
		this.teamNumHunters.Text = $"({(Game.Current as SandboxGame)?.hunterCount} currently in team)";
		this.teamNumProps.Text = $"({(Game.Current as SandboxGame)?.propCount} currently in team)";
		// Log.Info((Game.Current as SandboxGame).propCount);
	}

}
