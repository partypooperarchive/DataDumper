/*
 * Created by SharpDevelop.
 * User: User
 * Date: 26.10.2021
 * Time: 23:14
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;

namespace DataDumper
{
	/// <summary>
	/// Description of DeReader.
	/// </summary>
	public class DeReader
	{
		// Composition over inheritance
		private BinaryReader reader = null;
		
		public DeReader(string filename)
		{
			reader = new BinaryReader(File.Open(filename, FileMode.Open));
		}
		
		public byte ReadU8() {
			return reader.ReadByte();
		}
		
		/*public int ReadU16() {
			return reader.ReadUInt16();
		}*/
		
		public uint ReadU32() {
			return reader.ReadUInt32();
		}
		
		public ulong ReadU64() {
			return reader.ReadUInt64();
		}
		
		public float ReadF32() {
			return reader.ReadSingle();
		}
		
		public long ReadVarInt() { // From ZigZag-encoded data
			ulong encoded = ReadVarUInt();
			
			var abs_value = (long)(encoded >> 1);
			var sign = (long)(encoded & 1);
			var decoded = abs_value ^ (-sign);
			
			return decoded;
		}
		
		public ulong ReadVarUInt() {
			// Stolen from some repo on Github
			
			int shift = 0;
			ulong result = 0;
			int i = 0; // Foolproof check
			
			// Varint is max 128 bits => 19 bytes (+1 extra just for cute round number)
			for (i = 0; i < 20; i++) {
				ulong byteValue = reader.ReadByte();
				
				ulong tmp = byteValue & 0x7f;
				result |= tmp << shift;
				
				if ((byteValue & 0x80) != 0x80)
					return result;
				
				shift += 7;
			}
			
			throw new IOException("I messed up!");
		}
		
		public string ReadString() {
			var len = ReadVarUInt();
			var bytes = reader.ReadBytes((int)len);
			//var chars = reader.ReadChars((int)len);
			return System.Text.Encoding.UTF8.GetString(bytes);
		}
		
		public void Close() {
			reader.Close();
		}
	}
}
