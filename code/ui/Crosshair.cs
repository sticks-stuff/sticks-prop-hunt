
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

public class Crosshair : Panel
{
	int fireCounter;

	public Crosshair()
	{
		for( int i=0; i<5; i++ )
		{
			var p = Add.Panel( "element" );
			p.AddClass( $"el{i}" );
		}
	}

	public override void Tick()
	{
		base.Tick();
		var player = Local.Pawn as Player;
		if ( !player.IsValid() ) return;

		var eyePos = player.EyePos;
		var eyeRot = player.EyeRot;

		var tr = Trace.Ray( eyePos, eyePos + eyeRot.Forward * 2000 )
						.Size( 1.0f )
						.Ignore( player )
						.UseHitboxes()
						.Run();

		var screenpos = tr.EndPos.ToScreen();

		if ( screenpos.z < 0 )
			return;

		this.Style.Left = Length.Fraction( screenpos.x );
		this.Style.Top = Length.Fraction( screenpos.y );
		this.Style.Dirty();

		SetClass( "fire", fireCounter > 0 );

		if ( fireCounter > 0 )
			fireCounter--;
	}

	[PanelEvent]
	public void FireEvent()
	{
		fireCounter += 2;
	}
}
