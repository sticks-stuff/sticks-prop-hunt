
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;

public partial class IntroScreen : Panel
{
	public static IntroScreen Instance;

	public IntroScreen()
	{
		Instance = this;

		StyleSheet.Load( "/ui/IntroScreen.scss" );

		var left = Add.Panel( "left" );
		{
			var body = left.Add.Panel( "body" );
			Label title = body.Add.Label(  "stick's Prop Hunt!", "title" );
			Label description = body.Add.Label(  "Hold Q to select a team\nFollow me on twitter twitter.com/stick_twt\nGitHub: github.com/sticks-stuff/sticks-prop-hunt\nSorry can't add links to hud elements, blame Garry!", "description" );
			Label continueButton = body.Add.Label( "Continue", "continueButton" );
			continueButton.AddEventListener( "onclick", () =>
			{
				ConsoleSystem.Run( "prophunt_seen_intro" );
			});
		}
	}

	public override void Tick()
	{
		base.Tick();
		// Parent.SetClass( "introopen", (Local.Pawn as SandboxPlayer).notSeenIntro );
		Parent.SetClass( "introopen", (Local.Pawn as SandboxPlayer).notSeenIntro );
	}

}
