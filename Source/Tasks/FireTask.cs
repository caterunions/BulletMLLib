﻿
using System;
using System.Diagnostics;
using System.Linq;

using UnityEditor.Experimental.GraphView;

using UnityEngine;
using UnityEngine.Rendering;

namespace BulletMLLib
{
	/// <summary>
	/// A task to shoot a bullet
	/// </summary>
	public class FireTask : BulletMLTask
	{
		#region Members

		/// <summary>
		/// The direction that this task will fire a bullet.
		/// </summary>
		/// <value>The fire direction.</value>
		public float FireDirection { get; private set; }
		public bool FaceDirection { get; private set; } = false;

		/// <summary>
		/// The speed that this task will fire a bullet.
		/// </summary>
		/// <value>The fire speed.</value>
		public float FireSpeed { get; private set; }

		public ElementType ElementType { get; private set; } = ElementType.Neutral;

		public float Lifetime { get; private set; }

		protected BulletMLEquation NodeEquation = new BulletMLEquation();

		public float StoredXOffset { get; private set; } = 0;
		public float StoredYOffset { get; private set; } = 0;
		public bool AbsoluteOffset = false;
		public float ContinuousRotation { get; private set; } = 0;
		private bool _didWeAim = false;

		private float _frequency = 0;
		private float _amplitude = 0;

		public string Visuals { get; private set; } = "";

		/// <summary>
		/// The number of times init has been called on this task
		/// </summary>
		/// <value>The number times initialized.</value>
		public int NumTimesInitialized { get; private set; }

		/// <summary>
		/// Flag used to tell if this is the first time this task has been run
		/// Used to determine if we should use the "initial" or "sequence" nodes to set bullets.
		/// </summary>
		/// <value><c>true</c> if initial run; otherwise, <c>false</c>.</value>
		public bool InitialRun
		{
			get
			{
				return NumTimesInitialized <= 0;
			}
		}

		/// <summary>
		/// If this fire node shoots from a bullet ref node, this will be a task created for it.
		/// This is needed so the params of the bullet ref can be set correctly.
		/// </summary>
		/// <value>The bullet reference task.</value>
		public BulletMLTask BulletRefTask { get; private set; }

		/// <summary>
		/// The node we are going to use to set the direction of any bullets shot with this task
		/// </summary>
		/// <value>The dir node.</value>
		public SetDirectionTask InitialDirectionTask { get; private set; }

		/// <summary>
		/// The node we are going to use to set the speed of any bullets shot with this task
		/// </summary>
		/// <value>The speed node.</value>
		public SetSpeedTask InitialSpeedTask { get; private set; }

		public SetOffsetXTask InitialOffsetXTask { get; private set; }
		public SetOffsetYTask InitialOffsetYTask { get; private set; }
		/// <summary>
		/// If there is a sequence direction node used to increment the direction of each successive bullet that is fired
		/// </summary>
		/// <value>The sequence direction node.</value>
		public SetDirectionTask SequenceDirectionTask { get; private set; }

		/// <summary>
		/// If there is a sequence direction node used to increment the direction of each successive bullet that is fired
		/// </summary>
		/// <value>The sequence direction node.</value>
		public SetSpeedTask SequenceSpeedTask { get; private set; }

		#endregion //Members

		#region Methods

		/// <summary>
		/// Initializes a new instance of the <see cref="BulletMLLib.FireTask"/> class.
		/// </summary>
		/// <param name="node">Node.</param>
		/// <param name="owner">Owner.</param>
		public FireTask(FireNode node, BulletMLTask owner) : base(node, owner)
		{
			System.Diagnostics.Debug.Assert(null != Node);
			System.Diagnostics.Debug.Assert(null != Owner);

			NumTimesInitialized = 0;
		}

