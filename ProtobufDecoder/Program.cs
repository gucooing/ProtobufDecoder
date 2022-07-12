//#define DDEBUG

/*
 * Created by SharpDevelop.
 * User: User
 * Date: 23.03.2021
 * Time: 8:44
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;

namespace ProtobufDecoder
{
	class Program
	{
		const string assembly_name = "Assembly-CSharp.dll";
		const char default_mode = 'd';
		const char default_type = 'y';
		
		public static void Main(string[] args)
		{
			
			
			#if DDEBUG
			var input_directory = @"Z:\Il2CppDumper-YuanShen\DummyDll_1.4.50";
			var output_directory = @"Z:\pyproto";
			#else
			if (args.Length < 2)
			{
				Usage();
				return;
			}
			var input_directory = args[0];
			var output_directory = args[1];
			#endif
			
			var type = default_type;
			
			if (args.Length > 2) {
				type = args[2][0];
			}
			
			if (type != 'y' && type != 'b') {
				Usage();
				return;
			}
			
			var mode = default_mode;
			
			if (args.Length > 3) {
				mode = args[3][0];
			}
			
			if (mode != 'd' && mode != 'f') {
				Usage();
				return;
			}
			
			AssemblyParser parser = new AssemblyParser(Path.Combine(input_directory, assembly_name));
			
			parser.parse();
			
			DescriptionWriter writer = null;
			
			if (type == 'b') {
				writer = new ProtobufWriter(parser.GetItems());
			} else {
				writer = new PythonPIWriter(parser.GetItems());
			}
			
			if (mode == 'f') {
				writer.DumpToFile(output_directory);
			} else {
				writer.DumpToDirectory(output_directory);
			}
			
			//Console.ReadKey(true);
		}
		
		public static void Usage() {
			var param_string = "\t{0,-15} {1}";
			
			var usage = string.Join(
				Environment.NewLine,
				"Protocol dumper tool for Unity projects",
				"",
				"Usage:",
				string.Format("\t{0} input_dir output_dir [type [mode]]", AppDomain.CurrentDomain.FriendlyName),
				"",
				"Parameters:",
				string.Format(param_string, "input_dir", "Directory where Assembly-CSharp.dll is located"),
				string.Format(param_string, "output_dir", "Directory for generated files (beware of overwriting!)"),
				string.Format(param_string, "type", "Type of definitions, 'y' for pYthon pb-inspector or 'b' for protoBuf itself"),
				string.Format(param_string, "", string.Format("Defaults to '{0}'", default_type)),
				string.Format(param_string, "mode", "Either 'f' for one big File or 'd' for separate files under output Directory"),
				string.Format(param_string, "", string.Format("Defaults to '{0}'", default_mode)),
				""
			);
			Console.WriteLine(usage);
		}
	}
}