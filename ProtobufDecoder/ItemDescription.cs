﻿/*
 * Created by SharpDevelop.
 * User: User
 * Date: 23.03.2021
 * Time: 9:40
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProtobufDecoder
{
	/// <summary>
	/// Description of ItemDescription.
	/// </summary>
	public class ItemDescription : ObjectDescription
	{
		public int Value = -1;
		
		public ItemDescription(string name, int value) : base(name)
		{
			Value = value;
		}
		
		public override string[] ToPBLines()
		{
			return new string[]{Name.ToSnakeCase().ToUpper() + " = " + Value.ToString()};
		}
		
		public override string[] ToPILines()
		{
			return new string[]{Value.ToString() + ": \"" + Name.ToSnakeCase().ToUpper() + "\""};
		}
	}
}
