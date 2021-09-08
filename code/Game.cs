using Sandbox;
using Sandbox.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

[Library( "sandbox", Title = "Sandbox" )]
partial class SandboxGame : Game
{
	[Net] public int propCount { get; set; } = 0;
	[Net] public int propCountAtStart { get; set; } = 0;
	[Net] public int propsKilled { get; set; } = 0;
	[Net] public int roundCount { get; set; } = 0;
	[Net] public int hunterCount { get; set; } = 0;
	[Net] public int hunterCountAtStart { get; set; } = 0;
	[Net] public int huntersKilled { get; set; } = 0;
	[Net] public bool GameActive { get; set; } = false;
	[Net] public bool HuntersBlind { get; set; } = false;
	[Net] public bool canStartGame { get; set; } = true;

	[ConVar.Replicated( "prophunt_min_players" )]
	public static int prophunt_min_players { get; set; } = 2;

	[ConVar.Replicated( "prophunt_restart_round_time" )]
	public static int prophunt_restart_round_time { get; set; } = 5;

	public SandboxGame()
	{
		if ( IsServer )
		{
			// Create the HUD
			_ = new SandboxHud();
		}
	}

	public override void ClientJoined( Client cl )
	{
		base.ClientJoined( cl );
		var player = new SandboxPlayer();
		player.Respawn();
		player.notSeenIntro = true;
		cl.Pawn = player;
	}

	[Net] public float CurrentBlindTimer { get; set; } = 0f;
	[Net] public float CurrentTimer { get; set; } = 0f;

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		if ( GameActive && IsServer )
		{
			CurrentTimer = MathX.Clamp( CurrentTimer, 0f, float.MaxValue );
			CurrentBlindTimer = MathX.Clamp( CurrentBlindTimer, 0f, float.MaxValue );

			if(HuntersBlind == true)
			{
				CurrentBlindTimer -= Time.Delta / Client.All.Count;
				// Log.Info(CurrentBlinqdTimer);
			}

			if (canStartGame == true)
			{
				CurrentTimer -= Time.Delta / Client.All.Count;
			}
			
			if (CurrentBlindTimer <= 0f && IsServer && HuntersBlind == true)
			{
				HuntersBlind = false;
				UnblindAll();
			}

			if (CurrentTimer <= 0f  && IsServer)
			{
				RestartRound();
			}
			else if ((propsKilled == propCountAtStart) && (huntersKilled == hunterCountAtStart) && IsServer)
			{
				RestartRound();
			}
			else if(propsKilled == propCountAtStart)
			{
				RestartRound();
			}
			else if(huntersKilled == hunterCountAtStart)
			{
				RestartRound();
			} //this could probably be one gross if statement but eww
		}

