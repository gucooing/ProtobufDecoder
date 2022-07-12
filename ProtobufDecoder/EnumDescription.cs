/*
 * Created by SharpDevelop.
 * User: User
 * Date: 23.03.2021
 * Time: 9:28
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using System.Collections.Generic;

namespace ProtobufDecoder
{
	/// <summary>
	/// Description of EnumDescription.
	/// </summary>
	public class EnumDescription : ObjectDescription
	{
		public Dictionary<string,ItemDescription> Items = null;
		
		public EnumDescription(string name) : base(name)
		{
			Items = new Dictionary<string,ItemDescription>();
		}
		
		public override string[] ToPBLines()
		{
			var lines = new List<string>();
			
			lines.Add("enum " + Name.CutAfterPlusAndDot() + " {");
			
			// If enum has duplicates, we need to add directive
			var fuckYouMihoyo = Items.Values.Select(i => i.Value);
			
			bool has_dupes = fuckYouMihoyo.Count() != fuckYouMihoyo.Distinct().Count();
			
			if (has_dupes)
				lines.Add("\toption allow_alias = true;");
			
			foreach (var item in Items)
			{			
				lines.AddRange(item.Value.ToPBLines().PadStrings("\t", ";"));
			}
			
			lines.Add("}");
			
			return lines.ToArray();
		}
		
		public override string[] ToPILines()
		{
			var lines = new List<string>();
			
			lines.Add("\"enum " + Name.CutAfterPlusAndDot() + "\": {");
			
			// If enum has duplicates, we need to comment out them
			var added = new HashSet<int>();
			
			foreach (var item in Items)
			{			
				lines.AddRange(item.Value.ToPILines().PadStrings(added.Contains(item.Value.Value) ? "\t#" : "\t", ","));
				added.Add(item.Value.Value);
			}
			
			lines.Add("},");
			
			return lines.ToArray();
		}
	}
}
