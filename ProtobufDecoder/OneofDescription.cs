/*
 * Created by SharpDevelop.
 * User: User
 * Date: 23.03.2021
 * Time: 18:16
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace ProtobufDecoder
{
	/// <summary>
	/// Description of OneofDescription.
	/// </summary>
	public class OneofDescription : FieldDescription
	{
		public Dictionary<string,FieldDescription> Fields = null;
		
		public OneofDescription(string name) : base(name, -1, "")
		{
			Fields = new Dictionary<string, FieldDescription>();
		}
		
		public override string[] ToPBLines() {
			var lines = new List<string>();
			
			lines.Add("oneof " + Name.CutAfterPlusAndDot() + " {");
			
			foreach (var item in Fields)
			{
				lines.AddRange(item.Value.ToPBLines().PadStrings("\t", ";"));
			}
			
			lines.Add("}");
			
			return lines.ToArray();
		}
		
		public override string[] ToPILines() {
			// Oneofs aren't directly supported by protobuf-inspector, so we'll just put comments marking it's start/end
			var lines = new List<string>();
			
			lines.Add("# oneof " + Name.CutAfterPlusAndDot() + " {");
			
			foreach (var item in Fields)
			{
				// Note that we don't want to pad it
				lines.AddRange(item.Value.ToPILines().PadStrings(""));
			}
			
			lines.Add("# }");
			
			return lines.ToArray();
		}
	}
}
