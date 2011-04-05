#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Air
{
	public class FlyAttack : Activity
	{
		readonly Target Target;
		Activity inner;

		public FlyAttack(Target target) { Target = target; }

		public override Activity Tick(Actor self)
		{
			if( !Target.IsValid )
				Cancel( self );
			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if( limitedAmmo != null && !limitedAmmo.HasAmmo() )
				Cancel( self );

			var attack = self.Trait<AttackPlane>();
			attack.DoAttack( self, Target );

			if( inner == null )
			{
				if( IsCanceled )
					return NextActivity;
				inner = Util.SequenceActivities(
					Fly.ToPx(Target.CenterLocation),
					new FlyTimed(50));
			}
			inner = Util.RunActivity( self, inner );

			return this;
		}

		protected override bool OnCancel( Actor self )
		{
			if( inner != null )
				inner.Cancel( self );
			return base.OnCancel( self );
		}
	}

	public class FlyAttackLoop : Activity
	{
		int2 Target;

		public FlyAttackLoop(int2 target) { Target = target; }

		public override Activity Tick(Actor self)
		{
			if( IsCanceled ) return NextActivity;

			return Util.SequenceActivities(
				Fly.ToCell(Target),
				new FlyTimed(50),
				this);
		}
	}
}