		/// <summary>
		/// Parse a specified node and bullet into this task
		/// </summary>
		/// <param name="myNode">the node for this dude</param>
		/// <param name="bullet">the bullet this dude is controlling</param>
		public override void ParseTasks(Bullet bullet)
		{
			if (null == bullet)
			{
				throw new NullReferenceException("bullet argument cannot be null");
			}

			foreach (BulletMLNode childNode in Node.ChildNodes)
			{
				ParseChildNode(childNode, bullet);
			}

			//Setup all the direction nodes
			GetDirectionTasks(this);
			GetDirectionTasks(BulletRefTask);

			//setup all the speed nodes
			GetSpeedNodes(this);
			GetSpeedNodes(BulletRefTask);

			GetElemNode(this);

			GetLifetimeNode(this);

			GetOffsetNodes(this);

			GetFaceDirectionNode(this);

			GetContinuousRotationNode(this);

			GetVisualsNode(this);

			GetSineNodes(this);
		}

		/// <summary>
		/// Parse a specified node and bullet into this task
		/// </summary>
		/// <param name="myNode">the node for this dude</param>
		/// <param name="bullet">the bullet this dude is controlling</param>
		public override void ParseChildNode(BulletMLNode childNode, Bullet bullet)
		{
			System.Diagnostics.Debug.Assert(null != childNode);
			System.Diagnostics.Debug.Assert(null != bullet);

			switch (childNode.Name)
			{
				case ENodeName.bulletRef:
					{
						//Create a task for the bullet ref 
						BulletRefNode refNode = childNode as BulletRefNode;
						BulletRefTask = new BulletMLTask(refNode.ReferencedBulletNode, this);

						//populate the params of the bullet ref
						for (int i = 0; i < childNode.ChildNodes.Count; i++)
						{
							BulletRefTask.ParamList.Add(childNode.ChildNodes[i].GetValue(this));
						}

						BulletRefTask.ParseTasks(bullet);
						ChildTasks.Add(BulletRefTask);
					}
					break;

				case ENodeName.bullet:
					{
						//Create a task for the bullet ref 
						BulletRefTask = new BulletMLTask(childNode, this);
						BulletRefTask.ParseTasks(bullet);
						ChildTasks.Add(BulletRefTask);
					}
					break;

				default:
					{
						//run the node through the base class if we don't want it
						base.ParseChildNode(childNode, bullet);
					}
					break;
			}
		}

		/// <summary>
		/// This gets called when nested repeat nodes get initialized.
		/// </summary>
		/// <param name="bullet">Bullet.</param>
		public override void HardReset(Bullet bullet)
		{
			//This is the whole point of the hard reset, so the sequence nodes get reset.
			NumTimesInitialized = 0;

			base.HardReset(bullet);
		}

		/// <summary>
		/// this sets up the task to be run.
		/// </summary>
		/// <param name="bullet">Bullet.</param>
		protected override void SetupTask(Bullet bullet)
		{
			//offset land
			if (InitialOffsetXTask != null)
			{
				StoredXOffset = InitialOffsetXTask.GetNodeValue();
			}

			if(InitialOffsetYTask != null)
			{
				StoredYOffset = InitialOffsetYTask.GetNodeValue();
			}

			//get the direction to shoot the bullet

			//is this the first time it has ran?  If there isn't a sequence node, we don't care!
			if (InitialRun || (null == SequenceDirectionTask))
			{
				//do we have an initial direction node?
				if (null != InitialDirectionTask)
				{
					//Set the fire direction to the "initial" value
					float newBulletDirection = InitialDirectionTask.GetNodeValue() * (float)Mathf.PI / 180.0f;
					switch (InitialDirectionTask.Node.NodeType)
					{
						case ENodeType.absolute:
							{
								//the new bullet points right at a particular direction
								FireDirection = newBulletDirection;
							}
							break;

						case ENodeType.relative:
							{
								//the new bullet direction will be relative to the old bullet
								FireDirection = newBulletDirection + bullet.Direction;
							}
							break;

						default:
							{
								//aim the bullet at the player
								_didWeAim = true;
							}
							break;
					}
				}
				else
				{
					//There isn't an initial direction task, so just aim at the bad guy.
					//aim the bullet at the player
					_didWeAim = true;
				}
			}
			else if (null != SequenceDirectionTask)
			{
				//else if there is a sequence node, add the value to the "shoot direction"
				FireDirection += SequenceDirectionTask.GetNodeValue() * (float)Mathf.PI / 180.0f;
			}

			//Set the speed to shoot the bullet

			//is this the first time it has ran?  If there isn't a sequence node, we don't care!
			if (InitialRun || (null == SequenceSpeedTask))
			{
				//do we have an initial speed node?
				if (null != InitialSpeedTask)
				{
					//set the shoot speed to the "initial" value.
					float newBulletSpeed = InitialSpeedTask.GetNodeValue();
					switch (InitialSpeedTask.Node.NodeType)
					{
						case ENodeType.relative:
							{
								//the new bullet speed will be relative to the old bullet
								FireSpeed = newBulletSpeed + bullet.Speed;
							}
							break;

						default:
							{
								//the new bullet shoots at a predeterminde speed
								FireSpeed = newBulletSpeed;
							}
							break;
					}
				}
				else
				{
					//there is no initial speed task, use the old dude's speed
					FireSpeed = bullet.Speed;
				}
			}
			else if (null != SequenceSpeedTask)
			{
				//else if there is a sequence node, add the value to the "shoot direction"
				FireSpeed += SequenceSpeedTask.GetNodeValue();
			}

			//make sure the direction is between 0 and 359
			while ((2.0f * Mathf.PI) <= FireDirection)
			{
				FireDirection -= (2.0f * (float)Mathf.PI);
			}
			while (0.0f > FireDirection)
			{
				FireDirection += (2.0f * (float)Mathf.PI);
			}

			//make sure we don't overwrite the initial values if we aren't supposed to
			NumTimesInitialized++;
		}

