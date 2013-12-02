using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MavLinkGenerator
{
	public class GoGenerator : GeneratorBase
	{
		private static Dictionary<string, Tuple<string, int>> _typeMap = new Dictionary<string,Tuple<string,int>>() {
			{"float"    , new Tuple<string, int>("float32", 4)},
			{"double"   , new Tuple<string, int>("float64", 8)},
			{"char"     , new Tuple<string, int>("byte", 1)},
			{"int8_t"   , new Tuple<string, int>("int8", 1)},
			{"uint8_t"  , new Tuple<string, int>("uint8", 1)},
			{"uint8_t_mavlink_version"  , new Tuple<string, int>("uint8", 1)}, // Make something special?
			{"int16_t"  , new Tuple<string, int>("int16", 2)},
			{"uint16_t" , new Tuple<string, int>("uint16", 2)},
			{"int32_t"  , new Tuple<string, int>("int32", 4)},
			{"uint32_t" , new Tuple<string, int>("uint32", 4)},
			{"int64_t"  , new Tuple<string, int>("int64", 8)},
			{"uint64_t" , new Tuple<string, int>("uint64", 8)},
		};

		private static Dictionary<string, string> _reservedWords = new Dictionary<string, string>();

		public override string GetArrayDefinitionForType(string type, int arrayLength, int typeSize)
		{
			return string.Format("[{0}]{1}", arrayLength, type);
		}

		protected override string LoadTemplate()
		{
			return LoadTemplate("GoTemplate.go");
		}

		private void AddComment(StringBuilder sb, string description, int indent = 0)
		{
			description = description.Replace("\n", "\n" + Indent(indent) + "// ");
			sb.AppendLine(Indent(indent) + "// " + description);
		}

		protected override void GenerateEnums(System.Xml.XmlDocument document, ref string template)
		{
			StringBuilder sb = new StringBuilder();

			XmlNodeList nodes = document.SelectNodes("/mavlink/enums/enum");
			foreach (XmlNode node in nodes)
			{
				string enumName = node.GetAttribute("name");
				if (string.IsNullOrEmpty(enumName))
					continue;

				enumName = CamelCase(enumName);
				sb.AppendLine();

				string description = node.ChildValue("description");
				if (!string.IsNullOrEmpty(description))
					AddComment(sb, description);

				//sb.AppendLine(string.Format("type {0} byte", enumName));
				//sb.AppendLine();

				XmlNodeList enumValues = node.SelectNodes("entry");
				
				sb.AppendLine("const (");
				bool iotaWritten = false;
				int lastValue = 0;
				for (int i = 0; i < enumValues.Count; ++i)
				{
					XmlNode enumValue = enumValues[i];

					string value = enumValue.GetAttribute("value");
					string name = enumValue.GetAttribute("name");
					string valueDescription = enumValue.ChildValue("description");

					if (string.IsNullOrEmpty(name))
						continue;

					if (!string.IsNullOrEmpty(valueDescription))
						AddComment(sb, valueDescription, 1);

					sb.Append(Indent(1) + name);
					if (i == 0)
					{
						//sb.Append(" " + enumName);
						if (value == null)
						{
							sb.Append(" = iota");
							iotaWritten = true;
						}
						else
						{
							sb.Append(" = " + value);
							lastValue = int.Parse(value);
						}

						sb.AppendLine();
					}
					else
					{
						if (value != null)
							sb.AppendLine(" = " + value);
						else if (iotaWritten)
							sb.AppendLine();
						else
							sb.AppendLine(" = " + (++lastValue).ToString());
					}
				}
				sb.AppendLine(")");
			}

			template = template.Replace("/*ENUMS*/", sb.ToString());
		}

		protected override void GenerateMessages(System.Xml.XmlDocument document, ref string template)
		{
			StringBuilder sb = new StringBuilder();

			XmlNodeList messageNodes = document.SelectNodes("/mavlink/messages/message");

			Dictionary<int, string> messages = new Dictionary<int,string>();

			foreach (XmlNode messageNode in messageNodes)
			{
				string messageId = messageNode.GetAttribute("id");
				string name = messageNode.GetAttribute("name");

				if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(name))
					continue;

				name = CamelCase(name);
				messages.Add(int.Parse(messageId), name);

				sb.AppendLine();

				string description = messageNode.ChildValue("description");
				if (!string.IsNullOrEmpty(description))
					AddComment(sb, description);

				List<FieldDefinition> fields = GetFields(messageNode);
				fields.ForEach(f => f.Name = CamelCase(f.Name));

				sb.AppendLine(string.Format("type {0} struct {{", name));
				sb.AppendLine(string.Join("\n", fields.Select(f => string.Format("{0}{1}\t{2}{3}", Indent(1), f.Name, f.Type, (!string.IsNullOrEmpty(f.Description) ? "\t// " + f.Description : "")))));
				sb.AppendLine("}");

				sb.AppendLine();
				sb.AppendLine(string.Format("func(self *{0}) ID() uint8 {{\n\treturn {1}\n}}", name, messageId));
				sb.AppendLine();
				int size = fields.Select(f =>
				{
					if (f.IsArray)
						return f.TypeSize * f.ArrayLength;
					else
						return f.TypeSize;
				}).Sum();

				sb.AppendLine(string.Format("func(self *{0}) Size() uint8 {{\n\treturn {1}\n}}", name, size));
			}

			string factory = string.Join("\n", messages.Select(k => string.Format("{0}{1}: func() Message {{ return new({2}) }},", Indent(1), k.Key, k.Value)));

			template = template.Replace("/*MESSAGEFACTORY*/", factory);
			template = template.Replace("/*MESSAGES*/", sb.ToString());
		}

		public override Dictionary<string, Tuple<string, int>> TypeMap
		{
			get { return _typeMap; }
		}

		public override Dictionary<string, string> ReservedWords
		{
			get { return _reservedWords; }
		}
	}
}
