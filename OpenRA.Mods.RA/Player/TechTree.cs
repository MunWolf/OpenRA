#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class TechTreeInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new TechTree(init); }
	}

	public class TechTree
	{
		readonly List<Watcher> watchers = new List<Watcher>();
		readonly Player player;

		public TechTree(ActorInitializer init)
		{
			player = init.self.Owner;
			init.world.ActorAdded += ActorChanged;
			init.world.ActorRemoved += ActorChanged;
		}

		public void ActorChanged(Actor a)
		{
			var bi = a.Info.Traits.GetOrDefault<BuildableInfo>();
			if (a.Owner == player && (a.HasTrait<ITechTreePrerequisite>() || (bi != null && bi.BuildLimit > 0)))
				Update();
		}

		public void Update()
		{
			var buildables = GatherBuildables(player);
			foreach (var w in watchers)
				w.Update(buildables);
		}

		public void Add(string key, BuildableInfo info, ITechTreeElement tte)
		{
			watchers.Add(new Watcher(key, info, tte));
		}

		public void Remove(string key)
		{
			watchers.RemoveAll(x => x.Key == key);
		}

		static Cache<string, List<Actor>> GatherBuildables(Player player)
		{
			var ret = new Cache<string, List<Actor>>(x => new List<Actor>());
			if (player == null)
				return ret;

			// Add buildables that provide prerequisites
			var prereqs = player.World.ActorsWithTrait<ITechTreePrerequisite>()
				.Where(a => a.Actor.Owner == player && !a.Actor.IsDead() && a.Actor.IsInWorld);

			foreach (var b in prereqs)
			{
				foreach (var p in b.Trait.ProvidesPrerequisites)
				{
					// Ignore bogus prerequisites
					if (p == null)
						continue;

					ret[p].Add(b.Actor);
				}
			}

			// Add buildables that have a build limit set and are not already in the list
			player.World.ActorsWithTrait<Buildable>()
				  .Where(a => a.Actor.Info.Traits.Get<BuildableInfo>().BuildLimit > 0 && !a.Actor.IsDead() && a.Actor.Owner == player && ret.Keys.All(k => k != a.Actor.Info.Name))
				  .ToList()
				  .ForEach(b => ret[b.Actor.Info.Name].Add(b.Actor));

			return ret;
		}

		class Watcher
		{
			public readonly string Key;

			// Strings may be either actor type, or "alternate name" key
			readonly string[] prerequisites;
			readonly ITechTreeElement watcher;
			bool hasPrerequisites;
			int buildLimit;
			bool hidden;

			public Watcher(string key, BuildableInfo info, ITechTreeElement watcher)
			{
				this.Key = key;
				this.prerequisites = info.Prerequisites;
				this.watcher = watcher;
				this.hasPrerequisites = false;
				this.buildLimit = info.BuildLimit;
				this.hidden = info.Hidden;
			}

			bool HasPrerequisites(Cache<string, List<Actor>> buildables)
			{
				return prerequisites.All(p => !(p.Contains("!") ^ !buildables.Keys.Contains(p.Replace("!", "").Replace("~", ""))));
			}

			bool isHidden(Cache<string, List<Actor>> buildables)
			{
				return prerequisites.Any(p => (p.Contains("!") && p.Contains("~")) ^ (p.Contains("~") && !buildables.Keys.Contains(p.Replace("~", "").Replace("!", ""))));
			}

			public void Update(Cache<string, List<Actor>> buildables)
			{
				var hasReachedBuildLimit = buildLimit > 0 && buildables[Key].Count >= buildLimit;
				// Checks for prerequsites, the ! annotation in those prereqs is a not operator aka if the player does not have an ore refinery the condition is met
				var nowHasPrerequisites = HasPrerequisites(buildables) && !hasReachedBuildLimit;
				var nowHidden = isHidden(buildables);

				// Checks if the prereq has the ~ annotation and if those prereqs are availible, if not it hides the item from the buildqueue
				if (nowHidden != hidden)
					watcher.PrerequisitesHidden(Key, hidden);

				if (nowHasPrerequisites && !hasPrerequisites)
					watcher.PrerequisitesAvailable(Key);

				if (!nowHasPrerequisites && hasPrerequisites)
					watcher.PrerequisitesUnavailable(Key);

				hidden = nowHidden;
				hasPrerequisites = nowHasPrerequisites;
			}
		}
	}

	public class ProvidesCustomPrerequisiteInfo : ITraitInfo, IPrerequisiteProvider
	{
		public readonly string[] Prerequisites;

		public object Create(ActorInitializer init) { return new ProvidesCustomPrerequisite(this); }

		public string[] Prerequisite
		{
			get { return Prerequisites; }
		}
	}

	public class ProvidesCustomPrerequisite : ITechTreePrerequisite
	{
		ProvidesCustomPrerequisiteInfo info;

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				if (info.Prerequisites == null)
					yield return null;
				else
				{
					foreach (var Prerequisite in info.Prerequisites)
					{
						yield return Prerequisite;
					}
				}
			}
		}

		public ProvidesCustomPrerequisite(ProvidesCustomPrerequisiteInfo info)
		{
			this.info = info;
		}
	}
}
