using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

public class Health : Panel
{
	public Label Label;

	public Health()
	{
		Label = Add.Label( "100", "value" );
	}

	public override void Tick()
	{
		var player = (Local.Pawn as SandboxPlayer);
		if ( player == null) return;

		if(player.CurTeam == SandboxPlayer.Team.Spectator || player.spectating == true)
		{
			Parent.SetClass("hide", true);
			SetClass("hide", true);
			Label.Text = "";
			return;
		}
		else 
		{
			Parent.SetClass("hide", false);
			SetClass("hide", false);
		}

		Label.Text = $"{player.Health.CeilToInt()}";

		if ( player.Health < 50 && player.Health > 20 )
		{
			SetClass( "yellow", true );
			SetClass( "danger", false );
		}
		else if ( player.Health <= 20 )
		{
			SetClass( "yellow", false );
			SetClass( "danger", true );
		}
		else
		{
			SetClass( "yellow", false );
			SetClass( "danger", false );
		}
	}
}
