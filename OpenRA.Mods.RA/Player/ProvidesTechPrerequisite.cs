#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ProvidesTechPrerequisiteInfo : ITraitInfo, ITechTreePrerequisiteInfo
	{
		public readonly string Name;
		public readonly string[] Prerequisite;

		public object Create(ActorInitializer init) { return new ProvidesTechPrerequisite(this, init); }

		public string[] Prerequisites
		{
			get { return Prerequisite; }
		}
	}

	public class ProvidesTechPrerequisite : ITechTreePrerequisite
	{
		ProvidesTechPrerequisiteInfo info;
		bool enabled;

		public string Name { get { return info.Name; } }

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				if (enabled)
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
		}

		public ProvidesTechPrerequisite(ProvidesTechPrerequisiteInfo info, ActorInitializer init)
		{
			this.info = info;
			this.enabled = info.Name == init.world.LobbyInfo.GlobalSettings.TechLevel;
		}
	}
}
