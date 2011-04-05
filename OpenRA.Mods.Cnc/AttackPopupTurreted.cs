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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.Cnc
{
	class AttackPopupTurretedInfo : AttackBaseInfo
	{
		public int CloseDelay = 125;
		public int DefaultFacing = 0;
		public float ClosedDamageMultiplier = 0.5f;
		public override object Create(ActorInitializer init) { return new AttackPopupTurreted( init, this ); }
	}

	class AttackPopupTurreted : AttackBase, INotifyBuildComplete, INotifyIdle, IDamageModifier
	{
		enum PopupState
		{
			Open,
			Rotating,
			Transitioning,
			Closed
		};
		
		protected Target target;
		AttackPopupTurretedInfo Info;
		Turreted Turret;
		int IdleTicks = 0;
		PopupState State = PopupState.Open;
		
		public AttackPopupTurreted(ActorInitializer init, AttackPopupTurretedInfo info) : base(init.self)
		{
			Info = info;
			Turret = init.self.Trait<Turreted>();
			if (init.Contains<SkipMakeAnimsInit>())
			{
				State = PopupState.Closed;
				init.self.Trait<RenderBuilding>()
					.PlayCustomAnimRepeating(init.self, "closed-idle");
				Turret.desiredFacing = null;
				buildComplete = true;
			}
		}

		protected override bool CanAttack( Actor self, Target target )
		{
			if (State == PopupState.Transitioning)
				return false;
			
			if( self.HasTrait<Building>() && !buildComplete )
				return false;
			
			if (!base.CanAttack( self, target ))
				return false;
			
			IdleTicks = 0;
			if (State == PopupState.Closed)
			{
				State = PopupState.Transitioning;
				var rb = self.Trait<RenderBuilding>();
				rb.PlayCustomAnimThen(self, "opening", () =>
				{
					State = PopupState.Open;
					rb.PlayCustomAnimRepeating(self, "idle");
				});
				return false;
			}
			
			Turret.desiredFacing = Util.GetFacing( target.CenterLocation - self.CenterLocation, Turret.turretFacing );
			if( Turret.desiredFacing != Turret.turretFacing )
				return false;

			return true;
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			DoAttack( self, target );
		}
		
		public void TickIdle(Actor self)
		{
			if (State == PopupState.Open && IdleTicks++ > Info.CloseDelay)
			{
				Turret.desiredFacing = Info.DefaultFacing;
				State = PopupState.Rotating;
			}
			else if (State == PopupState.Rotating && Turret.turretFacing == Info.DefaultFacing)
			{
				State = PopupState.Transitioning;
				var rb = self.Trait<RenderBuilding>();
				rb.PlayCustomAnimThen(self, "closing", () =>
				{
					State = PopupState.Closed;
					rb.PlayCustomAnimRepeating(self, "closed-idle");
					Turret.desiredFacing = null;
				});
			}
		}
		
		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			return new AttackActivity( newTarget );
		}

		public override void ResolveOrder(Actor self, Order order)
		{
			base.ResolveOrder(self, order);

			if (order.OrderString == "Stop")
				target = Target.None;
		}

		bool buildComplete = false;
		public void BuildingComplete(Actor self) { buildComplete = true; }
		
		public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			return State == PopupState.Closed ? Info.ClosedDamageMultiplier : 1f;
		}
		
		class AttackActivity : Activity
		{
			readonly Target target;
			public AttackActivity( Target newTarget ) { this.target = newTarget; }

			public override Activity Tick( Actor self )
			{
				if( IsCanceled || !target.IsValid ) return NextActivity;

				if (self.TraitsImplementing<IDisable>().Any(d => d.Disabled))
					return this;

				var attack = self.Trait<AttackPopupTurreted>();
				const int RangeTolerance = 1;	/* how far inside our maximum range we should try to sit */
				var weapon = attack.ChooseWeaponForTarget(target);
				if (weapon != null)
				{
					attack.target = target;

					if (self.HasTrait<Mobile>() && !self.Info.Traits.Get<MobileInfo>().OnRails)
						return Util.SequenceActivities(
							new Follow( target, Math.Max( 0, (int)weapon.Info.Range - RangeTolerance ) ),
							this );
				}
				return NextActivity;
			}
		}
	}
}
