using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Sandbox.UI
{
	public class NameTagsPH : NameTags
	{
		public override BaseNameTag CreateNameTag( Player player )
		{
			if ( player.GetClientOwner() == null || !((player as SandboxPlayer).CurTeam == SandboxPlayer.Team.Hunter) )
				return null;

			var tag = new BaseNameTag( player );
			tag.Parent = this;
			return tag;
		}
	}
}
