/*
 * Created by SharpDevelop.
 * User: User
 * Date: 23.03.2021
 * Time: 23:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace ProtobufDecoder
{
	/// <summary>
	/// Description of Extension.
	/// </summary>
	public static partial class Extension
	{
		public static string ToSnakeCase(this string text)
		{
			if(text.Length < 2) {
				return text;
			}
			var sb = new StringBuilder();
			sb.Append(char.ToLowerInvariant(text[0]));
			for(int i = 1; i < text.Length; ++i) {
				char c = text[i];
				if(char.IsUpper(c)) {
					sb.Append('_');
					sb.Append(char.ToLowerInvariant(c));
				} else {
					sb.Append(c);
				}
			}
			return sb.ToString();
		}
		
		public static string CutAfterPlusAndDot(this string name) {
			int plus_pos = name.LastIndexOf('+');
			
			if (plus_pos >-1)
				name = name.Substring(plus_pos+1);
			
			int dot_pos = name.LastIndexOf('.');
			
			if (dot_pos > -1)
				name = name.Substring(dot_pos+1);
			
			return name;
		}
		
		public static string[] PadStrings(this string[] lines, string left_pad = "\t", string right_pad = "")
		{
			var ret = new List<string>();
			
			foreach (var line in lines)
				ret.Add(left_pad + line + right_pad);
			
			return ret.ToArray();
		}
	}
}
