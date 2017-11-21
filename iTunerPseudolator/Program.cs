//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTunerPseudolator
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Xml.Linq;


	/// <summary>
	/// This is a very quick and dirty means of scanning the default en-US Resources.resx
	/// file for all localizable content and generating a pseudo-English variant in the form
	/// of a Resources.en-029.resx resource file.  This is used during development to localize
	/// and guarantee 100% coverage of all visible text strings in the UI.
	/// </summary>

	class Program
	{
		private const string pseudoCulture = "en-029";  // Caribbean English!
		private static Dictionary<char, char> map;
		private static StringCollection reserved;

		static void Main (string[] args)
		{
			#region Map
			map = new Dictionary<char, char>();
			map.Add('A', 'Å');
			map.Add('B', '8');
			map.Add('C', 'Č');
			map.Add('D', 'Ď');
			map.Add('E', 'Ë');
			map.Add('F', '₣');
			map.Add('G', 'Ĝ');
			map.Add('H', 'Ĥ');
			map.Add('I', 'Ï');
			map.Add('J', 'Ĵ');
			map.Add('K', 'Ķ');
			map.Add('L', 'Ł');
			map.Add('M', 'M');
			map.Add('N', 'Ń');
			map.Add('O', 'Ö');
			map.Add('P', 'P');
			map.Add('Q', 'Ø');
			map.Add('R', 'Ř');
			map.Add('S', 'Ŝ');
			map.Add('T', 'Ť');
			map.Add('U', 'Ú');
			map.Add('V', 'У');
			map.Add('W', 'Ŵ');
			map.Add('X', 'ҳ');
			map.Add('Y', 'Ŷ');
			map.Add('Z', 'z');
			map.Add('a', 'â');
			map.Add('b', 'ъ');
			map.Add('c', 'ĉ');
			map.Add('d', 'ď');
			map.Add('e', 'é');
			map.Add('f', 'ƒ');
			map.Add('g', 'ğ');
			map.Add('h', 'ђ');
			map.Add('i', 'ï');
			map.Add('j', 'ĵ');
			map.Add('k', 'ķ');
			map.Add('l', 'ļ');
			map.Add('m', 'm');
			map.Add('n', 'ň');
			map.Add('o', 'õ');
			map.Add('p', 'ρ');
			map.Add('q', 'q');
			map.Add('r', 'ř');
			map.Add('s', '$');
			map.Add('t', 'ţ');
			map.Add('u', 'ù');
			map.Add('v', 'ч');
			map.Add('w', 'ŵ');
			map.Add('x', 'X');
			map.Add('y', 'ŷ');
			map.Add('z', 'z');
			#endregion Map
			#region Reserved
			reserved = new StringCollection();
			reserved.Add("Ctrl");
			reserved.Add("Alt");
			reserved.Add("Shift");
			reserved.Add("Esc");
			#endregion Reserved

			if (Path.GetExtension(args[0]).Equals(".xaml"))
			{
				TranslateXaml(args[0]);
			}
			else
			{
				TranslateResx(args[0]);
			}
		}


		/// <summary>
		/// Translate the given resx file - we're assuming it will be the project Resources.resx -
		/// and generate a Resoures.en-029.resx file containing pseudo-English translations
		/// </summary>
		/// <param name="path"></param>

		static void TranslateResx (string path)
		{
			Console.WriteLine("... translating " + path);

			XElement root = XElement.Load(path);
			XNamespace ns = root.GetDefaultNamespace();

			var data = from node in root.Elements(ns + "data") select node;

			foreach (XElement datum in data)
			{
				// ignore specially typed entries; assume these are not strings
				if (datum.Attribute(ns + "type") == null)
				{
					// ignore I_ prefix names since these are "Invariant"
					if (!datum.Attribute(ns + "name").Value.StartsWith("I_"))
					{
						XElement value = datum.Element(ns + "value");
						value.Value = Translate(value.Value);
					}
				}
			}

			string target = Path.Combine(
				Path.GetDirectoryName(path),
				String.Format("{0}.{1}.resx", Path.GetFileNameWithoutExtension(path), pseudoCulture));

			Console.WriteLine("... saving " + target);

			PrepareTarget(target);
			root.Save(target, SaveOptions.None);
		}


		/// <summary>
		/// Translate the given Xaml file - we're assuming it will be a ResourceDictionary -
		/// and generate an en-029.xaml file containing pseudo-English translations
		/// </summary>
		/// <param name="path"></param>

		static void TranslateXaml (string path)
		{
			Console.WriteLine("... translating " + path);

			XElement root = XElement.Load(path);
			XNamespace ns = root.GetNamespaceOfPrefix("s");

			var strings = from node in root.Elements(ns + "String") select node;

			foreach (XElement element in strings)
			{
				element.Value = Translate(element.Value);
			}

			string target = Path.Combine(Path.GetDirectoryName(path), pseudoCulture + ".xaml");

			Console.WriteLine("... saving " + target);

			PrepareTarget(target);
			root.Save(target, SaveOptions.None);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>

		static string Translate (string text)
		{
			StringBuilder builder = new StringBuilder();
			int counter = 0;

			foreach (char c in text)
			{
				if (Char.IsUpper(c) || Char.IsLower(c))
				{
					builder.Append(counter % 3 == 0 ? map[c] : c);
					counter++;
				}
				else
				{
					builder.Append(c);
					counter = 0;
				}
			}

			return builder.ToString();
		}


		static void PrepareTarget (string target)
		{
			if (File.Exists(target))
			{
				FileAttributes attributes = File.GetAttributes(target);
				if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
				{
					File.SetAttributes(target, attributes ^ FileAttributes.ReadOnly);
				}
			}
		}
	
	}
}
