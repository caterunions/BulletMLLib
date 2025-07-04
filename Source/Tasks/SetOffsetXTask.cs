﻿using System.Diagnostics;

namespace BulletMLLib
{
	/// <summary>
	/// This action sets the velocity of a bullet
	/// </summary>
	public class SetOffsetXTask : BulletMLTask
	{
		#region Methods

		/// <summary>
		/// Initializes a new instance of the <see cref="BulletMLLib.BulletMLTask"/> class.
		/// </summary>
		/// <param name="node">Node.</param>
		/// <param name="owner">Owner.</param>
		public SetOffsetXTask(OffsetXNode node, BulletMLTask owner) : base(node, owner)
		{
			System.Diagnostics.Debug.Assert(null != Node);
			System.Diagnostics.Debug.Assert(null != Owner);
		}

		#endregion //Methods
	}
}