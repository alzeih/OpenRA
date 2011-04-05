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
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Air
{
	class HelicopterInfo : AircraftInfo
	{
		public readonly int IdealSeparation = 40;
		public readonly bool LandWhenIdle = true;

		public override object Create( ActorInitializer init ) { return new Helicopter( init, this); }
	}

	class Helicopter : Aircraft, ITick, IIssueOrder, IResolveOrder, IOrderVoice
	{
		HelicopterInfo Info;

		public Helicopter( ActorInitializer init, HelicopterInfo info) : base( init, info ) 
		{
			Info = info;
		}
		
		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterOrderTargeter<Building>( "Enter", 5, false, true,
					target => AircraftCanEnter( target ), target => !Reservable.IsReserved( target ) );

				yield return new AircraftMoveOrderTargeter();
			}
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "Enter" )
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			if( order.OrderID == "Move" )
				return new Order(order.OrderID, self, queued) { TargetLocation = Util.CellContaining(target.CenterLocation) };

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Move" || order.OrderString == "Enter") ? "Move" : null;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (reservation != null)
			{
				reservation.Dispose();
				reservation = null;
			}

			if (order.OrderString == "Move")
			{
				var target = self.World.ClampToWorld(order.TargetLocation);
				
				self.SetTargetLine(Target.FromCell(target), Color.Green);
				self.CancelActivity();
				self.QueueActivity(new HeliFly(Util.CenterOfCell(target)));
					
				if (Info.LandWhenIdle)
				{
					self.QueueActivity(new Turn(Info.InitialFacing));
					self.QueueActivity(new HeliLand(true));
				}
			}

			if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor)) return;
				var res = order.TargetActor.TraitOrDefault<Reservable>();
				if (res != null)
					reservation = res.Reserve(order.TargetActor, self, this);

				var exit = order.TargetActor.Info.Traits.WithInterface<ExitInfo>().FirstOrDefault();
				var offset = exit != null ? exit.SpawnOffset : int2.Zero;
				
				self.SetTargetLine(Target.FromActor(order.TargetActor), Color.Green);
				
				self.CancelActivity();
				self.QueueActivity(new HeliFly(order.TargetActor.Trait<IHasLocation>().PxPosition + offset));
				self.QueueActivity(new Turn(Info.InitialFacing));
				self.QueueActivity(new HeliLand(false));
				self.QueueActivity(Info.RearmBuildings.Contains(order.TargetActor.Info.Name)
					? (Activity)new Rearm() : new Repair(order.TargetActor));
			}

			if (order.OrderString == "Stop")
			{
				self.CancelActivity();

				if (Info.LandWhenIdle)
				{
					self.QueueActivity(new Turn(Info.InitialFacing));
					self.QueueActivity(new HeliLand(true));
				}
			}
		}
		
		public void Tick(Actor self)
		{
			var aircraft = self.Trait<Aircraft>();
			if (aircraft.Altitude <= 0)
				return;
			
			var otherHelis = self.World.FindUnitsInCircle(self.CenterLocation, Info.IdealSeparation)
				.Where(a => a.HasTrait<Helicopter>());

			var f = otherHelis
				.Select(h => GetRepulseForce(self, h))
				.Aggregate(int2.Zero, (a, b) => a + b);

			int RepulsionFacing = Util.GetFacing( f, -1 );
			if( RepulsionFacing != -1 )
				aircraft.TickMove( 1024 * aircraft.MovementSpeed, RepulsionFacing );
		}

		// Returns an int2 in subPx units
		public int2 GetRepulseForce(Actor self, Actor h)
		{
			if (self == h)
				return int2.Zero;
			if( h.Trait<Helicopter>().Altitude < Altitude )
				return int2.Zero;
			var d = self.CenterLocation - h.CenterLocation;
			
			if (d.Length > Info.IdealSeparation)
				return int2.Zero;

			if (d.LengthSquared < 1)
				return Util.SubPxVector[self.World.SharedRandom.Next(255)];
			return (5120 / d.LengthSquared) * d;
		}
	}
}
