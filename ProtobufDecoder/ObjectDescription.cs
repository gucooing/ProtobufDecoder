/*
 * Created by SharpDevelop.
 * User: User
 * Date: 23.03.2021
 * Time: 9:26
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace ProtobufDecoder
{
	/// <summary>
	/// Description of ItemDescription.
	/// </summary>
	public abstract class ObjectDescription
	{
		public string Name;
		
		public ObjectDescription(string name)
		{
			Name = name;
		}
		
		public abstract string[] ToPILines();
		
		public abstract string[] ToPBLines();
	}
}
