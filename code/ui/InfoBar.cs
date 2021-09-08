using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

public class InfoBar : Panel
{
	public Label teamName;
	public Label timeLeft;
	public Label roundCount;

	public InfoBar()
	{
		var infoBarInner = Add.Panel( "infoBarInner" );
		teamName = infoBarInner.Add.Label( "Team: Unknown", "teamName" );
		timeLeft = infoBarInner.Add.Label( "00:00", "timeLeft" );
		roundCount = infoBarInner.Add.Label( "Round: 0", "roundCount" );
	}

	public override void Tick()
	{
		var player = (Local.Pawn as SandboxPlayer);

		if ( player == null ) return;

		if(player.CurTeam == SandboxPlayer.Team.Hunter)
		{
			this.teamName.Text = "Team: Hunters";
		}
		else if(player.CurTeam == SandboxPlayer.Team.Prop)
		{
			this.teamName.Text = "Team: Props";
		} 
		else
		{
			this.teamName.Text = "Team: Spectators";
		}

		timeLeft.Text = $"{FormatTimer((Game.Current as SandboxGame).CurrentTimer)}";
		roundCount.Text = $"Round: {(Game.Current as SandboxGame).roundCount}";
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
