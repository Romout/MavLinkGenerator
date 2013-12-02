using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Xml
{
	public static class XmlExtensions
	{
		public static string GetAttribute(this XmlNode node, string name)
		{
			XmlAttribute attribute = node.Attributes[name];
			if (attribute != null)
				return attribute.Value;

			return null;
		}

		public static string ChildValue(this XmlNode node, string childTag)
		{
			foreach (XmlNode child in node.ChildNodes)
			{
				if (child.Name == childTag)
					return child.InnerText;
			}

			return null;
		}
	}
}
