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
using System.Collections.Generic;

namespace NClassifier.Bayesian
{
	/// <summary>
	/// An implementation of IClassifer based on Bayes' algorithm.
	/// </summary>
	public class BayesianClassifier : AbstractClassifier, ITrainableClassifier
	{
		#region Fields

		private readonly IWordsDataSource _wordsData;
		private readonly ITokenizer _tokenizer;
		private readonly IStopWordProvider _stopWordProvider;
		private bool _isCaseSensitive;

		#endregion

		#region Properties

		public bool IsCaseSensitive { get { return _isCaseSensitive; } set { _isCaseSensitive = value; } }
		public IWordsDataSource WordsDataSource { get { return _wordsData; } }
		public ITokenizer Tokenizer { get { return _tokenizer; } }
		public IStopWordProvider StopWordProvider { get { return _stopWordProvider; } }

		#endregion

		#region Constructors

		public BayesianClassifier() : this(new SimpleWordsDataSource(), new DefaultTokenizer(DefaultTokenizer.BREAK_ON_WORD_BREAKS)) {}

		public BayesianClassifier(IWordsDataSource wordsDataSource) : this(wordsDataSource, new DefaultTokenizer(DefaultTokenizer.BREAK_ON_WORD_BREAKS)) {}

		public BayesianClassifier(IWordsDataSource wordsDataSource, ITokenizer tokenizer) : this(wordsDataSource, tokenizer, new DefaultStopWordProvider()) {}

		public BayesianClassifier(IWordsDataSource wordsDataSource, ITokenizer tokenizer, IStopWordProvider stopWordProvider)
		{
			if (wordsDataSource == null)
			{
				throw new ArgumentNullException("wordsDataSource");
			}

			_wordsData = wordsDataSource;

			if (tokenizer == null)
			{
				throw new ArgumentNullException("tokenizer");
			}

			_tokenizer = tokenizer;

			if (stopWordProvider == null)
			{
				throw new ArgumentNullException("stopWordProvider");
			}

			_stopWordProvider = stopWordProvider;
		}

		#endregion

		public bool IsMatch(string category, string input)
		{
			return IsMatch(category, _tokenizer.Tokenize(input));
		}

		public override double Classify(string input)
		{
			return Classify(ICategorizedClassifierConstants.DEFAULT_CATEGORY, input);
		}

		public double Classify(string category, string input)
		{
			if (category == null)
			{
				throw new ArgumentNullException(category);
			}

			if (input == null)
			{
				throw new ArgumentNullException(input);
			}

			CheckCategoriesSupported(category);

			return Classify(category, _tokenizer.Tokenize(input));
		}

		public double Classify(string category, string[] words)
		{
			var wps = CalcWordsProbability(category, words);
			return NormalizeSignificance(CalculateOverallProbability(wps));
		}

		public void TeachMatch(string input)
		{
			TeachMatch(ICategorizedClassifierConstants.DEFAULT_CATEGORY, input);
		}

		public void TeachNonMatch(string input)
		{
			TeachNonMatch(ICategorizedClassifierConstants.DEFAULT_CATEGORY, input);
		}

		public void TeachMatch(string category, string input)
		{
			if (category == null)
			{
				throw new ArgumentNullException("category");
			}

			if (input == null)
			{
				throw new ArgumentNullException("input");
			}

			CheckCategoriesSupported(category);

			TeachMatch(category, _tokenizer.Tokenize(input));
		}

		public void TeachNonMatch(string category, string input)
		{
			if (category == null)
			{
				throw new ArgumentNullException("category");
			}

			if (input == null)
			{
				throw new ArgumentNullException("input");
			}

			CheckCategoriesSupported(category);

			TeachNonMatch(category, _tokenizer.Tokenize(input));
		}

		public bool IsMatch(string category, string[] input)
		{
			if (category == null)
			{
				throw new ArgumentNullException("category");
			}

			if (input == null)
			{
				throw new ArgumentNullException("input");
			}

			CheckCategoriesSupported(category);

			var matchProbability = Classify(category, input);

			return matchProbability >= cutoff;
		}

