using System.Diagnostics;

using UnityEngine;

namespace BulletMLLib
{
	/// <summary>
	/// This action sets the velocity of a bullet
	/// </summary>
	public class SetRotateTask : BulletMLTask
	{
		#region Methods

		/// <summary>
		/// Initializes a new instance of the <see cref="BulletMLLib.BulletMLTask"/> class.
		/// </summary>
		/// <param name="node">Node.</param>
		/// <param name="owner">Owner.</param>
		public SetRotateTask(RotateNode node, BulletMLTask owner) : base(node, owner)
		{
			System.Diagnostics.Debug.Assert(null != Node);
			System.Diagnostics.Debug.Assert(null != Owner);
		}

		public override ERunStatus Run(MLBullet bullet)
		{
			OriginXNode originXNode = Node.GetChild(ENodeName.originX) as OriginXNode;
			OriginYNode originYNode = Node.GetChild(ENodeName.originY) as OriginYNode;
			RotRateNode rotRateNode = Node.GetChild(ENodeName.rotRate) as RotRateNode;

			float originX = originXNode == null ? bullet.SpawnPos.x : originXNode.GetValue(this);

			float originY = originYNode == null ? bullet.SpawnPos.y : originYNode.GetValue(this);

			bullet.RotateOrigin = new Vector2(originX, originY);
			bullet.RotationRate = rotRateNode.GetValue(this);

			return ERunStatus.End;
		}

		#endregion //Methods
	}
}