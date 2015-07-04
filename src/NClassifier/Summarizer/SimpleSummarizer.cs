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
using System.Linq;
using System.Text;

namespace NClassifier.Summarizer
{
	public class SimpleSummarizer : ISummarizer
	{
		private IList<string> FindWordsWithFrequency(IDictionary<string, int> wordFrequencies, int frequency)
		{
			if (wordFrequencies == null || frequency == 0)
			{
				return new string[0];
			}

			var enumerable = wordFrequencies
				.Where(wordFrequency => frequency == wordFrequency.Value)
				.Select(wordFrequency => wordFrequency.Key);

			return enumerable.ToArray();
		}

		protected IList<string> GetMostFrequentWords(int count, IDictionary<string, int> wordFrequencies)
		{
			var result = new List<string>();
			var freq = Math.Max(wordFrequencies.Values.Max(), 0);

			while (result.Count < count && freq > 0)
			{
				var words = FindWordsWithFrequency(wordFrequencies, freq);
				result.AddRange(words);
				freq--;
			}

			return result;
		}

		public string Summarize(string input, int numberOfSentences)
		{
			// get the frequency of each word in the input
			var wordFrequencies = Utilities.GetWordFrequency(input);

			// now create a set of the X most frequent words
			var mostFrequentWords = GetMostFrequentWords(100, wordFrequencies);

			// break the input up into sentences
			var workingSentences = Utilities.GetSentences(input.ToLower());
			var actualSentences = Utilities.GetSentences(input);

			// iterate over the most frequent words, and add the first sentence
			// that includes each word to the result.
			var outputSentences = new List<string>();
			foreach (var word in mostFrequentWords)
			{
				for (var i = 0; i < workingSentences.Count; i++)
				{
					if (workingSentences[i].IndexOf(word) >= 0)
					{
						outputSentences.Add(actualSentences[i]);
						break;
					}

					if (outputSentences.Count >= numberOfSentences)
					{
						break;
					}
				}

				if (outputSentences.Count >= numberOfSentences)
				{
					break;
				}
			}

			var reorderedOutputSentences = ReorderSentences(outputSentences, input);

			var result = new StringBuilder();
			foreach (var sentence in reorderedOutputSentences)
			{
				if (result.Length > 0)
				{
					result.Append(' ');
				}

				result.Append(sentence);
				result.Append('.'); // this isn't correct - it should be whatever symbol the sentence finished with
			}

			return result.ToString();
		}

		private IList<string> ReorderSentences(List<string> outputSentences, string input)
		{
			var result = new List<string>(outputSentences);
			result.Sort(new SimpleSummarizerComparer(input));

			return result;
		}
	}

	public class SimpleSummarizerComparer : IComparer<string>
	{
		private readonly string _input = string.Empty;

		public SimpleSummarizerComparer(string input)
		{
			_input = input;
		}

		#region IComparer Members

		public int Compare(string x, string y)
		{
			return _input.IndexOf(x.Trim()) - _input.IndexOf(y.Trim());
		}

		#endregion
	}
}