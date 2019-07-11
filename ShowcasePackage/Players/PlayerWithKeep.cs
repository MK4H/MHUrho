using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using ShowcasePackage.Buildings;

namespace ShowcasePackage.Players
{
	public abstract class PlayerWithKeep : PlayerAIInstancePlugin
	{
		public  Keep Keep { get; protected set; }

		KeepType keepType;

		protected PlayerWithKeep(ILevelManager level, IPlayer player, KeepType keepType)
			: base(level, player)
		{
			this.keepType = keepType;
		}


		/// <summary>
		/// Gets players keep and checks there is really only one.
		/// </summary>
		/// <returns>The players keep.</returns>
		/// <exception cref="LevelLoadingException">Thrown when there is invalid number of keeps.</exception>
		protected Keep GetKeep()
		{
			IReadOnlyList<IBuilding> keeps = Player.GetBuildingsOfType(keepType.MyTypeInstance);
			if (keeps.Count != 1)
			{
				throw new LevelLoadingException("Player is missing a keep.");
			}
			return (Keep)keeps[0].Plugin;
		}
	}
}
