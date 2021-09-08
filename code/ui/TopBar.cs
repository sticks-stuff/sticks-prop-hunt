using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

public class TopBar : Panel
{
	public Label topBar;

	public TopBar()
	{
		var topBarInner = Add.Panel( "topBarInner" );
		topBar = topBarInner.Add.Label( "", "topBar" );
	}

	public override void Tick()
	{
		var player = (Local.Pawn as SandboxPlayer);
		if ( (Game.Current as SandboxGame).GameActive != true )
		{
			topBar.Text = "Waiting for players...";
			Parent.SetClass("hide", false);
			SetClass("hide", false);
		}
		else if ( (Game.Current as SandboxGame).HuntersBlind == true )
		{
			topBar.Text = $"Hunters will be released in {FormatTimer((Game.Current as SandboxGame).CurrentBlindTimer)}";
			Parent.SetClass("hide", false);
			SetClass("hide", false);
		}
		else if ((Game.Current as SandboxGame).CurrentTimer <= 0f)
		{
			topBar.Text = "Props win!";
			Parent.SetClass("hide", false);
			SetClass("hide", false);
		}
		else if (((Game.Current as SandboxGame).propsKilled == (Game.Current as SandboxGame).propCountAtStart) && ((Game.Current as SandboxGame).huntersKilled == (Game.Current as SandboxGame).hunterCountAtStart))
		{
			topBar.Text = "Draw, everyone loses!";
			Parent.SetClass("hide", false);
			SetClass("hide", false);
		}
		else if((Game.Current as SandboxGame).propsKilled == (Game.Current as SandboxGame).propCountAtStart)
		{
			topBar.Text = "Hunters win!";
			Parent.SetClass("hide", false);
			SetClass("hide", false);
		}
		else if((Game.Current as SandboxGame).huntersKilled == (Game.Current as SandboxGame).hunterCountAtStart)
		{
			topBar.Text = "Props win!";
			Parent.SetClass("hide", false);
			SetClass("hide", false);
		}
		else
		{
			topBar.Text = "";
			Parent.SetClass("hide", true);
			SetClass("hide", true);
		}
	}

	public string FormatTimer(float time)
	{
		int secs = MathX.CeilToInt( time );
		float mins = secs / 60;
		int roundMins = MathX.FloorToInt( mins );
		int minsSecs = secs - roundMins*60;
		string secPortion = minsSecs.ToString();
		if (minsSecs < 10)
		{
			secPortion = "0" + secPortion;
		}
		return roundMins + ":" + secPortion;
	}
}
