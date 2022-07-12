/*
 * Created by SharpDevelop.
 * User: User
 * Date: 23.03.2021
 * Time: 8:45
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace ProtobufDecoder
{
	/// <summary>
	/// Description of AssemblyParser.
	/// </summary>
	public class AssemblyParser
	{
		static Assembly assembly = null;
		
		Dictionary<string,ObjectDescription> items = null;
		
		public AssemblyParser(string filename)
		{
			var file_info = new FileInfo(filename);
			assembly = Assembly.LoadFile(file_info.FullName);
			items = new Dictionary<string, ObjectDescription>();
			
			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);
		}
		
		public Dictionary<string,ObjectDescription> GetItems()
		{
			return items;
		}
		
		public void parse()
		{
			foreach (Type type in GetProtobufTypes())
			{
				ObjectDescription id = type.IsEnum ? (ObjectDescription)parseEnum(type) : (ObjectDescription)parseClass(type);
				
				items.Add(type.FullName, id);
			}
		}
		
		private ClassDescription parseClass(Type t)
		{
			ClassDescription cd = new ClassDescription(t.FullName);
			
			// First, get inner classes and enums
			var inner_types = t.GetNestedTypes(BindingFlags.Public|BindingFlags.Static);
			
			var oneof_dict = new Dictionary<int, OneofDescription>();
			
			foreach (var inner_type in inner_types) {
				if (inner_type.Name.Equals("Types")) {
					// That's the static class that contains enum with CmdId, EnetChannelId and EnetIsReliable
					// As well as all other enums and classes
					var members = inner_type.GetMembers(BindingFlags.Public | BindingFlags.DeclaredOnly);
					
					var enums = members.Where(m => m.MemberType == MemberTypes.NestedType && (m as Type).IsEnum);
					
					var classes = members.Where(m => m.MemberType == MemberTypes.NestedType && !(m as Type).IsEnum);
					
					foreach (var e in enums) {
						var e_type = e as Type;
						var ed = parseEnum(e_type);
						
						// TODO: ugly, but...
						if (ed.Items.ContainsKey("CmdId"))
							cd.ServiceEnum = ed;
						else
							cd.Enums.Add(ed.Name, ed);
					}
					
					foreach (var c in classes) {
						var c_type = c as Type;
						ClassDescription icd = parseClass(c_type);
						cd.Classes.Add(c_type.Name, icd);
					}
				} else if (inner_type.Name.EndsWith("OneofCase")) {
					// This is an enum used to describe different options for some union
					EnumDescription ed = parseEnum(inner_type);
					
					OneofDescription od = new OneofDescription(ed.Name.Substring(0, ed.Name.Length - "OneofCase".Length));
					
					foreach (var id in ed.Items.Values) {
						if (id.Value != 0) // We don't need an explicit "None"
							oneof_dict.Add(id.Value, od);
					}
					
					cd.Fields.Add(od.Name, od);
				}
			}
			
			// Next, get all fields and properties
			
			var fields = t.GetFields(BindingFlags.Public|BindingFlags.Static|BindingFlags.NonPublic);
			
			var protobufIndices = fields.Where(f => f.Name.EndsWith("FieldNumber"));
			
			var properties = new Dictionary<string,PropertyInfo>();
			
			foreach (var prop in t.GetProperties(BindingFlags.Public|BindingFlags.Instance)) {
				properties.Add(prop.Name, prop);
			}
			
			foreach (var field in protobufIndices) {
				var field_name = field.Name.Remove(field.Name.Length - "FieldNumber".Length);
				var field_tag = (int)field.GetRawConstantValue();
				
				//Console.WriteLine("Processing field " + field_name + " with tag " + field_tag);
				
				var field_property = properties[field_name];
				
				var field_type_name = field_property.GetPropertyTypeName();
				
				if (field_type_name.StartsWith("Google.Protobuf.Collections.MapField") ||
				    field_type_name.StartsWith("Google.Protobuf.Collections.MessageMapField")
				   ) {
					// Those types can't be resolved in runtime due to missing implementation of some crucial methods.
					// Generic type name can be determined, but without generic parameters it's useless. 
					// Luckily for us, there're private fields with more complete type names we can use.
					// If out property is named "PooPee", then the field in question will be "_map_pooPee_codec".
					var private_field_name = string.Format("_map_{0}_codec", field_name.LowerFirstLetter());
					
					var private_field = fields.First(f => f.Name == private_field_name);
					
					if (private_field != null) {
						field_type_name = TransformMapTypeName(private_field.FieldType.FullName);
					} else {
						throw new Exception("PeePoo: " + field_name);
					}
				} else if (field_type_name.StartsWith("Google.Protobuf.Collections.Repeated")) {
					// Those types are resolved successfully, but their names are generic => useless
					// We need to extract info about element type.
					// As with maps, there're a private field with more complete type.
					// If out property is named "PooPee", then the field in question will be "_repeated_pooPee_codec".
					// HACK: there's this one field named like "PooPee_" (with last underscore), but it's private field drops that underscore.
					// We need to workaround that, hence PrepareUranus.
					var private_field_name = string.Format("_repeated_{0}_codec", field_name.LowerFirstLetter().PrepareUranus());
					
					var private_field = fields.FirstOrDefault(f => f.Name == private_field_name);
					
					if (private_field != null) {
						field_type_name = TransformRepeatedTypeName(private_field.FieldType.FullName);
					} else {
						throw new Exception("PeePoo: " + field_name);
					}
				}
				
				//Console.WriteLine(field_type_name);
				
				FieldDescription fd = new FieldDescription(field_name, field_tag, field_type_name);
				
				// If this field is a part of "Oneof", add it there, else into the class directly
				if (oneof_dict.ContainsKey(field_tag)) {
					oneof_dict[field_tag].Fields.Add(field_name, fd);
				} else {
					cd.Fields.Add(field_name, fd);
				}
			}
			
			Console.WriteLine("Processed class " + t.FullName);
			
			return cd;
		}
		
		private EnumDescription parseEnum(Type t)
		{
			EnumDescription ed = new EnumDescription(t.FullName);
			
			var names = Enum.GetNames(t);
			
			foreach (var name in names) {
				var value = Enum.Parse(t, name);
				
				ItemDescription id = new ItemDescription(name, (int)value);
				
				ed.Items.Add(name, id);
			}
			
			return ed;
		}
		
		private string TransformMapTypeName(string name)
		{
			// name looks something like this:
			// "Google.Protobuf.Collections.MapField`2+Codec[[System.UInt32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.UInt32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"
			
			// Generally speaking, we are only interested in the two types contained, 'cause Protobuf maps aren't recursive (lucky us!)
			// Cut off the "Codec" part
			var types_part = name.Substring(name.IndexOf('['));
			// Now we have 
			// "[[System.UInt32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.UInt32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"
			var types = types_part.Split(']');
			// types[0] = "[[System.UInt32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
			// types[1] = ",[System.UInt32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]"
			// types[2] = "", but nobody cares
			var key_type   = types[0].Substring(2, types[0].IndexOf(' ')-3);
			var value_type = types[1].Substring(2, types[1].IndexOf(' ')-3);
			return string.Format("Google.Protobuf.Collections.MapField<{0},{1}>", key_type, value_type);
		}
		
		private string TransformRepeatedTypeName(string name)
		{
			// name looks like
			// "Google.Protobuf.FieldCodec`1[[Proto.AbilityInvokeEntry, Assembly-CSharp, Version=3.7.1.6, Culture=neutral, PublicKeyToken=null]]"
			var element_type = name.Split('[')[2].Split(',')[0]; // Hahaha
			
			// TODO: there're actually two differrent Repeated* collections (RepeatedMessage and RepeatedField), but that doesn't matter... right?
			return string.Format("Google.Protobuf.Collections.RepeatedField<{0}>", element_type);
		}
		
		/* 
		 * Assembly loading stuff
		 */
		
		static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
		{
			string folder = Path.GetDirectoryName(assembly.Location);
			string path = Path.Combine(folder, new AssemblyName(args.Name).Name + ".dll");
			return Assembly.LoadFrom(path);
		}
		
		private Type[] GetTypes()
		{
			try {
				return assembly.GetTypes();
			} catch (ReflectionTypeLoadException e) {
				return e.Types.Where(t => t != null).ToArray();
			}
		}
		
		private Type[] GetProtobufTypes()
		{
			return GetTypes().Where(t => t.FullName.StartsWith("Proto.") && !t.FullName.Contains("+")).ToArray();
		}
	}
	
	public static partial class Extension {
		public static string GetPropertyTypeName(this PropertyInfo property)
		{
			try {
				return property.PropertyType.FullName;
			} catch (TypeLoadException e) {
				return e.TypeName;
			}
		}
		
		public static string LowerFirstLetter(this string s)
		{
			return char.ToLower(s[0]) + s.Substring(1);
		}
		
		public static string PrepareUranus(this string s)
		{
			if (s.EndsWith("_"))
				return s.Substring(0, s.LastIndexOf("_"));
			
			return s;
		}
	}
}
