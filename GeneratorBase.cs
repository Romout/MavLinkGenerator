using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Reflection;
using System.IO;

namespace MavLinkGenerator
{
	public abstract class GeneratorBase
	{
		protected abstract string LoadTemplate();
		protected abstract void GenerateEnums(XmlDocument document, ref string template);
		protected abstract void GenerateMessages(XmlDocument document, ref string template);

		public abstract string GetArrayDefinitionForType(string type, int arrayLength, int typeSize);

		public abstract Dictionary<string, Tuple<string, int>> TypeMap { get; }
		public abstract Dictionary<string, string> ReservedWords { get; }

		public void Generate(string fileName, string xmlDefinitionFile)
		{
			if (File.Exists(fileName))
				File.Delete(fileName);

			XmlDocument document = new XmlDocument();
			document.Load(xmlDefinitionFile);
			string template = LoadTemplate();

			GenerateEnums(document, ref template);
			GenerateMessages(document, ref template);

			File.WriteAllText(fileName, template);
		}

		protected string CamelCase(string name)
		{
			string[] words = name.Split('_', ' ', '-', '.');
			return string.Join("", words.Select(w => w.Substring(0, 1).ToUpper() + w.Substring(1).ToLower()));
		}

		protected string lowerCamelCase(string name)
		{
			string camelCase = CamelCase(name);
			return camelCase.Substring(0, 1).ToLower() + camelCase.Substring(1);
		}

		protected string LoadTemplate(string name)
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			string[] resourceNames = assembly.GetManifestResourceNames();
			name = name.ToLower();

			foreach (string resourceName in resourceNames)
			{
				if (resourceName.ToLower().EndsWith(name.ToLower()))
				{
					using (Stream stream = assembly.GetManifestResourceStream(resourceName))
					using (StreamReader reader = new StreamReader(stream))
					{
						return reader.ReadToEnd();
					}
				}
			}

			return null;
		}

		protected string Indent(int level)
		{
			return new string('\t', level);
		}

		protected List<FieldDefinition> GetFields(XmlNode messageNode)
		{
			XmlNodeList fieldNodes = messageNode.SelectNodes("field");
			List<FieldDefinition> fields = new List<FieldDefinition>();

			foreach (XmlNode field in fieldNodes)
			{
				FieldDefinition fieldDefinition = new FieldDefinition(this, field);
				if (fieldDefinition.IsValid)
				{
					fieldDefinition.OriginalIndex = fields.Count;
					fields.Add(fieldDefinition);
				}
			}

			// Sort fields at the beginning so we don't need to care about the ordering when serializing/deserializing
			fields.Sort((left, right) =>
			{
				int compareResult = -left.TypeSize.CompareTo(right.TypeSize);
				if (compareResult == 0)
					return left.OriginalIndex.CompareTo(right.OriginalIndex);

				return compareResult;
			});

			return fields;
		}
	}
}
