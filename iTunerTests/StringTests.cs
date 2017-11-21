//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerTests
{
	using System;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using iTuner;


	/// <summary>
	/// </summary>

	[TestClass]
	public class StringTests : TestBase
	{

		/// <summary>
		/// </summary>

		[TestMethod]
		public void EquivalentTests ()
		{
			bool eq = false;
			Assert.IsTrue(eq = "a".Equivalent("a"));
			Assert.IsFalse(eq = "a".Equivalent("b"));
			Assert.IsTrue(eq = "a a  a".Equivalent("a a  a"));
			Assert.IsTrue(eq = "  a a  a  ".Equivalent("a a  a  "));
			Assert.IsTrue(eq = "the same".Equivalent("same"));
			Assert.IsTrue(eq = "same".Equivalent("the same"));
			Assert.IsTrue(eq = "same, the".Equivalent("same, the"));
			Assert.IsTrue(eq = "the same".Equivalent("same, the"));
			Assert.IsTrue(eq = "same".Equivalent("same, the"));
			Assert.IsTrue(eq = "same".Equivalent("23 same"));
			Assert.IsTrue(eq = "23 the same".Equivalent("23 - same, the"));
		}


		/// <summary>
		/// </summary>

		[TestMethod]
		public void SimilarTests ()
		{
			string a;
			string b;

			a = "similarity test";
			b = "SIMILARITY TEST";
			int score = a.Similarity(b);
			Console.WriteLine("1 = " + score);
			Assert.AreEqual(100, score);

			a = "one two three";
			b = "one two three four";
			score = a.Similarity(b);
			Console.WriteLine("2 = " + score);
			Assert.AreEqual(73, score);

			a = "one two three";
			b = "one two threx four";
			score = a.Similarity(b);
			Console.WriteLine("3 = " + score);
			Assert.AreEqual(73, score);

			a = "marry had a little lamb";
			b = "marry had a lamb";
			score = a.Similarity(b);
			Console.WriteLine("4 = " + score);
			Assert.AreEqual(68, score);
		}


		/// <summary>
		/// </summary>

		[TestMethod]
		public void SelectSimilarTests ()
		{
			string a = "Marry had a little lamb";

			string[] candidates = new string[]
			{
				"joe had a little car",
				"marry had a huge elephant",
				"marry and joe did it behind the tree",
				"marry had lamb for dinner",
				"joe had marry for dinner",
				"marry and joe had a little lamb",
				"marry had a lamb"
			};

			Console.WriteLine("Comparing '{0}'", a);
			foreach (string s in candidates)
			{
				Console.WriteLine("score=[{0}] for '{1}'", a.Similarity(s), s);
			}

			int index = a.SelectSimilar(candidates);
			string result = (index < 0 ? String.Empty : candidates[index]);

			Console.WriteLine("Similar = [{0}]", result);
			Assert.AreEqual(candidates[5], result);
		}
	}
}
