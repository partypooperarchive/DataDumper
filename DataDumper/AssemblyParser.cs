//#define DDEBUG
/*
 * Created by SharpDevelop.
 * User: User
 * Date: 26.10.2021
 * Time: 23:02
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace DataDumper
{
	/// <summary>
	/// Description of AssemblyParser.
	/// </summary>
	public class AssemblyParser
	{
		private AssemblyDefinition assembly = null;
		private NumberFormatInfo nfi = null;
		
		public AssemblyParser(string filename)
		{
			nfi = new NumberFormatInfo();
			nfi.NumberDecimalSeparator = ".";
			
			var resolver = new DefaultAssemblyResolver();
			resolver.AddSearchDirectory(Path.GetDirectoryName(filename));
			
			assembly = AssemblyDefinition.ReadAssembly(filename, new ReaderParameters { AssemblyResolver = resolver });
		}
		
		public string ParsePacked(string filename, string classname) {
			var cls = this.FindClass(classname);
			
			if (cls == null) {
				throw new InvalidDataException(string.Format("Class {0} not found!", classname));
			}
				
			var reader = new DeReader(filename);
			
			var count = reader.ReadVarInt();
			
			Console.WriteLine("Parsing {0} with {1} definitions", classname, count);
			
			var items = new List<string>();
			
			for (int i = 0; i < count; i++) {
				WriteLine("============ Parsing definition {0} ===============", i);
				items.Add(this.ParseDefinition(cls, reader));
			}
			
			reader.Close();
			
			return "[" + string.Join(",", items) + "]";
		}
		
		private TypeDefinition FindClass(string classname) {
			// TODO: decompiler behaves all over the place...
			// First, try to find full name
			var cls = assembly.MainModule.Types.FirstOrDefault(t => t.Name.EndsWith("MoleMole.Config."+classname));
			if (cls != null) 
				return cls;
			// Next, try to find just "classname" with correct namespace
			cls = assembly.MainModule.Types.FirstOrDefault(t => t.Name.EndsWith(classname) && t.Namespace.Equals("MoleMole.Config"));
			
			return cls;
		}
		
		private string ParseDefinition(TypeDefinition t, DeReader reader) {
			var bitmask = new BitMask(reader);
			// TODO: HACK!
			FixBitMask(bitmask, t.Name.Split('.').Last());
			//var fields = RearrangeFields(GetAllFields(t)).ToArray();
			var fields = RearrangeProperties(GetAllProperties(t)).ToArray();
			
			var output = new List<string>();
			
			for (int i = 0, j = 0; i < fields.Length && j < fields.Length; i++) {
				var f = fields[i];
				
				if (bitmask == null || bitmask.TestBit(j)) {
					//var ft = f.FieldType;
					var ft = f.PropertyType;
					Write("Field (#{0}) {1}, type {2} = ", j, f.Name, ft.Name);	
					var ret = ParseFieldType(ft, reader);
					WriteLine();
					
					output.Add(string.Format("\"{0}\": {1}", TransformName(f.Name), ret));
				} else {
					WriteLine("Skipping field (#{0}) {1}", j, f.Name);
				}
				
				// HACK: two hash fields are treated like one
				if (!f.Name.EndsWith("HashSuffix")) {
					j++;
				}
			}
			
			return "{" + string.Join(",", output) + "}";
		}
		
		private string ParseFieldType(TypeReference ft, DeReader reader) {
			if (ft.IsArray) {
				var arr = ft as ArrayType;

				var items = new List<string>();
				
				ulong length = 0;
				
				// TODO: dirty hack!
				if (IsUnsignedCount(arr.ElementType) || arr.ElementType.FullName.EndsWith("MoleMole.Config.ElementType")) {
					length = reader.ReadVarUInt();
				} else {
					length = (ulong)reader.ReadVarInt();
				}
				
				Write("({0}) [", length);
				
				for (uint i = 0; i < length; i++) {
					items.Add(ParseFieldType(arr.ElementType, reader));
					Write(" ");
				}
				Write("]");
				
				return "[" + string.Join(",", items) + "]";
			} else if (ft.Resolve().IsEnum) {
				long value = 0;
				if (IsEnumSigned(ft.Resolve()))
					value = reader.ReadVarInt();
				else
					value = (long)reader.ReadVarUInt();
				var s_value = ParseEnumValue(ft.Resolve(), value);
				Write(s_value);
				return "\"" + s_value.ToString() + "\"";
			} else if (ft.FullName.Equals(typeof(string).FullName)) {
				var value = reader.ReadString();
				Write("{0}", value);
				return "\"" + value.ToString() + "\"";
			} else if (ft.FullName.Equals(typeof(uint).FullName)) {
				var value = reader.ReadVarUInt();
				Write(value);
				return value.ToString();
			} else if (ft.FullName.Equals(typeof(Int64).FullName)) {
				var value = reader.ReadVarInt();
				Write(value);
				return value.ToString();
			} else if (ft.FullName.Equals(typeof(Int32).FullName)) {
				var value = reader.ReadVarInt();
				Write(value);
				return value.ToString();
			} else if (ft.FullName.Equals(typeof(UInt16).FullName)) {
				var value = reader.ReadVarUInt();
				Write(value);
				return value.ToString();
			} else if (ft.FullName.Equals(typeof(byte).FullName)) {
				//var value = reader.ReadVarInt();
				var value = reader.ReadU8();
				Write(value);
				return value.ToString();
			} else if (ft.FullName.Equals(typeof(bool).FullName)) {
				var value = Convert.ToBoolean(reader.ReadU8());
				Write(value);
				return value.ToString().ToLower();
			} else if (ft.FullName.Equals(typeof(Single).FullName)) {
				var value = reader.ReadF32();
				Write(value);
				return value.ToString(nfi);
			} else if (ft.FullName.EndsWith("SimpleSafeFloat")) {
				var value = reader.ReadF32();
				Write(value);
				return value.ToString(nfi);
			} else if (ft.FullName.EndsWith("SimpleSafeUInt32")) {
				var value = reader.ReadVarUInt();
				Write(value);
				return value.ToString();
			} else if (ft.FullName.EndsWith("SimpleSafeUInt16")) {
				var value = reader.ReadVarUInt();
				Write(value);
				return value.ToString();
			} else if (ft.FullName.EndsWith("SimpleSafeInt32")) {
				var value = reader.ReadVarInt();
				Write(value);
				return value.ToString();
			} else if (ft.FullName.StartsWith("MoleMole.Config.")) {
				return ParseDefinition(ft.Resolve(), reader);
			} else if (ft.Name.EndsWith("Dictionary`2")) {
				var dt = ft as GenericInstanceType;
				var key_type = dt.GenericArguments[0];
				var value_type = dt.GenericArguments[1];
				
				var items = new List<string>();
				
				ulong length = 0;
				
				//if (IsUnsignedCount(arr.ElementType)) {
					length = reader.ReadVarUInt();
				//} else {
				//	length = (ulong)reader.ReadVarInt();
				//}
				
				Write("({0}) {", length);
				
				for (uint i = 0; i < length; i++) {
					var key_value = ParseFieldType(key_type, reader);
					Write(": ");
					var val_value = ParseFieldType(value_type, reader);
					items.Add(string.Format("\"{0}\": {1}", key_value, val_value));
					Write(" ");
				}
				Write("}");
				
				return "{" + string.Join(",", items) + "}";
				
			} else {
				throw new InvalidOperationException(string.Format("Type {0} is not supported", ft.FullName));
			}
		}
		
		private string ParseArray(TypeReference ft, int depth, DeReader reader) {
			if (depth == 0) {
				return ParseFieldType(ft, reader);
			} else {
				var items = new List<string>();
				
				ulong length = 0;
				
				if (IsUnsignedCount(ft)) {
					length = reader.ReadVarUInt();
				} else {
					length = (ulong)reader.ReadVarInt();
				}
				
				Write("({0}) [", length);
				
				for (uint i = 0; i < length; i++) {
					items.Add(ParseArray(ft, depth - 1, reader));
					Write(" ");
				}
				Write("]");
				
				return "[" + string.Join(",", items) + "]";
			}
		}
		
		private bool IsUnsignedCount(TypeReference t) {
			return t.FullName.Equals(typeof(string).FullName) ||
			    t.FullName.Equals(typeof(uint).FullName) ||
				t.FullName.Equals(typeof(Int64).FullName) ||
				t.FullName.Equals(typeof(Int32).FullName) ||
				t.FullName.Equals(typeof(UInt16).FullName) ||
			    t.FullName.Equals(typeof(byte).FullName) ||
			    t.FullName.Equals(typeof(bool).FullName) ||
				t.FullName.Equals(typeof(Single).FullName) ||
			    t.FullName.EndsWith("SimpleSafeFloat") || 
			    t.FullName.EndsWith("SimpleSafeUInt32") ||
				t.FullName.EndsWith("SimpleSafeUInt16") ||
				t.FullName.EndsWith("SimpleSafeInt32") ||
				(t.Resolve().IsEnum && !IsEnumSigned(t.Resolve())) ||
				t.IsArray;
		}
		
		private bool IsEnumSigned(TypeDefinition t) {		
			foreach (var field in t.Fields) {
				if (field.Name == "value__") {
					var n = field.FieldType.FullName;
					// Possible underlying types: byte, sbyte, short, ushort, int, uint, long, ulong
					return (
						n.Equals(typeof(sbyte).FullName) ||
						n.Equals(typeof(short).FullName) ||
						n.Equals(typeof(int).FullName) ||
						n.Equals(typeof(long).FullName)
					);
				}
			}
			
			throw new ArgumentException("Unable to determine signedness of enum {0}!", t.FullName);
		}
		
		private IEnumerable<FieldDefinition> RearrangeFields(IEnumerable<FieldDefinition> prior) {
			// Fields named "HashSuffix" and "HashPre" come in serialized file in reverse order to specified in assembly
			var ret = new List<FieldDefinition>();
			
			var array = prior.ToArray();
			
			for (int i = 0; i < array.Length; ) {
				if (array[i].Name.EndsWith("HashPre")) {
					ret.Add(array[i+1]);
					ret.Add(array[i]);
					i += 2;
				} else {
					ret.Add(array[i]);
					i++;
				}
			}
			
			return ret;
		}
		
		private IEnumerable<PropertyReference> RearrangeProperties(IEnumerable<PropertyReference> prior) {
			// Fields named "HashSuffix" and "HashPre" come in serialized file in reverse order to specified in assembly
			var ret = new List<PropertyReference>();
			
			var array = prior.ToArray();
			
			for (int i = 0; i < array.Length; ) {
				if (array[i].Name.EndsWith("HashPre")) {
					ret.Add(array[i+1]);
					ret.Add(array[i]);
					i += 2;
				} else {
					ret.Add(array[i]);
					i++;
				}
			}
			
			return ret;
		}
		
		private IEnumerable<FieldDefinition> GetAllFields(TypeDefinition t) {
			var fields = new List<FieldDefinition>();
			
			fields.AddRange(t.Fields.Where(f => 
			                               !f.FullName.StartsWith("System.Func") && // IsFunctionPointer doesn't work for some reason
			                               !f.IsStatic &&
			                               !f.FieldType.Name.Equals("List`1") && // Lists aren't serialized
			                               //!f.FieldType.Name.Equals("Dictionary`2") && // Just as dicts
			                               !f.FieldType.Name.Equals("Nullable`1") && // Just as nullable
			                               !f.IsPublic && // Public fields are skipped
			                               !f.IsPrivate
			                              ));
			
			if (t.BaseType != null)
				fields.AddRange(GetAllFields(t.BaseType.Resolve()));
			
			return fields;
		}
		
		private IEnumerable<PropertyReference> GetAllProperties(TypeDefinition t) {
			var props = new List<PropertyReference>();
			
			props.AddRange(
				t.Properties.Where(p => p.SetMethod != null)
			);
			
			if (t.BaseType != null)
				props.AddRange(GetAllProperties(t.BaseType.Resolve()));
			
			return props;
		}
		
		private string ParseEnumValue(TypeDefinition t, long value) {
			foreach (var field in t.Fields) {
				if (field.Name == "value__")
					continue;
				
				string s_value = value.ToString();
				
				if (field.Constant.ToString().Equals(s_value)) {
					return field.Name;
				}
			}
			
			throw new InvalidDataException(string.Format("Failed to resolve value {0} for enum {1}", value, t.FullName));
		}
		
		private string TransformName(string name) {
			if (name.StartsWith("_"))
				name = name.Substring(1);
			
			if (char.IsLower(name[0]))
				name = char.ToUpper(name[0]) + name.Substring(1);
			
			if (name.EndsWith("RawNum"))
				name = name.Substring(0, name.Length - "RawNum".Length);
			
			return name.ToPascalCase();
		}
		
		private void WriteLine() {
			WriteLine("", null);
		}
		
		private void Write(bool b) {
			Write(b.ToString(), null);
		}
		
		private void Write(ulong u) {
			Write(u.ToString(), null);
		}
		
		private void Write(byte u) {
			Write(u.ToString(), null);
		}
		
		private void Write(float u) {
			Write(u.ToString(), null);
		}
		
		private void Write(string format, params object[] m) {
			#if DEBUG
			Console.Write(format, m);
			#endif
		}
		
		private void WriteLine(string format, params object[] m) {
			#if DEBUG
			Console.WriteLine(format, m);
			#endif
		}
		
		// TODO: dirty hack!
		private void FixBitMask(BitMask b, string classname) {
			if (classname.Equals("DialogExcelConfig")) {
				b.PushBit(0, 0);
				b.PopBit(6);
				b.PopBit(11);
				b.PopBit(11);
				b.PopBit(11);
			} else if (classname.Equals("CoopInteractionExcelConfig")) {
				b.PopBit(1);
			} else if (classname.Equals("GadgetExcelConfig")) {
				b.PopBit(1);
				b.PopBit(9);
				b.PopBit(17); // May be 16 or 18?
			} else if (classname.Equals("MainCoopExcelConfig")) {
				b.PopBit(1);
			} else if (classname.Equals("MonsterExcelConfig")) {
				b.PopBit(2);
			} else if (classname.Equals("NpcExcelConfig")) {
				b.PopBit(19); // Or 20
			} else if (classname.Equals("RefreshIndexExcelConfig")) {
				b.PopBit(4);
			} else if (classname.Equals("WeatherExcelConfig")) {
				b.PopBit(3);
			} 
		}
	}
}
