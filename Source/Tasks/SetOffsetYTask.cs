﻿using System.Diagnostics;

namespace BulletMLLib
{
	/// <summary>
	/// This action sets the velocity of a bullet
	/// </summary>
	public class SetOffsetYTask : BulletMLTask
	{
		#region Methods

		/// <summary>
		/// Initializes a new instance of the <see cref="BulletMLLib.BulletMLTask"/> class.
		/// </summary>
		/// <param name="node">Node.</param>
		/// <param name="owner">Owner.</param>
		public SetOffsetYTask(OffsetYNode node, BulletMLTask owner) : base(node, owner)
		{
			System.Diagnostics.Debug.Assert(null != Node);
			System.Diagnostics.Debug.Assert(null != Owner);
		}

		#endregion //Methods
	}
}