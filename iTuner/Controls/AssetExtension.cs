//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.Controls
{
	using System;
	using System.Reflection;
	using System.Text.RegularExpressions;
	using System.Windows.Markup;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;


	/// <summary>
	/// This is a great little XAML markup extention that allieviates the need to use
	/// full "pack://" URIs in XAML.  Instead, you can use something like:
	/// <para>
	/// &lt;Image Source="{ns:Asset Path=Images/Files/Open.png}" /&gt;
	/// </para>
	/// </summary>
	/// <remarks>
	/// http://blog.pixelingene.com/?p=352
	/// </remarks>

	[MarkupExtensionReturnType(typeof(object))]
	internal class AssetExtension : MarkupExtension
	{

		private enum ResourceType
		{
			None,
			Xaml,
			Font,
			Image
		}

		private static Regex pattern = new Regex(
			@"
				(?<Image>(\.png|\.gif|\.jpg|\.bmp|\.ico)$)
				| (?<Font>\#.+$)
				| (?<Xaml>\.xaml$)
			",
			RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace |
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);


		//========================================================================================
		// Constructor
		//========================================================================================

		[ConstructorArgument("path")]
		public string Path { get; set; }

		public AssetExtension ()
		{
		}

		public AssetExtension (string path)
		{
			this.Path = path;
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Returns a fully formatted resource URI derived from the given relative path.
		/// This is used by code-behind to specify full resource paths.
		/// </summary>
		/// <param name="path">A relative path of the resource.</param>
		/// <returns>A pack URI specification.</returns>

		public static Uri GetResourceUri (string path)
		{
			string asmName = Assembly.GetExecutingAssembly().GetName().Name;
			string uriString = string.Format("pack://application:,,,/{0};component/{1}", asmName, path);
			Uri uri = new Uri(uriString);

			return uri;
		}


		public override object ProvideValue (IServiceProvider serviceProvider)
		{
			IProvideValueTarget target = serviceProvider.GetService(
				typeof(IProvideValueTarget)) as IProvideValueTarget;

			if (target == null)
			{
				return null;
			}

			ResourceType type = ParseResourceType(Path);
			switch (type)
			{
				case ResourceType.Image:
					BitmapImage image = new BitmapImage(GetResourceUri(Path));
					return image;

				case ResourceType.Font:
					FontFamily family = new FontFamily(GetResourceUri(""), Path);
					return family;

				case ResourceType.Xaml:
					return GetResourceUri(Path);
			}

			return null;
		}


		private ResourceType ParseResourceType (string path)
		{
			ResourceType type = ResourceType.None;

			Match match = pattern.Match(path);

			if (match.Groups["Image"].Success)
			{
				type = ResourceType.Image;
			}
			else if (match.Groups["Font"].Success)
			{
				type = ResourceType.Font;
			}
			else if (match.Groups["Xaml"].Success)
			{
				type = ResourceType.Xaml;
			}

			return type;
		}
	}
}
