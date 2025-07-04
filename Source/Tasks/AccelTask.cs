﻿using UnityEngine;
using System.Diagnostics;

namespace BulletMLLib
{
	/// <summary>
	/// This task adds acceleration to a bullet.
	/// </summary>
	public class AccelTask : BulletMLTask
	{
		#region Members

		/// <summary>
		/// How long to run this task... measured in frames
		/// </summary>
		public float Duration { get; private set; }

		private float startDuration;

		/// <summary>
		/// The direction to accelerate in 
		/// </summary>
		private Vector2 _acceleration = Vector2.zero;

		/// <summary>
		/// Gets or sets the acceleration.
		/// </summary>
		/// <value>The acceleration.</value>
		public Vector2 Acceleration
		{
			get
			{
				return _acceleration;
			}
			private set
			{
				_acceleration = value;
			}
		}

		private Vector2 _startAcceleration;

		#endregion //Members

		#region Methods

		/// <summary>
		/// Initializes a new instance of the <see cref="BulletMLLib.BulletMLTask"/> class.
		/// </summary>
		/// <param name="node">Node.</param>
		/// <param name="owner">Owner.</param>
		public AccelTask(AccelNode node, BulletMLTask owner) : base(node, owner)
		{
			System.Diagnostics.Debug.Assert(null != Node);
			System.Diagnostics.Debug.Assert(null != Owner);
		}

		/// <summary>
		/// this sets up the task to be run.
		/// </summary>
		/// <param name="bullet">Bullet.</param>
		protected override void SetupTask(Bullet bullet)
		{
			//set the accelerataion we are gonna add to the bullet
			startDuration = Node.GetChildValue(ENodeName.term, this) / 1000;

			_startAcceleration = bullet.Acceleration;

			//check for divide by 0
			if (0.0f == startDuration)
			{
				startDuration = Time.deltaTime;
			}
			Duration = startDuration;

			//Get the horizontal node
			HorizontalNode horiz = Node.GetChild(ENodeName.horizontal) as HorizontalNode;
			if (null != horiz)
			{
				//Set the x component of the acceleration
				switch (horiz.NodeType)
				{
					case ENodeType.sequence:
						{
							//Sequence in an acceleration node means "add this amount every frame"
							_acceleration.x = horiz.GetValue(this);
						}
						break;

					case ENodeType.relative:
						{
							//accelerate by a certain amount
							_acceleration.x = horiz.GetValue(this);
						}
						break;

					default:
						{
							//accelerate to a specific value
							_acceleration.x = (horiz.GetValue(this) - bullet.Acceleration.x);
						}
						break;
				}
			}

			//Get the vertical node
			VerticalNode vert = Node.GetChild(ENodeName.vertical) as VerticalNode;
			if (null != vert)
			{
				//set teh y component of the acceleration
				switch (vert.NodeType)
				{
					case ENodeType.sequence:
						{
							//Sequence in an acceleration node means "add this amount every frame"
							_acceleration.y = vert.GetValue(this);
						}
						break;

					case ENodeType.relative:
						{
							//accelerate by a certain amount
							_acceleration.y = vert.GetValue(this);
						}
						break;

					default:
						{
							//accelerate to a specific value
							_acceleration.y = (vert.GetValue(this) - bullet.Acceleration.y);
						}
						break;
				}
			}
		}

		/// <summary>
		/// Run this task and all subtasks against a bullet
		/// This is called once a frame during runtime.
		/// </summary>
		/// <returns>ERunStatus: whether this task is done, paused, or still running</returns>
		/// <param name="bullet">The bullet to update this task against.</param>
		public override ERunStatus Run(Bullet bullet)
		{
			//Add the acceleration to the bullet
			bullet.Acceleration = Vector2.Lerp(Acceleration, _startAcceleration, (startDuration - Duration) / startDuration);

			//decrement the amount if time left to run and return End when this task is finished
			Duration -= bullet.TimeSpeed * Time.deltaTime;
			if (Duration <= 0.0f)
			{
				TaskFinished = true;
				return ERunStatus.End;
			}
			else
			{
				//since this task isn't finished, run it again next time
				return ERunStatus.Continue;
			}
		}

		#endregion //Methods
	}
}