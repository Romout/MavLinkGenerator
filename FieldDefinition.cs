using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace MavLinkGenerator
{
	public class FieldDefinition
	{
		private static Regex _arrayRegEx = new Regex(@"\[(?<length>\d+)\]", RegexOptions.Compiled);

		public string Type;
		public string Name;
		public string Description;
		public int ArrayLength;
		public int TypeSize;

		public FieldDefinition(GeneratorBase generator, XmlNode field)
		{
			Type = field.GetAttribute("type");
			Name = field.GetAttribute("name");
			Description = field.InnerText;
			ArrayLength = 0;

			string alternativeName;
			if (generator.ReservedWords.TryGetValue(Name, out alternativeName))
				Name = alternativeName;

			Match match = _arrayRegEx.Match(Type);
			if (match.Success)
			{
				// It's an array, modify Type and remember length of array
				ArrayLength = int.Parse(match.Groups["length"].Value);
				Type = Type.Replace(match.Value, "");
			}

			Tuple<string, int> csharpType;
			if (generator.TypeMap.TryGetValue(Type, out csharpType))
			{
				Type = csharpType.Item1;
				TypeSize = csharpType.Item2;
			}

			if (ArrayLength > 0)
			{
				Type = generator.GetArrayDefinitionForType(Type, ArrayLength, TypeSize);
			}
		}

		public bool IsArray
		{
			get
			{
				return ArrayLength > 0;
			}
		}

		public bool IsValid
		{
			get
			{
				return !string.IsNullOrEmpty(Type) && !string.IsNullOrEmpty(Name);
			}
		}

		public int OriginalIndex { get; set; }
	}
}