		/// <summary>
		/// Run this task and all subtasks against a bullet
		/// This is called once a frame during runtime.
		/// </summary>
		/// <returns>ERunStatus: whether this task is done, paused, or still running</returns>
		/// <param name="bullet">The bullet to update this task against.</param>
		public override ERunStatus Run(Bullet bullet)
		{
			//Create the new bullet
			Bullet newBullet = bullet.MyBulletManager.CreateBullet(bullet, false);

			if (newBullet == null)
			{
				//wtf did you do???
				TaskFinished = true;
				return ERunStatus.End;
			}

			if (InitialOffsetXTask == null)
			{
				newBullet.X = bullet.X;
			}
			else
			{
				switch(InitialOffsetXTask.Node.NodeType)
				{
					case ENodeType.relative:
						{
							newBullet.X = bullet.X + StoredXOffset;
							break;
						}
					default:
						{
							newBullet.X = StoredXOffset;
							break;
						}
				}
			}

			if(InitialOffsetYTask == null)
			{
				newBullet.Y = bullet.Y;
			}
			else
			{
				switch(InitialOffsetYTask.Node.NodeType)
				{
					case ENodeType.relative:
						{
							newBullet.Y = bullet.Y + StoredYOffset;
							break;
						}
					default:
						{
							newBullet.Y = StoredYOffset;
							break;
						}
				}
			}

			newBullet.SpawnPos = new Vector2(newBullet.X, newBullet.Y);

			//set the direction of the new bullet

			if (_didWeAim)
			{
				newBullet.Direction = newBullet.GetAimDir();
			}
			else
			{
				newBullet.Direction = FireDirection;
			}
			newBullet.VisualDirection = newBullet.Direction;

			//set teh speed of the new bullet
			newBullet.Speed = FireSpeed;

			newBullet.ElementType = ElementType;

			newBullet.Lifetime = Lifetime;

			newBullet.FaceDirection = FaceDirection;

			newBullet.ContinousRotation = ContinuousRotation;

			newBullet.Visuals = Visuals;

			newBullet.Frequency = _frequency;

			newBullet.Amplitude = _amplitude;

			//initialize the bullet with the bullet node stored in the Fire node
			FireNode myFireNode = Node as FireNode;
			System.Diagnostics.Debug.Assert(null != myFireNode);

			newBullet.InitNode(myFireNode.BulletDescriptionNode);

			// Let the bullet handler initialize the bullet ingame data
			//newBullet.InitBullet();

			//set the owner of all the top level tasks for the new bullet to this dude
			foreach (BulletMLTask task in newBullet.Tasks)
			{
				task.Owner = this;
			}

			TaskFinished = true;
			return ERunStatus.End;
		}