		public void TeachMatch(string category, string[] words)
		{
			var categorizedWordsDataSource = _wordsData as ICategorizedWordsDataSource;

			for (var i = 0; i < words.Length; i++)
			{
				if (IsClassifiableWord(words[i]))
				{
					if (categorizedWordsDataSource == null)
					{
						_wordsData.AddMatch(TransformWord(words[i]));
					}
					else
					{
						categorizedWordsDataSource.AddMatch(category, TransformWord(words[i]));
					}
				}
			}
		}

		public void TeachNonMatch(string category, string[] words)
		{
			var categorizedWordsDataSource = _wordsData as ICategorizedWordsDataSource;

			for (var i = 0; i < words.Length; i++)
			{
				if (IsClassifiableWord(words[i]))
				{
					if (categorizedWordsDataSource == null)
					{
						_wordsData.AddNonMatch(TransformWord(words[i]));
					}
					else
					{
						categorizedWordsDataSource.AddNonMatch(category, TransformWord(words[i]));
					}
				}
			}
		}

		/// <summary>
		/// Allows transformations to be done to the given word.
		/// </summary>
		/// <param name="word">The word to transform.</param>
		/// <returns>The transformed word.</returns>
		public string TransformWord(string word)
		{
			if (word == null)
			{
				throw new ArgumentNullException("word");
			}
			
			return _isCaseSensitive ? word : word.ToLower();
		}

		public double CalculateOverallProbability(IList<WordProbability> wps)
		{
			if (wps == null || wps.Count == 0)
			{
				return IClassifierConstants.NEUTRAL_PROBABILITY;
			}

			// we need to calculate xy/(xy + z) where z = (1 - x)(1 - y)
			
			// first calculate z and xy
			var z = 0d;
			var xy = 0d;
			for (var i = 0; i < wps.Count; i++)
			{
				z = z == 0 ? 1 - wps[i].Probability : z*(1 - wps[i].Probability);
				xy = xy == 0 ? wps[i].Probability : xy*wps[i].Probability;
			}

			var numerator = xy;
			var denominator = xy + z;

			return numerator / denominator;
		}

		private IList<WordProbability> CalcWordsProbability(string category, string[] words)
		{
			if (category == null)
				throw new ArgumentNullException("Category cannot be null.");

			var categorizedWordsDataSource = _wordsData as ICategorizedWordsDataSource;

			CheckCategoriesSupported(category);
			
			if (words == null)
				return new WordProbability[0];
			
			var wps = new List<WordProbability>();
			for (var i = 0; i < words.Length; i++)
			{
				if (IsClassifiableWord(words[i]))
				{
					WordProbability wp = null;
					if (categorizedWordsDataSource == null)
					{
						wp = _wordsData.GetWordProbability(TransformWord(words[i]));
					}
					else
					{
						categorizedWordsDataSource.GetWordProbability(category, TransformWord(words[i]));
					}

					if (wp != null)
					{
						wps.Add(wp);
					}
				}
			}

			return wps;
		}

		private void CheckCategoriesSupported(string category)
		{
			// if the category is not the default
			if (ICategorizedClassifierConstants.DEFAULT_CATEGORY != category && !(_wordsData is ICategorizedWordsDataSource))
			{
				throw new ArgumentException("Word Data Source does not support non-default categories.");
			}
		}

		private bool IsClassifiableWord(string word)
		{
			return !string.IsNullOrEmpty(word) && !_stopWordProvider.IsStopWord(word);
		}

		public static double NormalizeSignificance(double sig)
		{
			if (IClassifierConstants.UPPER_BOUND < sig)
			{
				return IClassifierConstants.UPPER_BOUND;
			}

			if (IClassifierConstants.LOWER_BOUND > sig)
			{
				return IClassifierConstants.LOWER_BOUND;
			}
			
			return sig;
		}
	}
}