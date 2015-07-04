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
using System.Text.RegularExpressions;

namespace NClassifier
{
	public static class Utilities
	{
		public static IDictionary<string, int> GetWordFrequency(string input, bool caseSensitive = false)
		{
			return GetWordFrequency(input, caseSensitive, new DefaultTokenizer(), new DefaultStopWordProvider());
		}

		/// <summary>
		/// Gets a dictionary of words and integers representing the number of each word.
		/// </summary>
		/// <param name="input">The string to get the word frequency of.</param>
		/// <param name="caseSensitive">True if words should be treated as separate if they have different casing.</param>
		/// <param name="tokenizer">A instance of ITokenizer.</param>
		/// <param name="stopWordProvider">An instance of IStopWordProvider.</param>
		/// <returns></returns>
		public static IDictionary<string, int> GetWordFrequency(string input, bool caseSensitive, ITokenizer tokenizer, IStopWordProvider stopWordProvider)
		{
			var convertedInput = input;
			if (!caseSensitive)
			{
				convertedInput = input.ToLower();
			}

			var words = tokenizer.Tokenize(convertedInput);
			Array.Sort(words);

			var uniqueWords = GetUniqueWords(words);

			var result = new Dictionary<string, int>();
			for (var i = 0; i < uniqueWords.Length; i++)
			{
				var word = uniqueWords[i];

				if (stopWordProvider == null || (IsWord(word) && !stopWordProvider.IsStopWord(word)))
				{
					int value;
					if (result.TryGetValue(word, out value))
					{
						result[word] = value + CountWords(word, words);
					}
					else
					{
						result.Add(word, CountWords(word, words));
					}
				}
			}

			return result;
		}

		public static bool IsWord(string word)
		{
			return word != null && word.Trim() != string.Empty;
		}

		/// <summary>
		/// Find all unique words in an array of words.
		/// </summary>
		/// <param name="input">An array of strings.</param>
		/// <returns>An array of all unique strings.  Order is not guaranteed.</returns>
		public static string[] GetUniqueWords(string[] input)
		{
			if (input == null)
			{
				return new string[0];
			}
			
			var result = new List<string>();
			foreach (var word in input)
			{
				if (!result.Contains(word))
				{
					result.Add(word);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Count how many times a word appears in an array of words.
		/// </summary>
		/// <param name="word">The word to count.</param>
		/// <param name="words">A non-null array of words.</param>
		public static int CountWords(string word, string[] words)
		{
			// find the index of one of the items in the array
			var itemIndex = Array.BinarySearch(words, word);

			// iterate backwards until we find the first match
			while (itemIndex > 0 && words[itemIndex] == word)
			{
				itemIndex--;
			}

			// now itemIndex is one item before the start of the words
			var count = 0;
			while (itemIndex < words.Length && itemIndex >= 0)
			{
				if (words[itemIndex] == word)
				{
					count++;
				}

				itemIndex++;

				if (itemIndex < words.Length && words[itemIndex] != word)
				{
					break;
				}
			}

			return count;
		}

		/// <summary>
		/// Gets an array of sentences.
		/// </summary>
		/// <param name="input">A string that contains sentences.</param>
		/// <returns>An array of strings, each element containing a sentence.</returns>
		public static IList<string> GetSentences(string input)
		{
			if (input == null)
			{
				return new string[0];
			}
			
			// split on a ".", a "!", a "?" followed by a space or EOL
			// the original Java regex was (\.|!|\?)+(\s|\z)
			var result = Regex.Split(input, @"(?:\.|!|\?)+(?:\s+|\z)");

			// hacky... doing this to pass the unit tests
			return result.Where(s => s.Length > 0).ToArray();
		}
	}
}