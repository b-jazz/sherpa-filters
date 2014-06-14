#region GPL
// Sherpa Filters
// Copyright (C) 2002
// This program is free software; you can redistribute it and/or modify it 
// under the terms of the GNU General Public License as published by the 
// Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc.; 59 Temple Place, Suite 330; Boston, MA 02111-1307 USA
#endregion GPL

using System;

namespace Sherpa
{
	/// <summary>
	/// Simple structure to hold three pieces of information: a token, probability it is spam, and magnitude of distance from 50%.
	/// </summary>
	public struct ProbTuple : IComparable
	{
		private String token;
		private Double probability;
		private Double magnitude;

		public ProbTuple(String token, Double probability, Double magnitude)
		{
			this.token = token;
			this.probability = probability;
			this.magnitude = magnitude;
		}

		public Double Probability
		{
			get
			{
				return probability;
			}
		}

		public Double Magnitude
		{
			get
			{
				return magnitude;
			}
		}

		public String Token
		{
			get
			{
				return token;
			}
		}

		public Int32 CompareTo(Object b)
		{
			if (b.GetType() != this.GetType())
				return -1;

			ProbTuple bTuple = (ProbTuple)b;
			return magnitude.CompareTo(bTuple.Magnitude);
		}
	}
}
