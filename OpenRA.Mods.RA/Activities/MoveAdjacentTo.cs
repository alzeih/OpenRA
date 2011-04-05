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
using System.Linq;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA.Activities
{
	public class MoveAdjacentTo : Activity
	{
		readonly Actor target;

		public MoveAdjacentTo( Actor target )
		{
			this.target = target;
		}

		public override Activity Tick( Actor self )
		{
			if( IsCanceled || target.Destroyed || !target.IsInWorld) return NextActivity;

			var mobile = self.Trait<Mobile>();
			var ps1 = new PathSearch( self.World, mobile.Info )
			{
				checkForBlocked = true,
				heuristic = location => 0,
				inReverse = true
			};
			foreach( var cell in target.Trait<IOccupySpace>().OccupiedCells() )
			{
				ps1.AddInitialCell( cell.First );
				if( ( mobile.toCell - cell.First ).LengthSquared <= 2 )
					return NextActivity;
			}
			ps1.heuristic = PathSearch.DefaultEstimator( mobile.toCell );

			var ps2 = PathSearch.FromPoint( self.World, mobile.Info, mobile.toCell, target.Location, true );
			var ret = self.World.WorldActor.Trait<PathFinder>().FindBidiPath( ps1, ps2 );
			if( ret.Count > 0 )
				ret.RemoveAt( 0 );
			return Util.SequenceActivities( mobile.MoveTo( () => ret ), this );
		}
	}
}
