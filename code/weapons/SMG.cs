using Sandbox;
using System;

[Library( "weapon_smg", Title = "SMG", Spawnable = true )]
[Hammer.EditorModel( "weapons/rust_smg/rust_smg.vmdl" )]
partial class SMG : Weapon
{ 
	public override string ViewModelPath => "weapons/rust_smg/v_rust_smg.vmdl";

	public override int ClipSize => 45;
	public override float PrimaryRate => 15.0f;
	public override float SecondaryRate => 1.0f;
	public override float ReloadTime => 4.0f;
	public override int Bucket => 1;
	public override CType Crosshair => CType.SMG;
	public override string Icon => "ui/weapons/weapon_smg.png";

	public override void Spawn()
	{
		base.Spawn();
		AmmoClip = 1045;
		SetModel( "weapons/rust_smg/rust_smg.vmdl" );
	}

	public override void AttackPrimary()
	{
		if ( !BaseAttackPrimary() ) return;

		PlaySound( "rust_smg.shoot" );

		//
		// Shoot the bullets
		//
		ShootBullet( 0.1f, 1.5f, 5.0f, 3.0f );
	}

	public override void AttackSecondary()
	{
		if ( Parent.IsServer && AmmoClip > 1000)
		{
			using ( Prediction.Off() )
			{
				AmmoClip -= 1000;
				var ent = new Prop();
				ent.SetModel( "models/weapons/ar2_grenade" );
				ent.Position = Owner.EyePos + Owner.EyeRot.Forward * 40;
				Rotation vecThrow = Owner.EyeRot;
				vecThrow.y += 0.125f;
				// ent.AngularVelocity = new Angles(9999, 99999, 99999);
				ent.Rotation = Rotation.LookAt( Vector3.Random.Normal );
				ent.PhysicsGroup.AddAngularVelocity(5);
				ent.Owner = Owner;
				ent.Velocity = Owner.EyeRot.Forward * 1450;
				PlaySound( "grenade_launcher1" );
				ShootEffectsSecondary();
			}
		}
	}

	[ClientRpc]
	protected override void ShootEffects()
	{
		Host.AssertClient();

		Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );
		Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );

		if ( Owner == Local.Pawn )
		{
			new Sandbox.ScreenShake.Perlin(0.5f, 4.0f, 1.0f, 0.5f);
		}

		ViewModelEntity?.SetAnimBool( "fire", true );
		CrosshairPanel?.CreateEvent( "fire" );
	}
	
	[ClientRpc]
	protected virtual void ShootEffectsSecondary()
	{
		Host.AssertClient();

		Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );

		if ( Owner == Local.Pawn )
		{
			new Sandbox.ScreenShake.Perlin(0.5f, 4.0f, 1.0f, 0.5f);
		}

		ViewModelEntity?.SetAnimBool( "fire", true );
		CrosshairPanel?.CreateEvent( "fire" );
	}

	public override void SimulateAnimator( PawnAnimator anim )
	{
		anim.SetParam( "holdtype", 2 ); // TODO this is shit
		anim.SetParam( "aimat_weight", 1.0f );
	}

}
