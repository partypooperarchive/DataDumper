//#define DDEBUG
/*
 * Created by SharpDevelop.
 * User: User
 * Date: 26.10.2021
 * Time: 22:58
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;

namespace DataDumper
{
	class Program
	{
		const string assembly_name = "Assembly-CSharp.dll";
		const char default_mode = 'p';
		
		public static void Main(string[] args)
		{
			string dll_path = null;
			string filename = null;
			string output_file = null;
			char mode = default_mode;
			string class_name = null;
			
			#if DEBUG
			//dll_path = @"Z:\Il2CppDumper\OUT-2.2.0-dev\DummyDll";
			dll_path = @"Z:\Il2CppDumper\OUT-2.2.0-Rel\DummyDll";
			filename = @"Z:\DD\HuntingClueMonsterExcelConfigData.bin";
			output_file = @"Z:\DD\output.json";
			#else
			Console.WriteLine("THIS IS A TRIAL VERSION!");
			Console.WriteLine("To acquire a full-featured build, send your ID to <recruit@kgb.su> and wait for further instructions");
			Console.WriteLine("");
			
			if (args.Length < 3)
			{
				Usage();
				return;
			}
			dll_path = args[0];
			filename = args[1];
			output_file = args[2];

			if (args.Length > 3)
				mode = args[3][0];

			if (args.Length > 4)
				class_name = args[4];
			#endif
			
			if (class_name == null)
				class_name = DeriveClassName(filename);
			
			if (class_name == null)
				throw new ArgumentNullException("Failed to derive classname! Please specify manually");
			
			var parser = new AssemblyParser(Path.Combine(dll_path, assembly_name));
			
			string output = null;
			
			if (mode == 'p')
				output = parser.ParsePacked(filename, class_name);
			else
				throw new NotImplementedException(string.Format("Mode {0} is not implemented!", mode));
			//var output = parser.ParseSingle(filename, class_name);
			
			File.WriteAllText(output_file, output);
			
			#if DEBUG
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
			#endif
		}
		
		public static void Usage() {
			var param_string = "\t{0,-15} {1}";
			
			var usage = string.Join(
				Environment.NewLine,
				"Data dumper tool",
				"",
				"Usage:",
				string.Format("\t{0} input_dir input_file output_file [mode [class_name]]", AppDomain.CurrentDomain.FriendlyName),
				"",
				"Parameters:",
				string.Format(param_string, "input_dir", "Directory where Assembly-CSharp.dll is located"),
				string.Format(param_string, "input_file", "Binary input file (decrypted)"),
				string.Format(param_string, "output_file", "Path to the output file (beware of overwriting!)"),
				string.Format(param_string, "mode", "Either 'p' for Packed file (ExcelBinOutput) or UNIMPLEMENTED"),
				string.Format(param_string, "", string.Format("Defaults to '{0}'", default_mode)),
				string.Format(param_string, "class_name", "Name of the class to deserialize"),
				string.Format(param_string, "", string.Format("If omitted, tool will try to derive it from input_file (and may fail)")),
				""
			);
			Console.WriteLine(usage);
		}
		
		public static string DeriveClassName(string filepath) {
			var filename = Path.GetFileNameWithoutExtension(filepath);
			if (filename.EndsWith("Data"))
				return filename.Substring(0, filename.Length-4);
			
			return null;
		}
	}
}