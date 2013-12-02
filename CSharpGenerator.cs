using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MavLinkGenerator
{
	public class CSharpGenerator : GeneratorBase
	{
		private static Dictionary<string, Tuple<string, int>> _typeMap = new Dictionary<string, Tuple<string, int>>() {
				{"float"    , new Tuple<string, int>("float", 4)},
				{"double"   , new Tuple<string, int>("double", 8)},
				{"char"     , new Tuple<string, int>("byte", 1)},
				{"int8_t"   , new Tuple<string, int>("sbyte", 1)},
				{"uint8_t"  , new Tuple<string, int>("byte", 1)},
				{"uint8_t_mavlink_version"  , new Tuple<string, int>("byte", 1)}, // Make something special?
				{"int16_t"  , new Tuple<string, int>("Int16", 2)},
				{"uint16_t" , new Tuple<string, int>("UInt16", 2)},
				{"int32_t"  , new Tuple<string, int>("Int32", 4)},
				{"uint32_t" , new Tuple<string, int>("UInt32", 4)},
				{"int64_t"  , new Tuple<string, int>("Int64", 8)},
				{"uint64_t" , new Tuple<string, int>("UInt64", 8)},
			};

		private static Dictionary<string, string> _reservedWords = new Dictionary<string, string>() {
				{"fixed", "fixedField"},
			};


		private void AddComment(StringBuilder sb, string description, int indent = 2)
		{
			description = description.Replace("\n", "\n" + Indent(indent) + "/// ");
			sb.AppendLine(Indent(indent) + "/// <summary>");
			sb.AppendLine(Indent(indent) + "/// " + description);
			sb.AppendLine(Indent(indent) + "/// </summary>");
		}

		public override string GetArrayDefinitionForType(string type, int arrayLength, int typeSize)
		{
			// don't care for the size
			return type + "[]";
		}

		protected override string LoadTemplate()
		{
			return LoadTemplate("CSharpTemplate.cs");
		}

		protected override void GenerateEnums(XmlDocument document, ref string template)
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

				sb.AppendLine(string.Format(Indent(2) + "enum {0}", enumName));
				sb.AppendLine(Indent(2) + "{");
				XmlNodeList enumValues = node.SelectNodes("entry");
				foreach (XmlNode enumValue in enumValues)
				{
					string value = enumValue.GetAttribute("value");
					string name = enumValue.GetAttribute("name");
					string valueDescription = enumValue.ChildValue("description");

					if (string.IsNullOrEmpty(name))
						continue;

					name = CamelCase(name);

					if (!string.IsNullOrEmpty(valueDescription))
						AddComment(sb, valueDescription, 3);

					if (value != null)
						sb.AppendLine(Indent(3) + name + " = " + value + ",");
					else
						sb.AppendLine(Indent(3) + name + ",");
				}
				sb.AppendLine("\t\t}");
			}

			template = template.Replace("/*ENUMS*/", sb.ToString());
		}

		protected override void GenerateMessages(XmlDocument document, ref string template)
		{
			StringBuilder sb = new StringBuilder();

			XmlNodeList messageNodes = document.SelectNodes("/mavlink/messages/message");
			foreach (XmlNode messageNode in messageNodes)
			{
				string messageId = messageNode.GetAttribute("id");
				string name = messageNode.GetAttribute("name");

				if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(name))
					continue;

				name = CamelCase(name);

				sb.AppendLine();

				string description = messageNode.ChildValue("description");
				if (!string.IsNullOrEmpty(description))
					AddComment(sb, description);

				List<FieldDefinition> fields = GetFields(messageNode);
				fields.ForEach(f => f.Name = lowerCamelCase(f.Name));

				sb.AppendLine(string.Format("{0}public class {1} : MessageBase", Indent(2), name));
				sb.AppendLine(Indent(2) + "{");
				//sb.AppendLine(string.Join("\n", fields.Select(f => Indent(3) + "private " + f.Type + " _" + f.Name + ";").ToArray()));
				//sb.AppendLine(string.Format("{0}private object[] _fields = new object[{1}];", Indent(3), fields.Count));
				//sb.AppendLine();
				AddComment(sb, "Static Constructor to register type", 3);
				sb.AppendLine(string.Format("{0}static {1}()", Indent(3), name));
				sb.AppendLine(Indent(3) + "{");
				sb.AppendLine(string.Format("{0}MessageBase._messageTypes.Add({1}, typeof({2}));", Indent(4), messageId, name));
				sb.AppendLine(Indent(3) + "}");
				sb.AppendLine();

				AddComment(sb, "Default Constructor to initialize type fields", 3);
				sb.AppendLine(string.Format("{0}public {1}()", Indent(3), name));
				sb.AppendLine(Indent(3) + "{");
				sb.AppendLine(string.Format("{0}_fieldTypes = new Type[{1}];", Indent(4), fields.Count));
				sb.AppendLine(string.Format("{0}_arrayLengths = new int[{1}];", Indent(4), fields.Count));
				int index = 0;
				sb.AppendLine(string.Join("\n", fields.Select(f => Indent(4) + "_fieldTypes[" + (index++) + "] = typeof(" + f.Type + ");")));
				index = 0;
				sb.AppendLine(string.Join("\n", fields.Select(f => string.Format("{0}_arrayLengths[{1}] = {2};", Indent(4), index++, f.IsArray ? f.ArrayLength : 0))));
				sb.AppendLine(Indent(3) + "}");

				AddComment(sb, "Constructor", 3);
				sb.AppendLine(string.Join("\n", fields.Where(f => !string.IsNullOrEmpty(f.Description)).Select(f => string.Format("{0}/// <param name=\"{1}\">{2}</param>", Indent(3), f.Name, f.Description))));
				sb.AppendLine(string.Format("{0}public {1}({2})", Indent(3), name, string.Join(", ", fields.Select(f => f.Type + " " + f.Name).ToArray())));
				sb.AppendLine(Indent(4) + ": this()");
				sb.AppendLine(Indent(3) + "{");
				sb.AppendLine(string.Format("{0}_messageId = {1};", Indent(4), messageId));
				sb.AppendLine(string.Format("{0}_fields = new object[{1}];", Indent(4), fields.Count));
				sb.AppendLine(string.Format("{0}_payloadLength = {1};", Indent(4), fields.Select(f => {
					if (f.IsArray)
						return f.TypeSize * f.ArrayLength;
					else
						return f.TypeSize;
				}).Sum()));

				sb.AppendLine();

				index = 0;
				sb.AppendLine(string.Join("\n", fields.Select(f => Indent(4) + "_fields[" + (index++) + "] = " + f.Name + ";")));

				sb.AppendLine(Indent(3) + "}");
				for (int i = 0; i < fields.Count; ++i)
				{
					GenerateProperty(sb, fields[i], i, 3);
				}
				sb.AppendLine(Indent(2) + "}");
			}

			template = template.Replace("/*MESSAGES*/", sb.ToString());
		}

		private void GenerateProperty(StringBuilder sb, FieldDefinition field, int index, int indent)
		{
			sb.AppendLine();
			AddComment(sb, field.Description, indent);
			sb.AppendLine(string.Format("{0}public {1} {2}", Indent(indent), field.Type, field.Name));
			sb.AppendLine(Indent(indent) + "{");
			sb.AppendLine(Indent(indent + 1) + "get");
			sb.AppendLine(Indent(indent + 1) + "{");
			sb.AppendLine(string.Format("{0}return ({2})_fields[{1}];", Indent(indent + 2), index, field.Type));
			sb.AppendLine(Indent(indent + 1) + "}");
			sb.AppendLine(Indent(indent + 1) + "set");
			sb.AppendLine(Indent(indent + 1) + "{");
			sb.AppendLine(string.Format("{0}if (({2})_fields[{1}] != value)", Indent(indent + 2), index, field.Type));
			sb.AppendLine(Indent(indent + 2) + "{");
			sb.AppendLine(string.Format("{0}_fields[{1}] = value;", Indent(indent + 3), index));
			sb.AppendLine(string.Format("{0}OnPropertyChanged(\"{1}\");", Indent(indent + 3), field.Name));
			sb.AppendLine(Indent(indent + 2) + "}");
			sb.AppendLine(Indent(indent + 1) + "}");
			sb.AppendLine(Indent(indent) + "}");
		}

		public override Dictionary<string, string> ReservedWords
		{
			get { return _reservedWords; }
		}

		public override Dictionary<string, Tuple<string, int>> TypeMap
		{
			get { return _typeMap; }
		}
	}
}
