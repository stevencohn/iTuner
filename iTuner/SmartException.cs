//************************************************************************************************
// Copyright © 2010 Steven M. Cohn.  All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using System.Text;
	using System.Xml;
	using System.Xml.Serialization;


	/// <summary>
	/// Expanded Exception class to capture and display detailed information.
	/// </summary>
	/// <remarks>
	/// This class must be declared public for proper serialization!
	/// </remarks>

	public class SmartException : Exception, IXmlSerializable
	{
		private Exception root;
		private InnerDetail inner;


		/// <summary>
		/// 
		/// </summary>

		public class InnerDetail
		{
			public InnerDetail ()
				: this(String.Empty)
			{
			}

			public InnerDetail (string message)
			{
				this.Detail = message;
				this.Children = new List<InnerDetail>();
			}

			public string Detail { get; set; }
			public List<InnerDetail> Children { get; set; }
		}


		public class OtherItem
		{
			public string Name { get; set; }
			public string Description { get; set; }
		}


		//========================================================================================
		// Constructors
		//========================================================================================

		/// <summary>
		/// 
		/// </summary>

		public SmartException ()
		{
			this.root = null;
			this.inner = null;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="exc"></param>

		public SmartException (Exception exc)
			: base("iTuner Exception", exc)
		{
			this.root = exc;
			this.inner = null;
		}


		//========================================================================================
		// Properties - bindable
		//========================================================================================

		public string FullName
		{
			get { return root.GetType().FullName; }
		}


		public override string HelpLink
		{
			get { return root.HelpLink; }
			set { }
		}


		public InnerDetail InnerDetails
		{
			get
			{
				if (inner == null)
					inner = GetInnerExceptionDetails();

				return inner;
			}
		}


		public override string Message
		{
			get { return root.Message; }
		}


		public List<OtherItem> OtherInformation
		{
			get
			{
				IDictionaryEnumerator property = GetCustomExceptionInfo(root).GetEnumerator();
				List<OtherItem> others = new List<OtherItem>();

				while (property.MoveNext())
				{
					OtherItem other = new OtherItem();
					other.Name = property.Key.ToString();

					other.Description =
						(property.Value == null ? String.Empty : property.Value.ToString());

					others.Add(other);
				}

				return others;
			}
		}

		public override string Source
		{
			get { return root.Source; }
			set { }
		}


		public override string StackTrace
		{
			get { return root.StackTrace; }
		}


		public new string TargetSite
		{
			get { return GetTargetMethodFormat(root); }
		}


		public string XmlMessage
		{
			get
			{
				XmlSerializer serializer = new XmlSerializer(this.GetType());
				StringBuilder buffer = new StringBuilder();

				using (var writer = new StringWriter(buffer))
				{
					serializer.Serialize(writer, this);
				}

				return buffer.ToString();
			}
		}


		//========================================================================================
		// Helper Methods
		//========================================================================================

		private InnerDetail GetInnerExceptionDetails ()
		{
			Exception exc = root;
			InnerDetail detail = new InnerDetail();
			InnerDetail child = detail;

			while (exc != null)
			{
				child.Detail = exc.GetType().FullName;
				child.Children.Add(new InnerDetail(exc.Message));
				child.Children.Add(new InnerDetail(GetTargetMethodFormat(exc)));

				exc = exc.InnerException;

				if (exc != null)
				{
					InnerDetail inner = new InnerDetail();
					child.Children.Add(inner);
					child = inner;
				}
			}

			return detail;
		}


		private string GetTargetMethodFormat (Exception exc)
		{
			if (exc.TargetSite == null)
			{
				return String.Empty;
			}

			return String.Format("[{0}] {1}::{2}()",
				exc.TargetSite.DeclaringType.Assembly.GetName().Name,
				exc.TargetSite.DeclaringType,
				exc.TargetSite.Name);
		}


		private Hashtable GetCustomExceptionInfo (Exception exc)
		{
			Hashtable info = new Hashtable();
			Type baseType = typeof(System.Exception);

			foreach (PropertyInfo property in exc.GetType().GetProperties())
			{
				if (baseType.GetProperty(property.Name) == null)
					info.Add(property.Name, property.GetValue(exc, null));
			}

			return info;
		}


		//========================================================================================
		// IXmlSerializable Implementation
		//========================================================================================

		#region IXmlSerializable Implementation

		/// <summary>
		/// Required implementation of IXmlSerlializable.GetSchema.  This method
		/// returns <b>null</b>.
		/// </summary>

		public System.Xml.Schema.XmlSchema GetSchema ()
		{
			return null;
		}


		/// <summary>
		/// Required implementation of IXmlSerlializable.ReadXml.
		/// </summary>
		/// <param name="reader">
		/// The XmlReader stream from which the object is deserialized.
		/// </param>

		public void ReadXml (XmlReader reader)
		{
		}


		/// <summary>
		/// Required implementation of IXmlSerlializable.ReadXml.
		/// </summary>
		/// <param name="writer">The XmlWriter stream to which the object is serialized.</param>

		public void WriteXml (XmlWriter writer)
		{
			WriteException(writer, this.InnerException);
		}


		private void WriteException (XmlWriter writer, Exception exc)
		{
			writer.WriteAttributeString("etype", exc.GetType().FullName);

			writer.WriteElementString("message", exc.Message);
			writer.WriteElementString("source", exc.Source);

			if (exc.TargetSite != null)
			{
				writer.WriteStartElement("targetSite");
				writer.WriteElementString("assembly", exc.TargetSite.DeclaringType.Assembly.GetName().Name);
				writer.WriteElementString("declaringType", exc.TargetSite.DeclaringType.ToString());
				writer.WriteElementString("name", exc.TargetSite.Name);
				writer.WriteEndElement();
			}

			writer.WriteElementString("stackTrace", exc.StackTrace);
			writer.WriteElementString("helpLink", exc.HelpLink);

			if ((exc.Data != null) && (exc.Data.Count > 0))
			{
				writer.WriteStartElement("data");

				IDictionaryEnumerator e = exc.Data.GetEnumerator();
				while (e.MoveNext())
				{
					writer.WriteElementString(e.Key.ToString(), e.Current.ToString());
				}

				writer.WriteEndElement();
			}

			if (exc.InnerException != null)
			{
				writer.WriteStartElement("innerException");
				WriteException(writer, exc.InnerException);
				writer.WriteEndElement();
			}
		}

		#endregion IXmlSerializable Implementation
	}
}
