using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

public class CurrentWeaponName : Panel
{
	public Label Label;

	public CurrentWeaponName()
	{
		Label = Add.Label( "", "value" );
	}

	public override void Tick()
	{
		SetClass( "changepos", true );

		var player = (Local.Pawn as SandboxPlayer);

		if ( player == null ) return;

		if(player.CurTeam == SandboxPlayer.Team.Spectator)
		{
			Parent.SetClass("hide", true);
			SetClass("hide", true);
			return;
		}
		else 
		{
			Parent.SetClass("hide", false);
			SetClass("hide", false);
		}

		var cac = player.ActiveChild;

		if ( cac == null ) 
		{
			Parent.SetClass("hide", true);
			SetClass("hide", true);
			return;
		} 
		else 
		{
			Parent.SetClass("hide", false);
			SetClass("hide", false);
		}

		Label.Text = cac.ClassInfo.Title;

		var wep = player.ActiveChild as Weapon;

		if ( wep == null ) return;

		SetClass( "changepos", wep.ClipSize < 0 );
	}
}