		else if((this.hunterCount + this.propCount >= prophunt_min_players) && IsServer && (GameActive != true))
		{
			StartRound();
		}
	}

	public async void RestartRound()
	{
		// Log.Info("boy howdy am i being called");
		if(canStartGame == false)
		{
			return;
		}
		canStartGame = false;
		Log.Info("started waiting");
		await GameTask.DelayRealtimeSeconds( prophunt_restart_round_time );
		Log.Info("finished waiting");
		StartRound();
		canStartGame = true;
	}

	[ServerCmd( "prophunt_jointeam" )]
	public static void prophunt_jointeam( string team )
	{
		if ( ConsoleSystem.Caller.Pawn is not Player local ) return;
		var player = (SandboxPlayer)(ConsoleSystem.Caller.Pawn);
		if(team.ToLower() == "spectator")
		{
			if(player.CurTeam == SandboxPlayer.Team.Spectator)
			{
				Log.Info( "You're already in that team!" );
				return;
			}
			if(player.CurTeam == SandboxPlayer.Team.Hunter && (Game.Current as SandboxGame).hunterCount > 0)
			{
				(Game.Current as SandboxGame).hunterCount--;
				if(((Game.Current as SandboxGame).hunterCount + (Game.Current as SandboxGame).propCount <= prophunt_min_players) && (Game.Current as SandboxGame).IsServer && ((Game.Current as SandboxGame).GameActive == true))
				{
					(Game.Current as SandboxGame).GameActive = false;
				}
				player.OnKilled();
			}

			if(player.CurTeam == SandboxPlayer.Team.Prop && (Game.Current as SandboxGame).propCount > 0)
			{
				(Game.Current as SandboxGame).propCount--;
				if(((Game.Current as SandboxGame).hunterCount + (Game.Current as SandboxGame).propCount <= prophunt_min_players) && (Game.Current as SandboxGame).IsServer && ((Game.Current as SandboxGame).GameActive == true))
				{
					(Game.Current as SandboxGame).GameActive = false;
				}
				player.OnKilled();
			}

			player.CurTeam = SandboxPlayer.Team.Spectator;
			player.Respawn();
			Log.Info( "Joined team Spectator!" );
		} 
		if(team.ToLower() == "hunters") 
		{
			if(player.CurTeam == SandboxPlayer.Team.Hunter)
			{
				Log.Info( "You're already in that team!" );
				return;
			}
			
			(Game.Current as SandboxGame).hunterCount++;
			if(player.CurTeam == SandboxPlayer.Team.Prop && (Game.Current as SandboxGame).propCount > 0)
			{
				(Game.Current as SandboxGame).propCount--;
				if(((Game.Current as SandboxGame).hunterCount + (Game.Current as SandboxGame).propCount <= prophunt_min_players) && (Game.Current as SandboxGame).IsServer && ((Game.Current as SandboxGame).GameActive == true))
				{
					(Game.Current as SandboxGame).GameActive = false;
				}
				player.OnKilled();
			}
			
			player.CurTeam = SandboxPlayer.Team.Hunter;
			player.OnKilled();
			player.Respawn();
			Log.Info( "Joined team Hunters!" );
		}
		else if (team.ToLower() == "props") 
		{
			if(player.CurTeam == SandboxPlayer.Team.Prop)
			{
				Log.Info( "You're already in that team!" );
				return;
			}

			(Game.Current as SandboxGame).propCount++;
			if(player.CurTeam == SandboxPlayer.Team.Hunter && (Game.Current as SandboxGame).hunterCount > 0)
			{
				(Game.Current as SandboxGame).hunterCount--;
				if(((Game.Current as SandboxGame).hunterCount + (Game.Current as SandboxGame).propCount <= prophunt_min_players) && (Game.Current as SandboxGame).IsServer && ((Game.Current as SandboxGame).GameActive == true))
				{
					(Game.Current as SandboxGame).GameActive = false;
				}
				player.OnKilled();
			}

			player.CurTeam = SandboxPlayer.Team.Prop;
			player.Respawn();
			Log.Info( "Joined team Props!" );
		} else 
		{
			Log.Info( "Unknown Team!" );
		}
	}


	public override void ClientDisconnect( Client cl, NetworkDisconnectionReason reason )
	{
		if((cl.Pawn as SandboxPlayer).CurTeam == SandboxPlayer.Team.Prop && (Game.Current as SandboxGame).propCount > 0)
		{
			(Game.Current as SandboxGame).propCount--;
		}
		else if((cl.Pawn as SandboxPlayer).CurTeam == SandboxPlayer.Team.Prop && (Game.Current as SandboxGame).propCount > 0)
		{
			(Game.Current as SandboxGame).hunterCount--;
		}

		if((this.hunterCount + this.propCount <= prophunt_min_players) && IsServer && (GameActive == true))
		{
			GameActive = false;
		}
		
		base.ClientDisconnect( cl, reason );
	}

	[ConVar.Replicated( "prophunt_hunter_blind_time" )]
	public static float prophunt_hunter_blind_time { get; set; } = 30;

	[ConVar.Replicated( "prophunt_time_limit" )]
	public static float prophunt_time_limit { get; set; } = 300;

	[ServerCmd( "prophunt_forcegamestart" )]
	public static void prophunt_forcegamestart( )
	{
		StartRound();
	}

	public static void StartRound()
	{
		(Game.Current as SandboxGame).roundCount++;
		(Game.Current as SandboxGame).GameActive = true;
		(Game.Current as SandboxGame).HuntersBlind = true;
		(Game.Current as SandboxGame).CurrentBlindTimer = prophunt_hunter_blind_time;
		(Game.Current as SandboxGame).CurrentTimer = prophunt_time_limit;
		(Game.Current as SandboxGame).propCountAtStart = (Game.Current as SandboxGame).propCount;
		(Game.Current as SandboxGame).hunterCountAtStart = (Game.Current as SandboxGame).hunterCount;
		(Game.Current as SandboxGame).huntersKilled = 0;
		(Game.Current as SandboxGame).propsKilled = 0;

		foreach ( var player in Entity.All.OfType<SandboxPlayer>().ToList() )
		{
			if(player.CurTeam == SandboxPlayer.Team.Prop)
			{
				player.SpawnProp();
				player.spectating = false;
			}
			if(player.CurTeam == SandboxPlayer.Team.Hunter)
			{
				player.SpawnHunterBlind();
				player.spectating = false;
			}
		}
	}

	public virtual void UnblindAll()
	{
		foreach ( var player in Entity.All.OfType<SandboxPlayer>().ToList() )
		{
			if(player.CurTeam == SandboxPlayer.Team.Hunter)
			{
				player.Unblind();
			}
		}
	}
}
