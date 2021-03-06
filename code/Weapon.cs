using Sandbox;
using System.Collections.Generic;

public partial class Weapon : BaseWeapon, IUse
{
	public virtual int ClipSize => 16;
	public virtual int ClipTake => 1;
	public virtual float ReloadTime => 3.0f;
	public virtual string ReloadSound => "";
	public virtual bool RealReload => true;
	public virtual int Bucket => 1;
	public virtual int BucketWeight => 100;
	public virtual CType Crosshair => CType.Common;
	public virtual string Icon => "";

	[Net, Predicted]
	public int AmmoClip { get; set; }

	[Net, Predicted]
	public TimeSince TimeSinceReload { get; set; }

	[Net, Predicted]
	public bool IsReloading { get; set; }

	[Net, Predicted]
	public TimeSince TimeSinceDeployed { get; set; }

	public override void Spawn()
	{
		base.Spawn();
		AmmoClip = ClipSize;
	}

	public override void ActiveStart( Entity ent )
	{
		base.ActiveStart( ent );

		TimeSinceDeployed = 0;
		IsReloading = false;
	}

	public override void Reload()
	{
		if ( IsReloading )
			return;

		if ( (AmmoClip >= ClipSize && !(AmmoClip >= 1000) ) && ClipSize > -1 )
			return;

		TimeSinceReload = 0;
		IsReloading = true;

		(Owner as AnimEntity)?.SetAnimBool( "b_reload", true );

		if (ReloadSound != "")
			PlaySound(ReloadSound);

		StartReloadEffects();
	}

	public override void Simulate( Client owner )
	{
		if ( TimeSinceDeployed < 0.6f )
			return;

		if ( !IsReloading )
		{
			base.Simulate( owner );
		}

		if ( IsReloading && TimeSinceReload > ReloadTime )
		{
			OnReloadFinish();
		}
	}

	public bool BaseAttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		if ( !TakeAmmo( ClipTake ) )
		{
			//DryFire();
			Reload();

			return false;
		}

		(Owner as AnimEntity).SetAnimBool( "b_attack", true );

		//
		// Tell the clients to play the shoot effects
		//
		ShootEffects();

