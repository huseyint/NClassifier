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

using NClassifier.Bayesian;
using NUnit.Framework;

namespace NClassifier.Tests.Bayesian
{
	[TestFixture]
	public class BayesianClassifierTest
	{
		[Test]
		public void TestClassify()
		{
			var wds = new SimpleWordsDataSource();
			var classifier = new BayesianClassifier(wds);

			var sentence = new[] { "This", "is", "a", "sentence", "about", "java" };

			Assert.AreEqual(IClassifierConstants.NEUTRAL_PROBABILITY, classifier.Classify(ICategorizedClassifierConstants.DEFAULT_CATEGORY, sentence), 0d);

			wds.SetWordProbability(new WordProbability("This", .5d));
			wds.SetWordProbability(new WordProbability("is", .5d));
			wds.SetWordProbability(new WordProbability("a", .5d));
			wds.SetWordProbability(new WordProbability("sentence", .2d));
			wds.SetWordProbability(new WordProbability("about", .5d));
			wds.SetWordProbability(new WordProbability("java", .99d));

			Assert.AreEqual(.96d, classifier.Classify(ICategorizedClassifierConstants.DEFAULT_CATEGORY, sentence), .009d);
		}

		[Test]
		public void TestGetWordsDataSource()
		{
			var wds = new SimpleWordsDataSource();
			var classifier = new BayesianClassifier(wds);
			Assert.AreEqual(wds, classifier.WordsDataSource);
		}

		[Test]
		public void TestGetTokenizer()
		{
			var wds = new SimpleWordsDataSource();
			ITokenizer tokenizer = new DefaultTokenizer(DefaultTokenizer.BREAK_ON_WORD_BREAKS);
			var classifier = new BayesianClassifier(wds, tokenizer);
			Assert.AreEqual(tokenizer, classifier.Tokenizer);
		}

		[Test]
		public void TestGetStopWordProvider()
		{
			var wds = new SimpleWordsDataSource();
			ITokenizer tokenizer = new DefaultTokenizer(DefaultTokenizer.BREAK_ON_WORD_BREAKS);
			IStopWordProvider stopWordProvider = new DefaultStopWordProvider();
			var classifier = new BayesianClassifier(wds, tokenizer, stopWordProvider);
			Assert.AreEqual(stopWordProvider, classifier.StopWordProvider);
		}

		[Test]
		public void TestCaseSensitive()
		{
			var classifier = new BayesianClassifier();
			Assert.IsFalse(classifier.IsCaseSensitive);
			classifier.IsCaseSensitive = true;
			Assert.IsTrue(classifier.IsCaseSensitive);
		}

		[Test]
		public void TestTransformWord()
		{
			var classifier = new BayesianClassifier();
			Assert.IsFalse(classifier.IsCaseSensitive);

			string word = null;
			try
			{
				classifier.TransformWord(word);
				Assert.Fail("No exception thrown when null passed.");
			}
			catch {}

			word = "myWord";
			Assert.AreEqual(word.ToLower(), classifier.TransformWord(word));

			classifier.IsCaseSensitive = true;
			Assert.AreEqual(word, classifier.TransformWord(word));
		}

		[Test]
		public void TestCalculateOverallProbability()
		{
			var prob = 0.3d;
			var wp1 = new WordProbability("myWord1", prob);
			var wp2 = new WordProbability("myWord2", prob);
			var wp3 = new WordProbability("myWord3", prob);
		
			var wps = new[] { wp1, wp2, wp3 };
			var errorMargin = 0.0001d;
		
			var xy = (prob * prob * prob);
			var z = (1-prob)*(1-prob)*(1-prob);
		
			var result = xy/(xy + z);
		
			var classifier = new BayesianClassifier();
		 		
			Assert.AreEqual(result, classifier.CalculateOverallProbability(wps), errorMargin);
		}
	}
}
