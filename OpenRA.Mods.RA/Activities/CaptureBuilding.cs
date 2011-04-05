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
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	class CaptureBuilding : Activity
	{
		Actor target;

		public CaptureBuilding(Actor target) { this.target = target; }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (target == null || !target.IsInWorld || target.IsDead()) return NextActivity;
			if (target.Owner == self.Owner) return NextActivity;
			
			if( !target.Trait<IOccupySpace>().OccupiedCells().Any( x => x.First == self.Location ) )
				return NextActivity;

			// todo: clean this up
			self.World.AddFrameEndTask(w =>
			{
				// momentarily remove from world so the ownership queries don't get confused
				var oldOwner = target.Owner;
				w.Remove(target);
				target.Owner = self.Owner;
				w.Add(target);
				
				foreach (var t in target.TraitsImplementing<INotifyCapture>())
					t.OnCapture(target, self, oldOwner, self.Owner);

				foreach (var t in self.World.ActorsWithTrait<INotifyOtherCaptured>())
					t.Trait.OnActorCaptured(t.Actor, target, self, oldOwner, self.Owner);

				self.Destroy();
			});
			return this;
		}
	}
}
