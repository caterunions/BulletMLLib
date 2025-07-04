using System;
using Equationator;
using System.Diagnostics;

namespace BulletMLLib
{
	/// <summary>
	/// This is an equation used in BulletML nodes.
	/// This is an eays way to set up the grammar for all our equations.
	/// </summary>
	public class BulletMLEquation : Equation
	{
		/// <summary>
		/// A randomizer for getting random values
		/// </summary>
		static private Random g_Random = new Random(DateTime.Now.Millisecond);

		public BulletMLEquation()
		{
			//add the specific functions we will use for bulletml grammar
			AddFunction("rand", RandomValue);
			AddFunction("plrX", PlayerX);
			AddFunction("plrY", PlayerY);
			AddFunction("rRng", RoundedRandom);
		}

		/// <summary>
		/// used as a callback function in bulletml euqations
		/// </summary>
		/// <returns>The value.</returns>
		public float RandomValue()
		{
			//this value is "$rand", return a random number
			return (float)g_Random.NextDouble();
		}

		public float RoundedRandom()
		{
			if(g_Random.Next(2) == 0)
			{
				return -1;
			}
			return 1;
		}

		public float PlayerX()
		{
			return EnemyPatternManager.Instance.PlayerPosition(null).x;
		}

		public float PlayerY()
		{
			return EnemyPatternManager.Instance.PlayerPosition(null).y;
		}
	}
}

