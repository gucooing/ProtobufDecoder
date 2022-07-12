/*
 * Created by SharpDevelop.
 * User: User
 * Date: 23.03.2021
 * Time: 9:37
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace ProtobufDecoder
{
	/// <summary>
	/// Description of FieldDescription.
	/// </summary>
	public class FieldDescription : ObjectDescription
	{
		public int Tag = -1;
		
		public string Type = null;
		
		private static Dictionary<string, string> pbTypeNames = new Dictionary<string, string>() {
			{typeof(System.UInt32).FullName, "uint32"},
			{typeof(System.UInt64).FullName, "uint64"},
			{typeof(System.Int32).FullName, "int32"},
			{typeof(System.Int64).FullName, "int64"},
			{typeof(System.Boolean).FullName, "bool"},
			{typeof(System.String).FullName, "string"},
			{typeof(float).FullName, "float"},
			{typeof(double).FullName, "double"},
			{"Google.Protobuf.ByteString", "bytes"},
		};
		
		public FieldDescription(string name, int tag, string type) : base(name)
		{
			Tag = tag;
			Type = type;
		}
		
		public override string[] ToPBLines()
		{
			return new string[]{MapCsTypeToPb(Type) + " " + Name.ToSnakeCase() + " = " + Tag};
		}
		
		public override string[] ToPILines()
		{
			// TODO: type mapping!
			return new string[]{Tag + ": (\"" + MapCsTypeToPb(Type, true, false) + "\", \"" + Name.ToSnakeCase() + "\")"};
		}
		
		public static string MapCsTypeToPb(string typename, bool annotate_enums = false, bool add_repeated = true)
		{
			if (pbTypeNames.ContainsKey(typename))
				return pbTypeNames[typename];
			
			if (typename.StartsWith("Google.Protobuf.Collections.Repeated")) {
				var element_type = typename.Split('<')[1];
				
				element_type = element_type.Substring(0, element_type.Length-1);
				
				var type_name = MapCsTypeToPb(element_type, annotate_enums);
				
				if (add_repeated) {
					return "repeated " + type_name;
				} 
				
				if (pbTypeNames.ContainsKey(element_type)) {
					// This is a primitive type, and it should be packed for protobuf-inspector
					return "packed " + type_name;
				}
				
				return type_name;
			}
			
			if (typename.StartsWith("Google.Protobuf.Collections.MapField") ||
			    typename.StartsWith("Google.Protobuf.Collections.MessageMapField")) {
				var types_part = typename.Split('<');
				var types = types_part[1].Split(',');
				
				var key_type = MapCsTypeToPb(types[0], annotate_enums);
				var value_type = MapCsTypeToPb(types[1].Substring(0, types[1].Length-1), annotate_enums);
				
				return string.Format("map<{0}, {1}>", key_type, value_type);
			}
			
			var name = typename.CutAfterPlusAndDot();
			
			// HACK!
			if (annotate_enums && PythonPIWriter.IsEnum(typename))
				return "enum " + name;
			
			return name; // And pray for the best
		}
	}
}
