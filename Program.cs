using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MavLinkGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			Program program = new Program();
			program.Run();
		}

		private void Run()
		{
			GeneratorBase generator = new CSharpGenerator();
			generator.Generate("output.cs", "common.xml");

			generator = new GoGenerator();
			//generator.Generate("//BEAGLEBONE/root/development/go/src/testing/mavLink/mavLink.go", "common.xml");
			generator.Generate("messages.go", "common.xml");
		}
	}
}

