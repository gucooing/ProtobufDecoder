﻿/*
 * Created by SharpDevelop.
 * User: User
 * Date: 23.03.2021
 * Time: 9:27
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using System.Collections.Generic;

namespace ProtobufDecoder
{
	/// <summary>
	/// Description of ClassDescription.
	/// </summary>
	public class ClassDescription : ObjectDescription
	{
		public EnumDescription ServiceEnum = null;
		
		public Dictionary<string,FieldDescription> Fields = null;
		
		public Dictionary<string,EnumDescription> Enums = null;
		
		public Dictionary<string,ClassDescription> Classes = null;
		
		public ClassDescription(string name) : base(name)
		{
			Fields = new Dictionary<string,FieldDescription>();
			Enums = new Dictionary<string,EnumDescription>();
			Classes = new Dictionary<string,ClassDescription>();
		}
		
		public string[] GetExternalTypes(bool include_google_types = false)
		{
			// Step 1: Find all the types used
			var used_types = GetUsedTypes();
			
			// Step 2: Find all the types provided
			var provided_types = GetProvidedTypes();
			
			// Step 3: Find the difference, taking into account built-in types, and return it
			var difference = used_types.Except(provided_types);
			
			var types = new List<string>();
			
			foreach (var t in difference) {
				if (t.StartsWith("System."))
					continue;
				
				if (t.StartsWith("Google.") && !include_google_types)
					continue;
				
				types.Add(t);
			}
			
			return types.ToArray();
		}
		
		public string[] GetUsedTypes()
		{
			var types = new List<string>();
			
			foreach (var f in Fields) {
				var od = f.Value as OneofDescription;
				
				if (od != null) {
					foreach (var ff in od.Fields) {
						types.Add(ff.Value.Type); // TODO: pray that there'll be no inner oneofs
					}
				} else {
					types.Add(f.Value.Type);
				}
			}
			
			foreach (var c in Classes) {
				types.AddRange(c.Value.GetUsedTypes());
			}
			
			var extra_types = new List<string>();
			
			// Now iterate through the types and extract element types of maps and lists
			foreach (var t in types) {
				if (t.StartsWith("Google.")) {
					// Extract element's types
					int pos = t.IndexOf('<');
					var types_string = t.Substring(pos+1, t.Length-pos-2);
					var types_arr = types_string.Split(',');
					
					extra_types.AddRange(types_arr);
				}
			}
			
			types.AddRange(extra_types);
			
			return types.Distinct().ToArray();
		}
		
		public string[] GetProvidedTypes()
		{
			var types = new List<string>();
			
			if (ServiceEnum != null)
				types.Add(ServiceEnum.Name);
			
			foreach (var e in Enums)
				types.Add(e.Value.Name);
			
			foreach (var c in Classes) {
				types.Add(c.Value.Name);
				types.AddRange(c.Value.GetProvidedTypes());
			}
			
			return types.ToArray();
		}
		
		public string[] GetEnums() {
			var types = new List<string>();
			
			// TODO: we probably don't need this enum... do we?
			#if false
			if (ServiceEnum != null)
				types.Add(ServiceEnum.Name);
			#endif
			
			foreach (var e in Enums)
				types.Add(e.Value.Name);
			
			foreach (var c in Classes) {
				types.AddRange(c.Value.GetEnums());
			}
			
			return types.ToArray();
		}
		
		public override string[] ToPBLines()
		{
			var lines = new List<string>();
			
			lines.Add("message " + Name.CutAfterPlusAndDot() + " {");
			
			if (ServiceEnum != null) {
				lines.AddRange(ServiceEnum.ToPBLines().PadStrings());
				
				lines.Add("");
			}
			
			if (Enums.Count > 0) {
				foreach (var item in Enums)
					lines.AddRange(item.Value.ToPBLines().PadStrings());
			
				lines.Add("");
			}
			
			if (Classes.Count > 0) {
				foreach (var item in Classes)
					lines.AddRange(item.Value.ToPBLines().PadStrings());
			
				lines.Add("");
			}
			
			if (Fields.Count > 0) {
				foreach (var item in Fields)
					lines.AddRange(item.Value.ToPBLines().PadStrings("\t", (item.Value is OneofDescription) ? "" : ";"));
			}
			
			lines.Add("}");
			
			return lines.ToArray();
		}
		
		public override string[] ToPILines()
		{
			// Logic is a bit different from PB dump
			// First, we dump all fields as root
			// Next, we dump all inner classes and enums
			
			var lines = new List<string>();
			
			lines.Add("\"" + Name.CutAfterPlusAndDot() + "\": {");
			
			foreach (var item in Fields)
					lines.AddRange(item.Value.ToPILines().PadStrings("\t", ","));
			
			lines.Add("},");
			
			lines.Add("");
			
			// TODO: that's an important debug info, but there's no way of including it now
			#if false
			if (ServiceEnum != null) {
				lines.AddRange(ServiceEnum.ToPILines());
				
				lines.Add("");
			}
			#endif
			
			if (Enums.Count > 0) {
				foreach (var item in Enums)
					lines.AddRange(item.Value.ToPILines());
			
				lines.Add("");
			}
			
			if (Classes.Count > 0) {
				foreach (var item in Classes)
					lines.AddRange(item.Value.ToPILines());
			
				lines.Add("");
			}
			
			return lines.ToArray();
		}
	}
}
