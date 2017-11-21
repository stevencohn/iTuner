//************************************************************************************************
// Copyright © 2012 Steven M. Cohn.  All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.Amazon
{
	using System;
	using System.Collections.Generic;
	using System.Security.Cryptography;
	using System.Text;
	using iTuner.Tools;


	/// <summary>
	/// Amazon Web Services require signed request query strings that must abide by strict rules.
	/// Once signed, any modification to this string is invalidated and rejected by Amazon.
	/// </summary>

	internal class SignedRequestHelper : IDisposable
	{
		private const string URLPrefix = "ecs.amazonaws";
		private const string REQUEST_URI = "/onca/xml";
		private const string REQUEST_METHOD = "GET";

		private readonly string accessKeyID;		// AWS public access key ID
		private readonly string endpoint;			// name of remote AWS server
		private HMAC signer;						// cryptographer
		private bool isDisposed;


		#region class OrdinalComparer

		/// <summary>
		/// Simple comparison of two parameters used to order name-value pairs in
		/// a SortedDictionary.
		/// </summary>

		private class OrdinalComparer : IComparer<string>
		{
			public int Compare (string p1, string p2)
			{
				return String.CompareOrdinal(p1, p2);
			}
		}

		#endregion class OrdinalComparer


		//========================================================================================
		// Lifecycle
		//========================================================================================

		/// <summary>
		/// Initialize a new instance based on the given AWS credentials.
		/// </summary>
		/// <param name="awsAccessKeyId"></param>
		/// <param name="awsSecretKey"></param>
		/// <param name="destination"></param>
		/// <remarks>
		/// See http://aws.amazon.com for further information and to gain credentials.
		/// </remarks>

		public SignedRequestHelper (string accessKeyID, string secretKey)
		{
			this.accessKeyID = accessKeyID;
			this.endpoint = GetBestEndpoint();
			this.signer = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
			this.isDisposed = false;
		}


		/// <summary>
		/// Build a DNS name for the geographically closest AWS service.
		/// </summary>
		/// <returns>The DNS name of the closest AWS service.</returns>

		private string GetBestEndpoint ()
		{
			string domain = NetworkStatus.GetBestDomainName();

			if (!String.IsNullOrEmpty(domain))
			{
				if (domain.EndsWith(".com")) return URLPrefix + ".com";
				if (domain.EndsWith(".ca")) return URLPrefix + ".ca";
				if (domain.EndsWith(".co.uk")) return URLPrefix + ".co.uk";
				if (domain.EndsWith(".de")) return URLPrefix + ".de";
				if (domain.EndsWith(".fr")) return URLPrefix + ".fr";
				if (domain.EndsWith(".jp")) return URLPrefix + ".jp";
			}

			return URLPrefix + ".com";
		}


		public void Dispose ()
		{
			if (!isDisposed)
			{
				if (signer != null)
				{
					signer.Dispose();
					signer = null;
				}

				isDisposed = true;
			}
		}


		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// Sign a request in the form of a Dictionary of name-value pairs.
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>

		public string Sign (IDictionary<string, string> parameters)
		{
			// parameter must be sorted for AWS (they're awefully picky, aren't they?)
			var sorted = new SortedDictionary<string, string>(parameters, new OrdinalComparer());
			sorted["AWSAccessKeyId"] = accessKeyID;
			sorted["Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

			var qparams = BuildQueryString(sorted);

			// derive the bytes needs to be signed
			var builder = new StringBuilder();
			builder.Append(REQUEST_METHOD)
				.Append("\n")
				.Append(endpoint)
				.Append("\n")
				.Append(REQUEST_URI)
				.Append("\n")
				.Append(qparams);

			var package = Encoding.UTF8.GetBytes(builder.ToString());

			// compute the signature and convert to Base64
			var hashed = signer.ComputeHash(package);
			var signature = Convert.ToBase64String(hashed);

			// construct the complete URL
			builder.Clear();
			builder.Append("http://")
				.Append(endpoint)
				.Append(REQUEST_URI)
				.Append("?")
				.Append(qparams)
				.Append("&Signature=")
				.Append(Encode(signature));

			return builder.ToString();
		}


		/// <summary>
		/// Build a canonical query string from the sorted parameters.
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>

		private string BuildQueryString (SortedDictionary<string, string> parameters)
		{
			if (parameters.Count == 0)
			{
				return String.Empty;
			}

			var builder = new StringBuilder();
			foreach (var entry in parameters)
			{
				if (builder.Length > 0)
				{
					// separate this param from previous
					builder.Append("&");
				}

				// add this param as key=value pair
				builder.Append(Encode(entry.Key));
				builder.Append("=");
				builder.Append(Encode(entry.Value));
			}

			return builder.ToString();
		}


		/// <summary>
		/// Transforms the given string by percent-encoding (URL encoding) according to the
		/// string rules described by RFC 3986 as required by Amazon.
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		/// <remarks>
		/// This is necessary because .NET's HttpUtility.UrlEncode does not sufficiently
		/// encode according to the above standard.  Also, .NET returns lower-case encoding
		/// by default and Amazon requires upper-case encoding.
		/// </remarks>

		private string Encode (string info)
		{
			if ((info == null) || (info.Trim().Length == 0))
			{
				return String.Empty;
			}

			info = HttpUtility.UrlEncode(info);

			var builder = new StringBuilder();
			int i = 0;
			char c;
			while (i < info.Length)
			{
				c = info[i];
				if (c == '\'')
				{
					builder.Append("%27");
				}
				else if (c == '(')
				{
					builder.Append("%28");
				}
				else if (c == ')')
				{
					builder.Append("%29");
				}
				else if (c == '*')
				{
					builder.Append("%2A");
				}
				else if (c == '!')
				{
					builder.Append("%21");
				}
				else if (c == '+')
				{
					builder.Append("%20");
				}
				else if (c == '%')
				{
					if (i < info.Length - 2)
					{
						// unescape tilde
						if ((info[i + 1] == '7') && (info[i + 2] == 'e'))
						{
							builder.Append("~");
							i += 2;
						}
						else
						{
							// uppercase escaped sequence
							builder.Append(c);
							builder.Append(Char.IsLower(c = info[++i]) ? Char.ToUpper(c) : c);
							builder.Append(Char.IsLower(c = info[++i]) ? Char.ToUpper(c) : c);
						}
					}
					else
					{
						// we're at the end of the string with either "%c" or "%"
						// this is probably an error since a Percent shouldn't preceed
						// less than two characters
						builder.Append(c);
					}
				}
				else
				{
					// simple char
					builder.Append(c);
				}

				i++;
			}

			return builder.ToString();
		}
	}
}