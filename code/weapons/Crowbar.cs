using Sandbox;
using System;

[Library("weapon_crowbar", Title = "Crowbar", Spawnable = true)]
[Hammer.EditorModel("models/weapons/crowbar/crowbar.vmdl")]
partial class Crowbar : Weapon
{
	public override string ViewModelPath => "models/weapons/crowbar/v_crowbar.vmdl";

	public override int ClipSize => -1;
	public override float PrimaryRate => 2.6f;
	public override float SecondaryRate => 0.5f;
	public override float ReloadTime => 0f;
	public override int Bucket => 0;
	public override CType Crosshair => CType.None;
	public virtual int BaseDamage => 25;
	public virtual int MeleeDistance => 90;
	public override string Icon => "ui/weapons/weapon_crowbar.png";

	public override void Spawn()
	{
		base.Spawn();

		SetModel("models/weapons/crowbar/crowbar.vmdl");
	}

	bool isFlesh;

	private bool MeleeAttack()
	{
		var forward = Owner.EyeRot.Forward;
		forward = forward.Normal;

		bool hit = false;

		foreach (var tr in TraceBullet(Owner.EyePos, Owner.EyePos + forward * MeleeDistance, 5.0f))
		{
			if (!tr.Entity.IsValid()) continue;

			tr.Surface.DoBulletImpact(tr);

			hit = true;
			isFlesh = tr.Entity is SandboxPlayer;

			if (!IsServer) continue;

			using (Prediction.Off())
			{
				if(tr.Entity is ModelEntity && !(tr.Entity is SandboxPlayer) && (tr.Entity as ModelEntity).GetModelName() != null)
				{
					var damageInfo = DamageInfo.FromBullet( tr.EndPos, forward * 100, prophunt_hunter_fire_penalty )
						.UsingTraceResult( tr )
						.WithAttacker( Owner )
						.WithWeapon( this );
					Owner.TakeDamage( damageInfo );
					// Owner.Health = Owner.Health - prophunt_hunter_fire_penalty;
					// Log.Info("asdasd");
				}
				else 
				{
					var damageInfo = DamageInfo.FromBullet(tr.EndPos, forward * 100, BaseDamage)
						.UsingTraceResult(tr)
						.WithAttacker(Owner)
						.WithWeapon(this);

					tr.Entity.TakeDamage(damageInfo);
				}
			}
		}

		return hit;
	}

	public override void AttackPrimary()
	{
		if (!BaseAttackPrimary()) return;
		if (MeleeAttack())
		{
			PlaySound(isFlesh ? "weapon_crowbar.hit": "weapon_crowbar.hitworld");

			OnMeleeHit();
		}
		else
		{
			PlaySound("weapon_crowbar.swing");

			OnMeleeMiss();
		}
	}

	[ClientRpc]
	
	private void OnMeleeMiss()
	{
		Host.AssertClient();

		if (IsLocalPawn)
		{
			_ = new Sandbox.ScreenShake.Perlin(1.0f, 1.0f, 3.0f);
		}

		ViewModelEntity?.SetAnimBool("fire", true);
	}

	[ClientRpc]
	private void OnMeleeHit()
	{
		Host.AssertClient();

		if (IsLocalPawn)
		{
			_ = new Sandbox.ScreenShake.Perlin(1.0f, 1.0f, 3.0f);
		}

		ViewModelEntity?.SetAnimBool("fire", true);
		CrosshairPanel?.CreateEvent("fire");
	}

	[ClientRpc]
	protected override void ShootEffects()
	{
		Host.AssertClient();
	}

	public override void SimulateAnimator(PawnAnimator anim)
	{
		anim.SetParam("holdtype", 4); // TODO this is shit
		anim.SetParam("holdtype_attack", 2.0f);
		anim.SetParam("holdtype_handedness", 1);
		anim.SetParam("holdtype_pose", 0f);
		anim.SetParam("aimat_weight", 1.0f);
	}
}