		/// <summary>
		/// Given a node, pull the direction nodes out from underneath it and store them if necessary
		/// </summary>
		/// <param name="taskToCheck">task to check if has a child direction node.</param>
		private void GetDirectionTasks(BulletMLTask taskToCheck)
		{
			if (null == taskToCheck)
			{
				return;
			}

			//check if the dude has a direction node
			DirectionNode dirNode = taskToCheck.Node.GetChild(ENodeName.direction) as DirectionNode;
			if (null != dirNode)
			{
				//check if it is a sequence type of node
				if (ENodeType.sequence == dirNode.NodeType)
				{
					//do we need a sequence node?
					if (null == SequenceDirectionTask)
					{
						//store it in the sequence direction node
						SequenceDirectionTask = new SetDirectionTask(dirNode as DirectionNode, taskToCheck);
					}
				}
				else
				{
					//else do we need an initial node?
					if (null == InitialDirectionTask)
					{
						//store it in the initial direction node
						InitialDirectionTask = new SetDirectionTask(dirNode as DirectionNode, taskToCheck);
					}
				}
			}
		}

		/// <summary>
		/// Given a node, pull the speed nodes out from underneath it and store them if necessary
		/// </summary>
		/// <param name="nodeToCheck">Node to check.</param>
		private void GetSpeedNodes(BulletMLTask taskToCheck)
		{
			if (null == taskToCheck)
			{
				return;
			}

			//check if the dude has a speed node
			SpeedNode spdNode = taskToCheck.Node.GetChild(ENodeName.speed) as SpeedNode;
			if (null != spdNode)
			{
				//check if it is a sequence type of node
				if (ENodeType.sequence == spdNode.NodeType)
				{
					//do we need a sequence node?
					if (null == SequenceSpeedTask)
					{
						//store it in the sequence speed node
						SequenceSpeedTask = new SetSpeedTask(spdNode as SpeedNode, taskToCheck);
					}
				}
				else
				{
					//else do we need an initial node?
					if (null == InitialSpeedTask)
					{
						//store it in the initial speed node
						InitialSpeedTask = new SetSpeedTask(spdNode as SpeedNode, taskToCheck);
					}
				}
			}
		}

		private void GetElemNode(BulletMLTask taskToCheck)
		{
			if (taskToCheck == null) return;

			ElemNode elemNode = taskToCheck.Node.GetChild(ENodeName.elem) as ElemNode;
			if (elemNode == null)
			{
				//it didnt work GO STUPID
				FireTask fireTask = taskToCheck as FireTask;
				if (fireTask != null)
				{
					elemNode = fireTask.BulletRefTask.Node.ChildNodes.FirstOrDefault(n => n as ElemNode != null) as ElemNode;
				}
			}

			if (elemNode != null)
			{
				bool parsed = Enum.TryParse(typeof(ElementType), elemNode.Text, true, out object result);
				if (parsed)
				{
					ElementType = (ElementType)result;
				}
			}
		}

		private void GetLifetimeNode(BulletMLTask taskToCheck)
		{
			if (taskToCheck == null) return;

			LifetimeNode lifetimeNode = taskToCheck.Node.GetChild(ENodeName.lifetime) as LifetimeNode;
			if (lifetimeNode == null)
			{
				//it didnt work GO STUPID
				FireTask fireTask = taskToCheck as FireTask;
				if (fireTask != null)
				{
					lifetimeNode = fireTask.BulletRefTask.Node.ChildNodes.FirstOrDefault(n => n as LifetimeNode != null) as LifetimeNode;
				}
			}

			if (lifetimeNode != null)
			{
				NodeEquation.Parse(lifetimeNode.Text);
				Lifetime = NodeEquation.Solve(null);
			}
		}

