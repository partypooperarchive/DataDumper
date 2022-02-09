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
using System.Globalization;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataDumper
{
	/// <summary>
	/// Description of Extension.
	/// </summary>
	public static partial class Extension
	{
		public static string ToSnakeCase(this string text)
		{
			return text;
			
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
		
		public static string ToPascalCaseOld(this string text)
		{
			var s = text.ToLower().Replace("_", " ");
			TextInfo info = CultureInfo.CurrentCulture.TextInfo;
			
			return info.ToTitleCase(s).Replace(" ", string.Empty);
		}
		
		public static string ToPascalCase(this string original)
		{
			Regex invalidCharsRgx = new Regex("[^_a-zA-Z0-9]");
			Regex whiteSpace = new Regex(@"(?<=\s)");
			Regex startsWithLowerCaseChar = new Regex("^[a-z]");
			Regex firstCharFollowedByUpperCasesOnly = new Regex("(?<=[A-Z])[A-Z0-9]+$");
			Regex lowerCaseNextToNumber = new Regex("(?<=[0-9])[a-z]");
			Regex upperCaseInside = new Regex("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");

			// replace white spaces with undescore, then replace all invalid chars with empty string
			var pascalCase = invalidCharsRgx.Replace(whiteSpace.Replace(original, "_"), string.Empty)
			                  // split by underscores
        					.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
			                  // set first letter to uppercase
        					.Select(w => startsWithLowerCaseChar.Replace(w, m => m.Value.ToUpper()))
			                  // replace second and all following upper case letters to lower if there is no next lower (ABC -> Abc)
        					//.Select(w => firstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLower()))
			                  // set upper case the first lower case following a number (Ab9cd -> Ab9Cd)
        					.Select(w => lowerCaseNextToNumber.Replace(w, m => m.Value.ToUpper()))
			                  // lower second and next upper case letters except the last if it follows by any lower (ABcDEf -> AbcDef)
        					.Select(w => upperCaseInside.Replace(w, m => m.Value.ToLower()));

			return string.Concat(pascalCase);
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
