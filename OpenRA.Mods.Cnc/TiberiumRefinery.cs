#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.Cnc
{
	class TiberiumRefineryInfo : OreRefineryInfo
	{
		public override object Create(ActorInitializer init) { return new TiberiumRefinery(init.self, this); }
	}
	class TiberiumRefinery : OreRefinery
	{
		public TiberiumRefinery(Actor self, TiberiumRefineryInfo info)
			: base(self, info as OreRefineryInfo) {}
		
		public override Activity DockSequence(Actor harv, Actor self)
		{
			return new HarvesterDockSequence(harv, self);
		}
	}
}