		private void GetOffsetNodes(BulletMLTask taskToCheck)
		{
			if (taskToCheck == null) return;

			OffsetXNode offsetXNode = taskToCheck.Node.GetChild(ENodeName.offsetX) as OffsetXNode;
			if (offsetXNode == null)
			{
				//it didnt work GO STUPID
				FireTask fireTask = taskToCheck as FireTask;
				if (fireTask != null)
				{
					offsetXNode = fireTask.BulletRefTask.Node.ChildNodes.FirstOrDefault(n => n as OffsetXNode != null) as OffsetXNode;
				}
			}

			if (offsetXNode != null)
			{
				InitialOffsetXTask = new SetOffsetXTask(offsetXNode, taskToCheck);
			}

			// Y
			OffsetYNode offsetYNode = taskToCheck.Node.GetChild(ENodeName.offsetY) as OffsetYNode;
			if (offsetYNode == null)
			{
				//it didnt work GO STUPID
				FireTask fireTask = taskToCheck as FireTask;
				if (fireTask != null)
				{
					offsetYNode = fireTask.BulletRefTask.Node.ChildNodes.FirstOrDefault(n => n as OffsetYNode != null) as OffsetYNode;
				}
			}

			if (offsetYNode != null)
			{
				InitialOffsetYTask = new SetOffsetYTask(offsetYNode, taskToCheck);
			}
		}

		private void GetFaceDirectionNode(BulletMLTask taskToCheck)
		{
			if (taskToCheck == null) return;

			FaceHeadingNode dirNode = taskToCheck.Node.GetChild(ENodeName.faceHeading) as FaceHeadingNode;
			if (dirNode == null)
			{
				//it didnt work GO STUPID
				FireTask fireTask = taskToCheck as FireTask;
				if (fireTask != null)
				{
					dirNode = fireTask.BulletRefTask.Node.ChildNodes.FirstOrDefault(n => n as FaceHeadingNode != null) as FaceHeadingNode;
				}
			}

			if (dirNode != null)
			{
				FaceDirection = true;
			}
		}

		private void GetContinuousRotationNode(BulletMLTask taskToCheck)
		{
			if (taskToCheck == null) return;

			ContinuousRotationNode rotNode = taskToCheck.Node.GetChild(ENodeName.continuousRotation) as ContinuousRotationNode;
			if (rotNode == null)
			{
				//it didnt work GO STUPID
				FireTask fireTask = taskToCheck as FireTask;
				if (fireTask != null)
				{
					rotNode = fireTask.BulletRefTask.Node.ChildNodes.FirstOrDefault(n => n as ContinuousRotationNode != null) as ContinuousRotationNode;
				}
			}

			if (rotNode != null)
			{
				NodeEquation.Parse(rotNode.Text);
				ContinuousRotation = NodeEquation.Solve(null);
			}
		}

		private void GetVisualsNode(BulletMLTask taskToCheck)
		{
			if (taskToCheck == null) return;

			VisualsNode visNode = taskToCheck.Node.GetChild(ENodeName.visuals) as VisualsNode;
			if (visNode == null)
			{
				//it didnt work GO STUPID
				FireTask fireTask = taskToCheck as FireTask;
				if (fireTask != null)
				{
					visNode = fireTask.BulletRefTask.Node.ChildNodes.FirstOrDefault(n => n as VisualsNode != null) as VisualsNode;
				}
			}

			if (visNode != null)
			{
				Visuals = visNode.Text;
			}
		}

		public void GetSineNodes(BulletMLTask taskToCheck)
		{
			if (taskToCheck == null) return;

			SineNode sineNode = taskToCheck.Node.GetChild(ENodeName.sine) as SineNode;
			if (sineNode == null)
			{
				//it didnt work GO STUPID
				FireTask fireTask = taskToCheck as FireTask;
				if (fireTask != null)
				{
					sineNode = fireTask.BulletRefTask.Node.ChildNodes.FirstOrDefault(n => n as SineNode != null) as SineNode;
				}
			}

			if(sineNode != null)
			{
				FrequencyNode freqNode = sineNode.GetChild(ENodeName.frequency) as FrequencyNode;
				if(freqNode != null)
				{
					NodeEquation.Parse(freqNode.Text);
					_frequency = NodeEquation.Solve(null);
				}
				else
				{
					_frequency = 1;
				}

				AmplitudeNode ampNode = sineNode.GetChild(ENodeName.amplitude) as AmplitudeNode;
				if (ampNode != null)
				{
					NodeEquation.Parse(ampNode.Text);
					_amplitude = NodeEquation.Solve(null);
				}
				else
				{
					_amplitude = 1;
				}
			}
		}

		#endregion //Methods
	}
}