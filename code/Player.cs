using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

partial class SandboxPlayer : Player
{

	private DamageInfo lastDamage;

	[Net] public PawnController VehicleController { get; set; }
	[Net] public PawnAnimator VehicleAnimator { get; set; }
	[Net] public bool notSeenIntro { get; set; }
	[Net, Predicted] public ICamera VehicleCamera { get; set; }
	[Net, Predicted] public Entity Vehicle { get; set; }

	[Net, Local] public int maxHealth { get; set; } = 100;
	[Net, Predicted] public ICamera MainCamera { get; set; }

	// [ConVar.Replicated("sv_fall_damage")]
	private float previousZVelocity { get; set; } = 0;
	private bool takeFallDamage = false;
	[Net] public bool spectating { get; set; }

	public ICamera LastCamera { get; set; }

	public enum Team
	{
		Hunter,
		Prop,
		Spectator,
	}
	
	[Net]
	public Team CurTeam { get; set; }

	public SandboxPlayer()
	{
		Inventory = new Inventory( this );
	}

	public override void Spawn()
	{
		base.Spawn();
		CurTeam = Team.Spectator;
	}

	public override void Respawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );
		Camera = new FreeSpectateCamera();
		Controller = new WalkController();
		// (Controller as WalkController).WalkSpeed = 230.0f;
		// (Controller as WalkController).DefaultSpeed = 230.0f;
		EnableAllCollisions = false;
		ResetInterpolation();
		Inventory.DeleteContents();
		Velocity = Vector3.Zero;
		EnableDrawing = false;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
		this.spectating = true;
	}

	public virtual void SpawnProp()
	{
		// var startingModel = new ModelEntity();
		// startingModel.SetModel( "models/citizen/citizen.vmdl" );
		// Log.Info(startingModel.CollisionBounds.Mins);
		// Log.Info(startingModel.CollisionBounds.Maxs);
		// Controller = new PropController(startingModel);
		Controller = new PropController();

		ClearCollisionLayers();
		UsePhysicsCollision = false;
		Animator = new PropAnimator();

		Camera = new ThirdPersonCamera();
		ThirdPersonCamera.thirdperson_collision = true;

		hat?.Delete();
		jacket?.Delete();
		pants?.Delete();
		shoes?.Delete();
		
		SetBodyGroup( "Chest", 0 );
		SetBodyGroup( "Feet", 0 );
		SetBodyGroup("Legs", 0);

		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		Host.AssertServer();

		LifeState = LifeState.Alive;
		Health = 100;
		// maxHealth = 100;
		Velocity = Vector3.Zero;
		WaterLevel.Clear();
		CreateHull();
		CollisionGroup = CollisionGroup.Player;
		AddCollisionLayer( CollisionLayer.Player );
		// SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3( -1, -1, 0 ), new Vector3( 1, 1, 1 ) );
		MoveType = MoveType.MOVETYPE_WALK;
		EnableHitboxes = true;

		Game.Current?.MoveToSpawnpoint( this );
		ResetInterpolation();
		Inventory.DeleteContents();
	}

	public virtual void SpawnHunterBlind()
	{
		SetModel( "models/citizen/citizen.vmdl" );

		Controller = new HunterController();
		(Controller as HunterController).DefaultSpeed = 0.0f;
		Animator = new StandardPlayerAnimator();

		Camera = new BlindCamera();

		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
		Inventory.DeleteContents();

		Dress();
		base.Respawn();
	}

	public virtual void Unblind()
	{
		Camera = new FirstPersonCamera();
		(Controller as HunterController).DefaultSpeed = 230.0f;
		Inventory.Add(new Crowbar());
		Inventory.Add(new Shotgun());
		Inventory.Add(new SMG());
	}
	
	[ServerCmd( "prophunt_blindme" )]
	public static void prophunt_blindme( )
	{
		if ( ConsoleSystem.Caller.Pawn is not Player local ) return;
		ConsoleSystem.Caller.Pawn.Camera = new BlindCamera();
	}
	[ServerCmd( "prophunt_unblindme" )]
	public static void prophunt_unblindme( )
	{
		if ( ConsoleSystem.Caller.Pawn is not Player local ) return;
		ConsoleSystem.Caller.Pawn.Camera = new FirstPersonCamera();
	}

	public override void OnKilled()
	{
		base.OnKilled();
		if(this.CurTeam == SandboxPlayer.Team.Prop)
		{
			(Game.Current as SandboxGame).propsKilled++;
		} 
		else if(this.CurTeam == SandboxPlayer.Team.Hunter)
		{
			(Game.Current as SandboxGame).huntersKilled++;
		}

		if ( lastDamage.Flags.HasFlag( DamageFlags.Vehicle ) )
		{
			Particles.Create( "particles/impact.flesh.bloodpuff-big.vpcf", lastDamage.Position );
			Particles.Create( "particles/impact.flesh-big.vpcf", lastDamage.Position );
			PlaySound( "kersplat" );
		}

		VehicleController = null;
		VehicleAnimator = null;
		VehicleCamera = null;
		Vehicle = null;
		Inventory.DeleteContents();

		if(this.CurTeam == SandboxPlayer.Team.Prop) 
		{
			EnableDrawing = false;
			EnableAllCollisions = false;
		} 
		else
		{
			BecomeRagdollOnClient( Velocity, lastDamage.Flags, lastDamage.Position, lastDamage.Force, GetHitboxBone( lastDamage.HitboxIndex ) );
			LastCamera = MainCamera;
			MainCamera = new SpectateRagdollCamera();
			Camera = MainCamera;
			Controller = null;

			EnableAllCollisions = false;
			EnableDrawing = false;
		}
	}

	public override void TakeDamage( DamageInfo info )
	{
		if ( GetHitboxGroup( info.HitboxIndex ) == 1 )
		{
			info.Damage *= 2.0f;
		}

		lastDamage = info;

		TookDamage( lastDamage.Flags, lastDamage.Position, lastDamage.Force );

		base.TakeDamage( info );

		//Log.Info( info.Attacker is SandboxPlayer attacker && attacker != this );

		if ( (info.Attacker != null && (info.Attacker is SandboxPlayer || info.Attacker.Owner is SandboxPlayer)) )
		{
			SandboxPlayer attacker = info.Attacker as SandboxPlayer;

			if ( attacker == null )
				attacker = info.Attacker.Owner as SandboxPlayer;

			// Note - sending this only to the attacker!
			if ( attacker != this )
				attacker.DidDamage( To.Single( attacker ), info.Position, info.Damage, Health.LerpInverse( 100, 0 ), Health <= 0 );
		}

		TookDamage( To.Single( this ), (info.Weapon != null && info.Weapon.IsValid()) ? info.Weapon.Position : (info.Attacker != null && info.Attacker.IsValid()) ? info.Attacker.Position : Position );
	}

	[ClientRpc]
	public void TookDamage( DamageFlags damageFlags, Vector3 forcePos, Vector3 force )
	{
	}

	[ClientRpc]
	public void DidDamage( Vector3 pos, float amount, float healthinv, bool isdeath )
	{
		Sound.FromScreen( "dm.ui_attacker" )
			.SetPitch( 1 + healthinv * 1 );

		HitIndicator.Current?.OnHit( pos, amount, isdeath );
	}

	[ClientRpc]
	public void TookDamage( Vector3 pos )
	{
		//DebugOverlay.Sphere( pos, 5.0f, Color.Red, false, 50.0f );

		DamageIndicator.Current?.OnHit( pos );
	}

	public override PawnController GetActiveController()
	{
		return base.GetActiveController();
	}

	public override PawnAnimator GetActiveAnimator()
	{
		if ( VehicleAnimator != null ) return VehicleAnimator;

		return base.GetActiveAnimator();
	}

	public ICamera GetActiveCamera()
	{
		if ( VehicleCamera != null ) return VehicleCamera;

		return MainCamera;
	}

	public bool IsOnGround()
	{
		var tr = Trace.Ray( Position, Position + Vector3.Down * 5 )
				.Radius( 1 )
				.Ignore( this )
				.Run();

		return tr.Hit;
	}

	public void TakeDmg()
	{
		using ( Prediction.Off() )
		{
			var info = DealDamageBasedOnVelocityZ();
			if ( info.Damage > 0 )
			{
				PlaySound( "break-flesh-small" );
				TakeDamage( info );
			}
		}
	}

	private DamageInfo DealDamageBasedOnVelocityZ()
	{
		// Log.Info("My last attacker: " + LastAttacker );
		var damage = GetDamageBasedOnVelocityZ();
		var info = DamageInfo.Generic( damage ).WithAttacker( LastAttacker );
		info.HitboxIndex = 1;
		return info;
	}

	private int GetDamageBasedOnVelocityZ()
	{
		int damage = 0;
		if ( previousZVelocity < -2000 )
			damage = 100;
		if ( previousZVelocity < -1700 )
			damage = 99;
		else if ( previousZVelocity < -1500 )
			damage = 80;
		else if ( previousZVelocity < -1200 )
			damage = 50;
		else if ( previousZVelocity < -800 )
			damage = 25;
		else if ( previousZVelocity < -600 )
			damage = 10;

		return damage;
	}

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		if ( Input.ActiveChild != null )
		{
			ActiveChild = Input.ActiveChild;
		}

		if ( LifeState != LifeState.Alive || this.spectating == true)
			return;

		if ( VehicleController != null && DevController is NoclipController )
		{
			DevController = null;
		}
		
		// Log.Info(this.spectating);


		if(this.CurTeam == SandboxPlayer.Team.Prop) 
		{ //all the prop shit
			PropFind(this);
			Rotation.LookAt( Input.Rotation.Forward.WithZ( 0 ), Vector3.Up );
		}

		var controller = GetActiveController();
		if ( controller != null )
			EnableSolidCollisions = !controller.HasTag( "noclip" );

		// TickPlayerUse();
		SimulateActiveChild( cl, ActiveChild );

		if ( Velocity.z < 0 )
		{
			takeFallDamage = true;
			previousZVelocity = Velocity.z;
		}
		else if ( takeFallDamage && Velocity.z == 0 )
		{
			takeFallDamage = false;
			TakeDmg();
		}
	}

	[ServerCmd( "inventory_current" )]
	public static void SetInventoryCurrent( string entName )
	{
		var target = ConsoleSystem.Caller.Pawn;
		if ( target == null ) return;

		var inventory = target.Inventory;
		if ( inventory == null )
			return;

		for ( int i = 0; i < inventory.Count(); ++i )
		{
			var slot = inventory.GetSlot( i );
			if ( !slot.IsValid() )
				continue;

			if ( !slot.ClassInfo.IsNamed( entName ) )
				continue;

			inventory.SetActiveSlot( i, false );

			break;
		}
	}

	[ServerCmd( "prophunt_seen_intro" )]
	public static void prophunt_seen_intro()
	{
		var target = ConsoleSystem.Caller.Pawn;
		if ( target == null ) return;

		(ConsoleSystem.Caller.Pawn as SandboxPlayer).notSeenIntro = false;
	}

	[ServerCmd( "prophunt_show_intro" )]
	public static void prophunt_show_intro()
	{
		var target = ConsoleSystem.Caller.Pawn;
		if ( target == null ) return;

		(ConsoleSystem.Caller.Pawn as SandboxPlayer).notSeenIntro = true;
	}

	public virtual void PropFind(SandboxPlayer prop) //i know this should probably be in its own file but honestly i cba
	{
		var tr = Trace.Ray( prop.EyePos, prop.EyePos + prop.EyeRot.Forward * 130 )
			.UseHitboxes()
			.Ignore( prop )
			.Run();
		
		if ( !tr.Hit )
		{ // Nothing found, try a wider search
			tr = Trace.Ray( prop.EyePos, prop.EyePos + prop.EyeRot.Forward * 130 )
			.UseHitboxes()
			.Radius( 20 )
			.Ignore( prop )
			.Run();
		}

		List<Entity> ents = new List<Entity>();
		ents.AddRange( Entity.All.OfType<ModelEntity>() );
		foreach ( var ent in ents )
		{
			if ( ent is ModelEntity modelEnt )
			{
				if(ent == tr.Entity && tr.Entity is ModelEntity && !(tr.Entity is Player) )
				{
					//Log.Info("asdasdasd");
					modelEnt.GlowActive = true;
					if( Input.Pressed( InputButton.Use ) )
					{
						if(modelEnt.CollisionBounds.Mins == 0 || modelEnt.CollisionBounds.Maxs == 0)
						{
							return;
						}
						// modelEnt.SetModel( "models/sbox_props/watermelon/watermelon.vmdl" );
						SetModel( modelEnt.GetModel() );
						// new PropController().UpdateBBox(modelEnt.CollisionBounds.Mins, modelEnt.CollisionBounds.Maxs);
						Controller = new PropController(modelEnt);
						Log.Info(new BBox(modelEnt.CollisionBounds.Mins, modelEnt.CollisionBounds.Maxs).Volume);
						var ent_health = MathX.Clamp(new BBox(modelEnt.CollisionBounds.Mins, modelEnt.CollisionBounds.Maxs).Volume / 400, 1, 200);
						var new_health = MathX.Clamp((Health / maxHealth) * ent_health, 1, 200);
						Health = new_health;
						maxHealth = (int)ent_health;
					}
				} else 
				{
					modelEnt.GlowActive = false;
				}
			}
		}
	}

	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		if(this.CurTeam == SandboxPlayer.Team.Prop && spectating == false) 
		{ //all the prop shit
			PropCam(ref camSetup);
		}
	}

	public virtual void PropCam(ref CameraSetup camSetup)
	{
		Vector3 targetPos;

		var center = Position + Vector3.Up * 64;
		//Pos = center;

		float distance = 130.0f * Scale;
		targetPos = center;
		targetPos += Input.Rotation.Forward * -distance;
		camSetup.Position = targetPos;
		camSetup.Rotation = Input.Rotation;
		//camSetup.ViewModel.FieldOfView = FieldOfView;
	}

}
