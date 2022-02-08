/*
 * Created by SharpDevelop.
 * User: User
 * Date: 26.10.2021
 * Time: 23:43
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace DataDumper
{
	/// <summary>
	/// Description of BitMask.
	/// </summary>
	public class BitMask
	{
		uint[] mask = null;
		ulong len = 0;
		
		public BitMask(DeReader reader)
		{
			len = reader.ReadVarUInt();
			mask = new uint[len / sizeof(uint) + 1];
			
			for (uint i = 0; i < len / sizeof(uint); i++) {
				mask[i] = reader.ReadU32();
			}
			
			// Read leftovers
			for (uint i = 0; i < len % sizeof(uint); i++) {
				mask[len/sizeof(uint)] |= ((uint)reader.ReadU8() << (int)(i*8));
			}
			
			#if false
			for (int i = 0; i < (int)len * 8; i++) {
				Console.WriteLine("Bit {0} : {1}", i, TestBit(i));
			}
			#endif
		}
		
		public void SetBit(int b) {
			uint a = (uint)b;
			
			if (a > len*8) {
				throw new ArgumentOutOfRangeException("b");
			}
			
			mask[a >> 5] |= (1u << (int)(a & 0x1F));
		}
		
		public void ResetBit(int b) {
			uint a = (uint)b;
			
			if (a > len*8) {
				throw new ArgumentOutOfRangeException("b");
			}
			
			mask[a >> 5] &= ~(1u << (int)(a & 0x1F));
		}
		
		public bool TestBit(int b) {
			uint a = (uint)b;
			
			if (a > len*8) {
				//throw new ArgumentOutOfRangeException("b");
				return false;
			}
			
			return (mask[a >> 5] & (1u << (int)(a & 0x1F))) != 0;
		}
		
		public void PopBit(int b) {
			// TODO: highly inefficient!
			for (int i = b; i < (int)(len*8-1); i++) {
				if (TestBit(i+1))
					SetBit(i);
				else
					ResetBit(i);
			}
		}
		
		public void PushBit(int b, byte v) {
			// TODO: highly inefficient!
			uint a = (uint)b;
			
			if (a > len*8) {
				Array.Resize(ref mask, mask.Length+1);
				len += 1;
			}
			
			for (int i = (int)len*8-1; i >= a; i--) {
				if (TestBit(i))
					SetBit(i+1);
				else
					ResetBit(i+1);
			}
			
			if (v != 0)
				SetBit(b);
			else
				ResetBit(b);
		}
	}
}
