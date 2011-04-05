#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	public class CallFunc : Activity
	{
		public CallFunc(Action a) { this.a = a; }
		public CallFunc(Action a, bool interruptable)
		{
			this.a = a;
			this.interruptable = interruptable;
		}
		
		Action a;
		bool interruptable;

		public override Activity Tick(Actor self)
		{
			if (a != null) a();
			return NextActivity;
		}

		protected override bool OnCancel(Actor self)
		{
			if (!interruptable)
				return false;
			
			a = null;
			return true;
		}

		public override IEnumerable<Target> GetTargetQueue( Actor self )
		{
			if (NextActivity != null)
				foreach (var target in NextActivity.GetTargetQueue(self))
				{
					yield return target;
				}

			yield break;
		}
	}
}
