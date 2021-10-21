using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sandbox
{
	public class PropAnimator : StandardPlayerAnimator
	{
		public override void Simulate()
		{
			var idealRotation = Rotation.LookAt( Input.Rotation.Forward.WithZ( 0 ), Vector3.Up );

			Rotation = idealRotation;
			// DoWalk();

			//
			// Let the animation graph know some shit
			//
			// bool sitting = HasTag( "sitting" );
			// bool noclip = HasTag( "noclip" ) && !sitting;

			// SetParam( "b_grounded", GroundEntity != null || noclip || sitting );
			// SetParam( "b_noclip", noclip );
			// SetParam( "b_sit", sitting );
			// SetParam( "b_swim", Pawn.WaterLevel.Fraction > 0.5f && !sitting );

			Vector3 aimPos = Pawn.EyePos + Input.Rotation.Forward * 200;
			Vector3 lookPos = aimPos;

			// SetParam( "b_ducked", HasTag( "ducked" ) ); // old

			// if ( HasTag( "ducked" ) ) duck = duck.LerpTo( 1.0f, Time.Delta * 10.0f );
			// else duck = duck.LerpTo( 0.0f, Time.Delta * 5.0f );

			// SetParam( "duck", duck );

			// if ( Pawn.ActiveChild is BaseCarriable carry )
			// {
			// 	carry.SimulateAnimator( this );
			// }
			// else
			// {
			// 	SetParam( "holdtype", 0 );
			// 	SetParam( "aimat_weight", 0.5f ); // old
			// 	SetParam( "aim_body_weight", 0.5f );
			// }

		}

		public override void DoRotation( Rotation idealRotation )
		{
			Rotation = idealRotation;
		}
	}
}