		return true;
	}

	public virtual void OnReloadFinish()
	{
		IsReloading = false;

		if(AmmoClip >= 1000) {
			AmmoClip = ClipSize + 1000;
			return;
		}

		if ( AmmoClip <= 0 || !RealReload )
			AmmoClip = ClipSize;
		else
			AmmoClip = ClipSize + 1;
	}

	[ClientRpc]
	public virtual void StartReloadEffects()
	{
		ViewModelEntity?.SetAnimBool( "reload", true );

		// TODO - player third person model reload
	}

	public bool TakeAmmo( int amount )
	{
		if ( ClipSize < 0 ) return true;
		if ( (AmmoClip < amount) || (AmmoClip == 1000) )
			return false;

		AmmoClip -= amount;
		return true;
	}

	[ClientRpc]
	public virtual void DryFire()
	{
		// CLICK
	}

	public override void CreateViewModel()
	{
		Host.AssertClient();

		if ( string.IsNullOrEmpty( ViewModelPath ) )
			return;

		ViewModelEntity = new ViewModel
		{
			Position = Position,
			Owner = Owner,
			EnableViewmodelRendering = true
		};

		ViewModelEntity.SetModel( ViewModelPath );
	}

	public override void CreateHudElements()
	{
		if ( Local.Hud == null || Crosshair == CType.None ) return;

		CrosshairPanel = new Crosshair();
		CrosshairPanel.Parent = Local.Hud;

		if ( Crosshair == CType.Common ) return;

		CrosshairPanel.AddClass( Crosshair.ToString() );
	}

	public bool OnUse( Entity user )
	{
		if ( Owner != null )
			return false;

		if ( !user.IsValid() )
			return false;

		user.StartTouch( this );

		return false;
	}

	public virtual bool IsUsable( Entity user )
	{
		if ( Owner != null ) return false;

		if ( user.Inventory is Inventory inventory )
		{
			return inventory.CanAdd( this );
		}

		return true;
	}

	public void Remove()
	{
		PhysicsGroup?.Wake();
		Delete();
	}

	[ClientRpc]
	protected virtual void ShootEffects()
	{
		Host.AssertClient();

		Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );

		if ( IsLocalPawn )
		{
			_ = new Sandbox.ScreenShake.Perlin();
		}

        ViewModelEntity?.SetAnimBool("fire", true);
		CrosshairPanel?.CreateEvent( "fire" );
	}

	[ConVar.Replicated( "prophunt_hunter_fire_penalty" )]
	public static float prophunt_hunter_fire_penalty { get; set; } = 5;


	/// <summary>
	/// Shoot a single bullet
	/// </summary>
	public virtual void ShootBullet( Vector3 pos, Vector3 dir, float spread, float force, float damage, float bulletSize )
	{
		var forward = dir;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
		forward = forward.Normal;

		//
		// ShootBullet is coded in a way where we can have bullets pass through shit
		// or bounce off shit, in which case it'll return multiple results
		//
		foreach ( var tr in TraceBullet( pos, pos + forward * 5000, bulletSize ) )
		{
			tr.Surface.DoBulletImpact( tr );

			if ( !IsServer ) continue;
			if ( !tr.Entity.IsValid() ) continue;

			//
			// We turn predictiuon off for this, so any exploding effects don't get culled etc
			//
			using ( Prediction.Off() )
			{
				if(tr.Entity is ModelEntity && !(tr.Entity is SandboxPlayer) && (tr.Entity as ModelEntity).GetModelName() != null)
				{
					if(((tr.Entity as ModelEntity).GetModelName()).Contains("unnamed")) //for some reason shooting the water in construct damages u unless we do this
					{
						return;
					}
					Log.Info(((tr.Entity as ModelEntity).GetModelName()));
					var damageInfo = DamageInfo.FromBullet( tr.EndPos, forward * 100 * force, prophunt_hunter_fire_penalty )
						.UsingTraceResult( tr )
						.WithAttacker( Owner )
						.WithWeapon( this );
					Owner.TakeDamage( damageInfo );
					// Owner.Health = Owner.Health - prophunt_hunter_fire_penalty;
					// Log.Info("asdasd");
				}
				else 
				{
					var damageInfo = DamageInfo.FromBullet( tr.EndPos, forward * 100 * force, damage )
						.UsingTraceResult( tr )
						.WithAttacker( Owner )
						.WithWeapon( this );
					tr.Entity.TakeDamage( damageInfo );
				}
			}
		}
	}

	/// <summary>
	/// Shoot a single bullet from owners view point
	/// </summary>
	public virtual void ShootBullet( float spread, float force, float damage, float bulletSize )
	{
		ShootBullet( Owner.EyePos, Owner.EyeRot.Forward, spread, force, damage, bulletSize );
	}

	public override IEnumerable<TraceResult> TraceBullet( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		bool InWater = Physics.TestPointContents( start, CollisionLayer.Water );

		var tr = Trace.Ray( start, end )
				// .UseHitboxes()
				.HitLayer( CollisionLayer.Water, !InWater )
				.Ignore( Owner )
				.Ignore( this )
				.Size( radius )
				.Run();

		yield return tr;

		//
		// Another trace, bullet going through thin material, penetrating water surface?
		//
	}

	/// <summary>
	/// Shoot a multiple bullets from owners view point
	/// </summary>
	public virtual void ShootBullets( int numBullets, float spread, float force, float damage, float bulletSize )
	{
		var pos = Owner.EyePos;
		var dir = Owner.EyeRot.Forward;

		for ( int i = 0; i < numBullets; i++ )
		{
			ShootBullet( pos, dir, spread, force / numBullets, damage, bulletSize );
		}
	}
}

public enum CType
{
	None,
	Common,
	ShotGun,
	Pistol,
	SMG,
}
