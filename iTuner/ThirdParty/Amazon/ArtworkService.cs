//************************************************************************************************
// Copyright © 2012 Steven M. Cohn.  All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.Amazon
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Xml.Linq;
	using Settings = iTuner.Properties.Settings;


	/// <summary>
	/// The artwork service is responsible for retrieving the album art.
	/// </summary>
	/// <remarks>
	/// To give credit where credit is due... this is an adaption of Shaun Danielz's work
	/// as described on his blog http://www.shaundanielz.com/ and from his sample codeplex
	/// project http://itunesartworkapp.codeplex.com/.  Thanks Shaun!
	/// </remarks>

	internal class ArtworkService
	{
		private const string AwsAssociateName = "iTuner";
		private const string LogCategory = "ArtWorkService";

		private readonly string accessKey;
		private readonly string secretKey;
		private readonly bool isEnabled;
		private string filename;


		/// <summary>
		/// Initializes a new instance capable of retrieving artwork from Amazon Web services.
		/// </summary>

		public ArtworkService ()
		{
			filename = null;

			accessKey = Settings.Default.AwsAccessKey;
			secretKey = Settings.Default.AwsSecretKey;

			// attempt alternate source for AWS keys
			if (String.IsNullOrEmpty(accessKey) || String.IsNullOrEmpty(secretKey))
			{
				GetPrivateAwsKeys(out accessKey, out secretKey);
			}

			isEnabled = !(String.IsNullOrEmpty(accessKey) || String.IsNullOrEmpty(secretKey));
		}


		/// <summary>
		/// Read AWS keys from an alternate private location.  I use this as the iTuner developer
		/// so I can read my own keys from a file that is not included in the distribution.
		/// </summary>
		/// <param name="accessKey"></param>
		/// <param name="secretKey"></param>
		/// <remarks>
		/// Looks for a file named AwsSettings.xml in the iTuner installation directory.
		/// The file must have a format similar to the following.
		/// <![CDATA[
		/// <?xml version="1.0" encoding="utf-8"?>
		/// <AwsSettings>
		///   <accessKey>ABC01234567890ABCDEF</accessKey>
		///   <secretKey>e8Fghi9jK0m4nOPqRO+STuVWXYZ1234567890ABC</secretKey>
		/// </AwsSettings>
		/// ]]>
		/// </remarks>

		private void GetPrivateAwsKeys (out string accessKey, out string secretKey)
		{
			string path = Path.Combine(
				Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
				"AwsSettings.xml");

			if (File.Exists(path))
			{
				var root = XElement.Load(path);
				if (root != null)
				{
					var key = root.Element("accessKey");
					accessKey = (key == null ? null : key.Value);

					key = root.Element("secretKey");
					secretKey = (key == null ? null : key.Value);

					return;
				}
			}

			accessKey = null;
			secretKey = null;
		}


		/// <summary>
		/// Gets the simple original file name of the image.  This is only populated once
		/// an image is retrieved using the GetArtwork method.
		/// </summary>

		public string FileName
		{
			get { return filename; }
		}


		/// <summary>
		/// Gets a Boolean value indicating whether this service has been enabled by
		/// properly specifying both an AWS access key and a secret key in the config file.
		/// </summary>

		public bool IsEnabled
		{
			get { return isEnabled; }
		}


		/// <summary>
		/// Gets the first available artwork associated with the specified album.
		/// </summary>
		/// <param name = "album">The album that requires artwork.</param>

		public byte[] GetArtwork (string artist, string album)
		{
			if (!isEnabled)
			{
				return null;
			}

			if (String.IsNullOrEmpty(artist))
			{
				throw new ArgumentNullException("artist");
			}

			if (String.IsNullOrEmpty(album))
			{
				throw new ArgumentNullException("album");
			}

			// build the request URL and sign it according to Amazon's specification
			var parameters = new Dictionary<string, string>();
			parameters["Service"] = "AWSECommerceService";
			parameters["Version"] = "2009-11-01";
			parameters["AssociateTag"] = AwsAssociateName;
			parameters["ContentType"] = "text/xml";
			parameters["Operation"] = "ItemSearch";
			parameters["SearchIndex"] = "Music";
			parameters["ResponseGroup"] = "Images";
			parameters["Artist"] = artist;
			parameters["Keywords"] = album;

			string searchURL = null;
			using (var helper = new SignedRequestHelper(accessKey, secretKey))
			{
				searchURL = helper.Sign(parameters);
			}

			parameters.Clear();
			parameters = null;

			string artworkURL = GetArtworkUrl(searchURL);
			if (artworkURL != null)
			{
				byte[] image = GetArtworkImage(artworkURL);
				return image;
			}

			return null;
		}


		/// <summary>
		/// Gets the album download URL.
		/// </summary>
		/// <param name = "url">The request url for the Amazon Product Service.</param>
		/// <returns>The download url for the album image</returns>

		private string GetArtworkUrl (string searchURL)
		{
			XElement root = null;

			try
			{
				var request = WebRequest.Create(searchURL);
				var response = request.GetResponse();

				using (Stream stream = response.GetResponseStream())
				{
					root = XElement.Load(stream);
				}
			}
			catch (Exception exc)
			{
				Logger.WriteLine(LogCategory, "Error occured attempting to GetArtworkUrl()", exc);
				return null;
			}

			var ns = root.GetDefaultNamespace();

			var nodes =
				from e in root
					.Elements(ns + "Items")
					.Elements(ns + "Item")
					.Elements(ns + "ImageSets")
					.Elements(ns + "ImageSet")
					.Elements(ns + "LargeImage")
					.Elements(ns + "URL")
					select e;

			if ((nodes != null) && (nodes.Count() > 0))
			{
				string url = nodes.FirstOrDefault().Value;
				filename = url.Substring(url.LastIndexOf('/') + 1);

				return url;
			}

			return null;
		}


		private byte[] GetArtworkImage (string artworkURL)
		{
			byte[] data = null;
			MemoryStream stream = null;

			try
			{
				using (var client = new WebClient())
				{
					stream = new MemoryStream(client.DownloadData(artworkURL));
				}

				data = stream.ToArray();
			}
			catch (Exception exc)
			{
				Logger.WriteLine(LogCategory, "Error occured attempting to GetArtworkImage()", exc);
				return null;
			}
			finally
			{
				if (stream != null)
				{
					stream.Dispose();
					stream = null;
				}
			}

			return data;
		}
	}
}
