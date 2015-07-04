#region Copyright (c) 2004, Ryan Whitaker
/*********************************************************************************
'
' Copyright (c) 2004 Ryan Whitaker
'
' This software is provided 'as-is', without any express or implied warranty. In no 
' event will the authors be held liable for any damages arising from the use of this 
' software.
' 
' Permission is granted to anyone to use this software for any purpose, including 
' commercial applications, and to alter it and redistribute it freely, subject to the 
' following restrictions:
'
' 1. The origin of this software must not be misrepresented; you must not claim that 
' you wrote the original software. If you use this software in a product, an 
' acknowledgment (see the following) in the product documentation is required.
'
' This product uses software written by the developers of NClassifier
' (http://nclassifier.sourceforge.net).  NClassifier is a .NET port of the Nick
' Lothian's Java text classification engine, Classifier4J 
' (http://classifier4j.sourceforge.net).
'
' 2. Altered source versions must be plainly marked as such, and must not be 
' misrepresented as being the original software.
'
' 3. This notice may not be removed or altered from any source distribution.
'
'********************************************************************************/
#endregion

using System;

namespace NClassifier.Bayesian
{
	/// <summary>
	/// Represents the probability of a particular word.
	/// </summary>
	[Serializable]
	public class WordProbability : IComparable
	{
		static readonly int UNDEFINED = -1;

		string _word = string.Empty;
		string _category = string.Empty;
		long _matchingCount = UNDEFINED;
		long _nonMatchingCount = UNDEFINED;
		double _probability = IClassifierConstants.NEUTRAL_PROBABILITY;

		public string Word 
		{ 
			get { return _word; } 
			set { _word = value; } 
		}

		public string Category
		{
			get { return _category; }
			set { _category = value; }
		}
		
		public long MatchingCount 
		{ 
			get 
			{
				if (_matchingCount == UNDEFINED)
				{
					throw new ApplicationException("MatchingCount has not been defined.");
				}

				return _matchingCount; 
			} 
			set 
			{ 
				_matchingCount = value; 
				CalculateProbability();
			} 
		}
		
		public long NonMatchingCount
		{ 
			get 
			{
				if (_nonMatchingCount == UNDEFINED)
				{
					throw new ApplicationException("NonMatchingCount has not been defined.");
				}

				return _nonMatchingCount;
			} 
			set 
			{ 
				_nonMatchingCount = value; 
				CalculateProbability();
			} 
		}
		
		public double Probability 
		{ 
			get { return _probability; } 
			set 
			{ 
				_probability = value; 
				_matchingCount = UNDEFINED;
				_nonMatchingCount = UNDEFINED;
			} 
		}

		public WordProbability(string w, double probability)
		{
			Word = w;
			Probability = probability;
		}

		public WordProbability(string w, long matchingCount, long nonMatchingCount)
		{
			Word = w;
			MatchingCount = matchingCount;
			NonMatchingCount = nonMatchingCount;
		}

		public void RegisterMatch()
		{
			MatchingCount++;
			CalculateProbability();
		}

		public void RegisterNonMatch()
		{
			NonMatchingCount++;
			CalculateProbability();
		}

		private void CalculateProbability()
		{
			_probability = IClassifierConstants.NEUTRAL_PROBABILITY;

			if (_matchingCount == 0)
			{
				_probability = _nonMatchingCount == 0 ? IClassifierConstants.NEUTRAL_PROBABILITY : IClassifierConstants.LOWER_BOUND;
			}
			else
			{
				_probability = BayesianClassifier.NormalizeSignificance(_matchingCount / (double)(_matchingCount + _nonMatchingCount));
			}
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			if (!(obj is WordProbability))
			{
				throw new InvalidCastException(obj.GetType() + " is not a " + GetType());
			}

			var rhs = (WordProbability)obj;

			if (Category != rhs.Category)
			{
				return Category.CompareTo(rhs.Category);
			}

			if (Word != rhs.Word)
			{
				return Word.CompareTo(rhs.Word);
			}

			return 0;
		}

		public override string ToString()
		{
			return string.Format("{0}Word{1}Probability{2}MatchingCount{3}NonMatchingCount{4}", GetType(), Word, Probability, MatchingCount, NonMatchingCount);
		}

		#endregion
	}
}
