#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	public class Rearm : Activity
	{
		int remainingTicks = ticksPerPip;

		const int ticksPerPip = 25 * 2;

		public override Activity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (limitedAmmo == null) return NextActivity;

			if (--remainingTicks == 0)
			{
				if (!limitedAmmo.GiveAmmo()) return NextActivity;

				var hostBuilding = self.World.FindUnits(self.CenterLocation, self.CenterLocation)
					.FirstOrDefault(a => a.HasTrait<RenderBuilding>());

				if (hostBuilding != null)
					hostBuilding.Trait<RenderBuilding>().PlayCustomAnim(hostBuilding, "active");

				remainingTicks = ticksPerPip;
			}

			return this;
		}
	}
}
