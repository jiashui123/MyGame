using System;

namespace Photon.SocketServer.Numeric
{
	internal class BigInteger
	{
		private const int maxLength = 70;

		public static readonly int[] primesBelow2000 = new int[303]
		{
			2,
			3,
			5,
			7,
			11,
			13,
			17,
			19,
			23,
			29,
			31,
			37,
			41,
			43,
			47,
			53,
			59,
			61,
			67,
			71,
			73,
			79,
			83,
			89,
			97,
			101,
			103,
			107,
			109,
			113,
			127,
			131,
			137,
			139,
			149,
			151,
			157,
			163,
			167,
			173,
			179,
			181,
			191,
			193,
			197,
			199,
			211,
			223,
			227,
			229,
			233,
			239,
			241,
			251,
			257,
			263,
			269,
			271,
			277,
			281,
			283,
			293,
			307,
			311,
			313,
			317,
			331,
			337,
			347,
			349,
			353,
			359,
			367,
			373,
			379,
			383,
			389,
			397,
			401,
			409,
			419,
			421,
			431,
			433,
			439,
			443,
			449,
			457,
			461,
			463,
			467,
			479,
			487,
			491,
			499,
			503,
			509,
			521,
			523,
			541,
			547,
			557,
			563,
			569,
			571,
			577,
			587,
			593,
			599,
			601,
			607,
			613,
			617,
			619,
			631,
			641,
			643,
			647,
			653,
			659,
			661,
			673,
			677,
			683,
			691,
			701,
			709,
			719,
			727,
			733,
			739,
			743,
			751,
			757,
			761,
			769,
			773,
			787,
			797,
			809,
			811,
			821,
			823,
			827,
			829,
			839,
			853,
			857,
			859,
			863,
			877,
			881,
			883,
			887,
			907,
			911,
			919,
			929,
			937,
			941,
			947,
			953,
			967,
			971,
			977,
			983,
			991,
			997,
			1009,
			1013,
			1019,
			1021,
			1031,
			1033,
			1039,
			1049,
			1051,
			1061,
			1063,
			1069,
			1087,
			1091,
			1093,
			1097,
			1103,
			1109,
			1117,
			1123,
			1129,
			1151,
			1153,
			1163,
			1171,
			1181,
			1187,
			1193,
			1201,
			1213,
			1217,
			1223,
			1229,
			1231,
			1237,
			1249,
			1259,
			1277,
			1279,
			1283,
			1289,
			1291,
			1297,
			1301,
			1303,
			1307,
			1319,
			1321,
			1327,
			1361,
			1367,
			1373,
			1381,
			1399,
			1409,
			1423,
			1427,
			1429,
			1433,
			1439,
			1447,
			1451,
			1453,
			1459,
			1471,
			1481,
			1483,
			1487,
			1489,
			1493,
			1499,
			1511,
			1523,
			1531,
			1543,
			1549,
			1553,
			1559,
			1567,
			1571,
			1579,
			1583,
			1597,
			1601,
			1607,
			1609,
			1613,
			1619,
			1621,
			1627,
			1637,
			1657,
			1663,
			1667,
			1669,
			1693,
			1697,
			1699,
			1709,
			1721,
			1723,
			1733,
			1741,
			1747,
			1753,
			1759,
			1777,
			1783,
			1787,
			1789,
			1801,
			1811,
			1823,
			1831,
			1847,
			1861,
			1867,
			1871,
			1873,
			1877,
			1879,
			1889,
			1901,
			1907,
			1913,
			1931,
			1933,
			1949,
			1951,
			1973,
			1979,
			1987,
			1993,
			1997,
			1999
		};

		private uint[] data = null;

		public int dataLength;

		public BigInteger()
		{
			this.data = new uint[70];
			this.dataLength = 1;
		}

		public BigInteger(long value)
		{
			this.data = new uint[70];
			long tempVal = value;
			this.dataLength = 0;
			while (value != 0 && this.dataLength < 70)
			{
				this.data[this.dataLength] = (uint)(value & 4294967295u);
				value >>= 32;
				this.dataLength++;
			}
			if (tempVal > 0)
			{
				if (value != 0 || ((int)this.data[69] & -2147483648) != 0)
				{
					throw new ArithmeticException("Positive overflow in constructor.");
				}
			}
			else if (tempVal < 0 && (value != -1 || ((int)this.data[this.dataLength - 1] & -2147483648) == 0))
			{
				throw new ArithmeticException("Negative underflow in constructor.");
			}
			if (this.dataLength == 0)
			{
				this.dataLength = 1;
			}
		}

		public BigInteger(ulong value)
		{
			this.data = new uint[70];
			this.dataLength = 0;
			while (value != 0 && this.dataLength < 70)
			{
				this.data[this.dataLength] = (uint)(value & 4294967295u);
				value >>= 32;
				this.dataLength++;
			}
			if (value != 0 || ((int)this.data[69] & -2147483648) != 0)
			{
				throw new ArithmeticException("Positive overflow in constructor.");
			}
			if (this.dataLength == 0)
			{
				this.dataLength = 1;
			}
		}

		public BigInteger(BigInteger bi)
		{
			this.data = new uint[70];
			this.dataLength = bi.dataLength;
			for (int i = 0; i < this.dataLength; i++)
			{
				this.data[i] = bi.data[i];
			}
		}

		public BigInteger(string value, int radix)
		{
			BigInteger multiplier = new BigInteger(1L);
			BigInteger result = new BigInteger();
			value = value.ToUpper().Trim();
			int limit = 0;
			if (value[0] == '-')
			{
				limit = 1;
			}
			for (int j = value.Length - 1; j >= limit; j--)
			{
				int posVal2 = value[j];
				posVal2 = ((posVal2 < 48 || posVal2 > 57) ? ((posVal2 < 65 || posVal2 > 90) ? 9999999 : (posVal2 - 65 + 10)) : (posVal2 - 48));
				if (posVal2 >= radix)
				{
					throw new ArithmeticException("Invalid string in constructor.");
				}
				if (value[0] == '-')
				{
					posVal2 = -posVal2;
				}
				result += multiplier * (BigInteger)posVal2;
				if (j - 1 >= limit)
				{
					multiplier *= (BigInteger)radix;
				}
			}
			if (value[0] == '-')
			{
				if (((int)result.data[69] & -2147483648) == 0)
				{
					throw new ArithmeticException("Negative underflow in constructor.");
				}
			}
			else if (((int)result.data[69] & -2147483648) != 0)
			{
				throw new ArithmeticException("Positive overflow in constructor.");
			}
			this.data = new uint[70];
			for (int j = 0; j < result.dataLength; j++)
			{
				this.data[j] = result.data[j];
			}
			this.dataLength = result.dataLength;
		}

		public BigInteger(byte[] inData)
		{
			this.dataLength = inData.Length >> 2;
			int leftOver = inData.Length & 3;
			if (leftOver != 0)
			{
				this.dataLength++;
			}
			if (this.dataLength > 70)
			{
				throw new ArithmeticException("Byte overflow in constructor.");
			}
			this.data = new uint[70];
			int j = inData.Length - 1;
			int i = 0;
			while (j >= 3)
			{
				this.data[i] = (uint)((inData[j - 3] << 24) + (inData[j - 2] << 16) + (inData[j - 1] << 8) + inData[j]);
				j -= 4;
				i++;
			}
			switch (leftOver)
			{
			case 1:
				this.data[this.dataLength - 1] = inData[0];
				break;
			case 2:
				this.data[this.dataLength - 1] = (uint)((inData[0] << 8) + inData[1]);
				break;
			case 3:
				this.data[this.dataLength - 1] = (uint)((inData[0] << 16) + (inData[1] << 8) + inData[2]);
				break;
			}
			while (this.dataLength > 1 && this.data[this.dataLength - 1] == 0)
			{
				this.dataLength--;
			}
		}

		public BigInteger(byte[] inData, int inLen)
		{
			this.dataLength = inLen >> 2;
			int leftOver = inLen & 3;
			if (leftOver != 0)
			{
				this.dataLength++;
			}
			if (this.dataLength > 70 || inLen > inData.Length)
			{
				throw new ArithmeticException("Byte overflow in constructor.");
			}
			this.data = new uint[70];
			int j = inLen - 1;
			int i = 0;
			while (j >= 3)
			{
				this.data[i] = (uint)((inData[j - 3] << 24) + (inData[j - 2] << 16) + (inData[j - 1] << 8) + inData[j]);
				j -= 4;
				i++;
			}
			switch (leftOver)
			{
			case 1:
				this.data[this.dataLength - 1] = inData[0];
				break;
			case 2:
				this.data[this.dataLength - 1] = (uint)((inData[0] << 8) + inData[1]);
				break;
			case 3:
				this.data[this.dataLength - 1] = (uint)((inData[0] << 16) + (inData[1] << 8) + inData[2]);
				break;
			}
			if (this.dataLength == 0)
			{
				this.dataLength = 1;
			}
			while (this.dataLength > 1 && this.data[this.dataLength - 1] == 0)
			{
				this.dataLength--;
			}
		}

		public BigInteger(uint[] inData)
		{
			this.dataLength = inData.Length;
			if (this.dataLength > 70)
			{
				throw new ArithmeticException("Byte overflow in constructor.");
			}
			this.data = new uint[70];
			int j = this.dataLength - 1;
			int i = 0;
			while (j >= 0)
			{
				this.data[i] = inData[j];
				j--;
				i++;
			}
			while (this.dataLength > 1 && this.data[this.dataLength - 1] == 0)
			{
				this.dataLength--;
			}
		}

		public static implicit operator BigInteger(long value)
		{
			return new BigInteger(value);
		}

		public static implicit operator BigInteger(ulong value)
		{
			return new BigInteger(value);
		}

		public static implicit operator BigInteger(int value)
		{
			return new BigInteger(value);
		}

		public static implicit operator BigInteger(uint value)
		{
			return new BigInteger((ulong)value);
		}

		public static BigInteger operator +(BigInteger bi1, BigInteger bi2)
		{
			BigInteger result = new BigInteger();
			result.dataLength = ((bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength);
			long carry = 0L;
			for (int i = 0; i < result.dataLength; i++)
			{
				long sum = (long)bi1.data[i] + (long)bi2.data[i] + carry;
				carry = sum >> 32;
				result.data[i] = (uint)(sum & 4294967295u);
			}
			if (carry != 0 && result.dataLength < 70)
			{
				result.data[result.dataLength] = (uint)carry;
				result.dataLength++;
			}
			while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
			{
				result.dataLength--;
			}
			int lastPos = 69;
			if (((int)bi1.data[lastPos] & -2147483648) == ((int)bi2.data[lastPos] & -2147483648) && ((int)result.data[lastPos] & -2147483648) != ((int)bi1.data[lastPos] & -2147483648))
			{
				throw new ArithmeticException();
			}
			return result;
		}

		public static BigInteger operator ++(BigInteger bi1)
		{
			BigInteger result = new BigInteger(bi1);
			long carry = 1L;
			int index = 0;
			while (carry != 0 && index < 70)
			{
				long val2 = result.data[index];
				val2++;
				result.data[index] = (uint)(val2 & 4294967295u);
				carry = val2 >> 32;
				index++;
			}
			if (index > result.dataLength)
			{
				result.dataLength = index;
			}
			else
			{
				while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
				{
					result.dataLength--;
				}
			}
			int lastPos = 69;
			if (((int)bi1.data[lastPos] & -2147483648) == 0 && ((int)result.data[lastPos] & -2147483648) != ((int)bi1.data[lastPos] & -2147483648))
			{
				throw new ArithmeticException("Overflow in ++.");
			}
			return result;
		}

		public static BigInteger operator -(BigInteger bi1, BigInteger bi2)
		{
			BigInteger result = new BigInteger();
			result.dataLength = ((bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength);
			long carryIn = 0L;
			for (int j = 0; j < result.dataLength; j++)
			{
				long diff = (long)bi1.data[j] - (long)bi2.data[j] - carryIn;
				result.data[j] = (uint)(diff & 4294967295u);
				carryIn = ((diff >= 0) ? 0 : 1);
			}
			if (carryIn != 0)
			{
				for (int j = result.dataLength; j < 70; j++)
				{
					result.data[j] = 4294967295u;
				}
				result.dataLength = 70;
			}
			while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
			{
				result.dataLength--;
			}
			int lastPos = 69;
			if (((int)bi1.data[lastPos] & -2147483648) != ((int)bi2.data[lastPos] & -2147483648) && ((int)result.data[lastPos] & -2147483648) != ((int)bi1.data[lastPos] & -2147483648))
			{
				throw new ArithmeticException();
			}
			return result;
		}

		public static BigInteger operator --(BigInteger bi1)
		{
			BigInteger result = new BigInteger(bi1);
			bool carryIn = true;
			int index = 0;
			while (carryIn && index < 70)
			{
				long val2 = result.data[index];
				val2--;
				result.data[index] = (uint)(val2 & 4294967295u);
				if (val2 >= 0)
				{
					carryIn = false;
				}
				index++;
			}
			if (index > result.dataLength)
			{
				result.dataLength = index;
			}
			while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
			{
				result.dataLength--;
			}
			int lastPos = 69;
			if (((int)bi1.data[lastPos] & -2147483648) != 0 && ((int)result.data[lastPos] & -2147483648) != ((int)bi1.data[lastPos] & -2147483648))
			{
				throw new ArithmeticException("Underflow in --.");
			}
			return result;
		}

		public static BigInteger operator *(BigInteger bi1, BigInteger bi2)
		{
			int lastPos = 69;
			bool bi1Neg = false;
			bool bi2Neg = false;
			try
			{
				if (((int)bi1.data[lastPos] & -2147483648) != 0)
				{
					bi1Neg = true;
					bi1 = -bi1;
				}
				if (((int)bi2.data[lastPos] & -2147483648) != 0)
				{
					bi2Neg = true;
					bi2 = -bi2;
				}
			}
			catch (Exception)
			{
			}
			BigInteger result = new BigInteger();
			try
			{
				for (int l = 0; l < bi1.dataLength; l++)
				{
					if (bi1.data[l] != 0)
					{
						ulong mcarry = 0uL;
						int k = 0;
						int j = l;
						while (k < bi2.dataLength)
						{
							ulong val = (ulong)((long)bi1.data[l] * (long)bi2.data[k] + result.data[j] + (long)mcarry);
							result.data[j] = (uint)(val & 4294967295u);
							mcarry = val >> 32;
							k++;
							j++;
						}
						if (mcarry != 0)
						{
							result.data[l + bi2.dataLength] = (uint)mcarry;
						}
					}
				}
			}
			catch (Exception)
			{
				throw new ArithmeticException("Multiplication overflow.");
			}
			result.dataLength = bi1.dataLength + bi2.dataLength;
			if (result.dataLength > 70)
			{
				result.dataLength = 70;
			}
			while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
			{
				result.dataLength--;
			}
			if (((int)result.data[lastPos] & -2147483648) != 0)
			{
				if (bi1Neg != bi2Neg && result.data[lastPos] == 2147483648u)
				{
					if (result.dataLength == 1)
					{
						return result;
					}
					bool isMaxNeg = true;
					for (int l = 0; l < result.dataLength - 1; l++)
					{
						if (!isMaxNeg)
						{
							break;
						}
						if (result.data[l] != 0)
						{
							isMaxNeg = false;
						}
					}
					if (isMaxNeg)
					{
						return result;
					}
				}
				throw new ArithmeticException("Multiplication overflow.");
			}
			if (bi1Neg != bi2Neg)
			{
				return -result;
			}
			return result;
		}

		public static BigInteger operator <<(BigInteger bi1, int shiftVal)
		{
			BigInteger result = new BigInteger(bi1);
			result.dataLength = BigInteger.shiftLeft(result.data, shiftVal);
			return result;
		}

		private static int shiftLeft(uint[] buffer, int shiftVal)
		{
			int shiftAmount = 32;
			int bufLen = buffer.Length;
			while (bufLen > 1 && buffer[bufLen - 1] == 0)
			{
				bufLen--;
			}
			for (int count = shiftVal; count > 0; count -= shiftAmount)
			{
				if (count < shiftAmount)
				{
					shiftAmount = count;
				}
				ulong carry = 0uL;
				for (int i = 0; i < bufLen; i++)
				{
					ulong val2 = (ulong)buffer[i] << shiftAmount;
					val2 |= carry;
					buffer[i] = (uint)(val2 & 4294967295u);
					carry = val2 >> 32;
				}
				if (carry != 0 && bufLen + 1 <= buffer.Length)
				{
					buffer[bufLen] = (uint)carry;
					bufLen++;
				}
			}
			return bufLen;
		}

		public static BigInteger operator >>(BigInteger bi1, int shiftVal)
		{
			BigInteger result = new BigInteger(bi1);
			result.dataLength = BigInteger.shiftRight(result.data, shiftVal);
			if (((int)bi1.data[69] & -2147483648) != 0)
			{
				for (int j = 69; j >= result.dataLength; j--)
				{
					result.data[j] = 4294967295u;
				}
				uint mask = 2147483648u;
				for (int j = 0; j < 32; j++)
				{
					if ((result.data[result.dataLength - 1] & mask) != 0)
					{
						break;
					}
					result.data[result.dataLength - 1] |= mask;
					mask >>= 1;
				}
				result.dataLength = 70;
			}
			return result;
		}

		private static int shiftRight(uint[] buffer, int shiftVal)
		{
			int shiftAmount = 32;
			int invShift = 0;
			int bufLen = buffer.Length;
			while (bufLen > 1 && buffer[bufLen - 1] == 0)
			{
				bufLen--;
			}
			for (int count = shiftVal; count > 0; count -= shiftAmount)
			{
				if (count < shiftAmount)
				{
					shiftAmount = count;
					invShift = 32 - shiftAmount;
				}
				ulong carry = 0uL;
				for (int i = bufLen - 1; i >= 0; i--)
				{
					ulong val2 = (ulong)buffer[i] >> shiftAmount;
					val2 |= carry;
					carry = (ulong)buffer[i] << invShift;
					buffer[i] = (uint)val2;
				}
			}
			while (bufLen > 1 && buffer[bufLen - 1] == 0)
			{
				bufLen--;
			}
			return bufLen;
		}

		public static BigInteger operator ~(BigInteger bi1)
		{
			BigInteger result = new BigInteger(bi1);
			for (int i = 0; i < 70; i++)
			{
				result.data[i] = ~bi1.data[i];
			}
			result.dataLength = 70;
			while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
			{
				result.dataLength--;
			}
			return result;
		}

		public static BigInteger operator -(BigInteger bi1)
		{
			if (bi1.dataLength == 1 && bi1.data[0] == 0)
			{
				return new BigInteger();
			}
			BigInteger result = new BigInteger(bi1);
			for (int i = 0; i < 70; i++)
			{
				result.data[i] = ~bi1.data[i];
			}
			long carry = 1L;
			int index = 0;
			while (carry != 0 && index < 70)
			{
				long val2 = result.data[index];
				val2++;
				result.data[index] = (uint)(val2 & 4294967295u);
				carry = val2 >> 32;
				index++;
			}
			if (((int)bi1.data[69] & -2147483648) == ((int)result.data[69] & -2147483648))
			{
				throw new ArithmeticException("Overflow in negation.\n");
			}
			result.dataLength = 70;
			while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
			{
				result.dataLength--;
			}
			return result;
		}

		public static bool operator ==(BigInteger bi1, BigInteger bi2)
		{
			return bi1.Equals(bi2);
		}

		public static bool operator !=(BigInteger bi1, BigInteger bi2)
		{
			return !bi1.Equals(bi2);
		}

		public override bool Equals(object o)
		{
			BigInteger bi = (BigInteger)o;
			if (this.dataLength != bi.dataLength)
			{
				return false;
			}
			for (int i = 0; i < this.dataLength; i++)
			{
				if (this.data[i] != bi.data[i])
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			return this.ToString().GetHashCode();
		}

		public static bool operator >(BigInteger bi1, BigInteger bi2)
		{
			int pos = 69;
			if (((int)bi1.data[pos] & -2147483648) != 0 && ((int)bi2.data[pos] & -2147483648) == 0)
			{
				return false;
			}
			if (((int)bi1.data[pos] & -2147483648) == 0 && ((int)bi2.data[pos] & -2147483648) != 0)
			{
				return true;
			}
			int len = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;
			pos = len - 1;
			while (pos >= 0 && bi1.data[pos] == bi2.data[pos])
			{
				pos--;
			}
			if (pos >= 0)
			{
				if (bi1.data[pos] > bi2.data[pos])
				{
					return true;
				}
				return false;
			}
			return false;
		}

		public static bool operator <(BigInteger bi1, BigInteger bi2)
		{
			int pos = 69;
			if (((int)bi1.data[pos] & -2147483648) != 0 && ((int)bi2.data[pos] & -2147483648) == 0)
			{
				return true;
			}
			if (((int)bi1.data[pos] & -2147483648) == 0 && ((int)bi2.data[pos] & -2147483648) != 0)
			{
				return false;
			}
			int len = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;
			pos = len - 1;
			while (pos >= 0 && bi1.data[pos] == bi2.data[pos])
			{
				pos--;
			}
			if (pos >= 0)
			{
				if (bi1.data[pos] < bi2.data[pos])
				{
					return true;
				}
				return false;
			}
			return false;
		}

		public static bool operator >=(BigInteger bi1, BigInteger bi2)
		{
			return bi1 == bi2 || bi1 > bi2;
		}

		public static bool operator <=(BigInteger bi1, BigInteger bi2)
		{
			return bi1 == bi2 || bi1 < bi2;
		}

		private static void multiByteDivide(BigInteger bi1, BigInteger bi2, BigInteger outQuotient, BigInteger outRemainder)
		{
			uint[] result = new uint[70];
			int remainderLen = bi1.dataLength + 1;
			uint[] remainder = new uint[remainderLen];
			uint mask = 2147483648u;
			uint val = bi2.data[bi2.dataLength - 1];
			int shift = 0;
			int resultPos = 0;
			while (mask != 0 && (val & mask) == 0)
			{
				shift++;
				mask >>= 1;
			}
			for (int j = 0; j < bi1.dataLength; j++)
			{
				remainder[j] = bi1.data[j];
			}
			BigInteger.shiftLeft(remainder, shift);
			bi2 <<= shift;
			int i = remainderLen - bi2.dataLength;
			int pos = remainderLen - 1;
			ulong firstDivisorByte = bi2.data[bi2.dataLength - 1];
			ulong secondDivisorByte = bi2.data[bi2.dataLength - 2];
			int divisorLen = bi2.dataLength + 1;
			uint[] dividendPart = new uint[divisorLen];
			while (i > 0)
			{
				ulong dividend = ((ulong)remainder[pos] << 32) + remainder[pos - 1];
				ulong q_hat = dividend / firstDivisorByte;
				ulong r_hat = dividend % firstDivisorByte;
				bool done = false;
				while (!done)
				{
					done = true;
					if (q_hat == 4294967296L || q_hat * secondDivisorByte > (r_hat << 32) + remainder[pos - 2])
					{
						q_hat--;
						r_hat += firstDivisorByte;
						if (r_hat < 4294967296L)
						{
							done = false;
						}
					}
				}
				for (int h2 = 0; h2 < divisorLen; h2++)
				{
					dividendPart[h2] = remainder[pos - h2];
				}
				BigInteger kk = new BigInteger(dividendPart);
				BigInteger ss = bi2 * (BigInteger)(long)q_hat;
				while (ss > kk)
				{
					q_hat--;
					ss -= bi2;
				}
				BigInteger yy = kk - ss;
				for (int h2 = 0; h2 < divisorLen; h2++)
				{
					remainder[pos - h2] = yy.data[bi2.dataLength - h2];
				}
				result[resultPos++] = (uint)q_hat;
				pos--;
				i--;
			}
			outQuotient.dataLength = resultPos;
			int y2 = 0;
			int x = outQuotient.dataLength - 1;
			while (x >= 0)
			{
				outQuotient.data[y2] = result[x];
				x--;
				y2++;
			}
			for (; y2 < 70; y2++)
			{
				outQuotient.data[y2] = 0u;
			}
			while (outQuotient.dataLength > 1 && outQuotient.data[outQuotient.dataLength - 1] == 0)
			{
				outQuotient.dataLength--;
			}
			if (outQuotient.dataLength == 0)
			{
				outQuotient.dataLength = 1;
			}
			outRemainder.dataLength = BigInteger.shiftRight(remainder, shift);
			for (y2 = 0; y2 < outRemainder.dataLength; y2++)
			{
				outRemainder.data[y2] = remainder[y2];
			}
			for (; y2 < 70; y2++)
			{
				outRemainder.data[y2] = 0u;
			}
		}

		private static void singleByteDivide(BigInteger bi1, BigInteger bi2, BigInteger outQuotient, BigInteger outRemainder)
		{
			uint[] result = new uint[70];
			int resultPos = 0;
			int k;
			for (k = 0; k < 70; k++)
			{
				outRemainder.data[k] = bi1.data[k];
			}
			outRemainder.dataLength = bi1.dataLength;
			while (outRemainder.dataLength > 1 && outRemainder.data[outRemainder.dataLength - 1] == 0)
			{
				outRemainder.dataLength--;
			}
			ulong divisor = bi2.data[0];
			int pos2 = outRemainder.dataLength - 1;
			ulong dividend2 = outRemainder.data[pos2];
			if (dividend2 >= divisor)
			{
				ulong quotient2 = dividend2 / divisor;
				result[resultPos++] = (uint)quotient2;
				outRemainder.data[pos2] = (uint)(dividend2 % divisor);
			}
			pos2--;
			while (pos2 >= 0)
			{
				dividend2 = ((ulong)outRemainder.data[pos2 + 1] << 32) + outRemainder.data[pos2];
				ulong quotient2 = dividend2 / divisor;
				result[resultPos++] = (uint)quotient2;
				outRemainder.data[pos2 + 1] = 0u;
				outRemainder.data[pos2--] = (uint)(dividend2 % divisor);
			}
			outQuotient.dataLength = resultPos;
			int j = 0;
			k = outQuotient.dataLength - 1;
			while (k >= 0)
			{
				outQuotient.data[j] = result[k];
				k--;
				j++;
			}
			for (; j < 70; j++)
			{
				outQuotient.data[j] = 0u;
			}
			while (outQuotient.dataLength > 1 && outQuotient.data[outQuotient.dataLength - 1] == 0)
			{
				outQuotient.dataLength--;
			}
			if (outQuotient.dataLength == 0)
			{
				outQuotient.dataLength = 1;
			}
			while (outRemainder.dataLength > 1 && outRemainder.data[outRemainder.dataLength - 1] == 0)
			{
				outRemainder.dataLength--;
			}
		}

		public static BigInteger operator /(BigInteger bi1, BigInteger bi2)
		{
			BigInteger quotient = new BigInteger();
			BigInteger remainder = new BigInteger();
			int lastPos = 69;
			bool divisorNeg = false;
			bool dividendNeg = false;
			if (((int)bi1.data[lastPos] & -2147483648) != 0)
			{
				bi1 = -bi1;
				dividendNeg = true;
			}
			if (((int)bi2.data[lastPos] & -2147483648) != 0)
			{
				bi2 = -bi2;
				divisorNeg = true;
			}
			if (bi1 < bi2)
			{
				return quotient;
			}
			if (bi2.dataLength == 1)
			{
				BigInteger.singleByteDivide(bi1, bi2, quotient, remainder);
			}
			else
			{
				BigInteger.multiByteDivide(bi1, bi2, quotient, remainder);
			}
			if (dividendNeg != divisorNeg)
			{
				return -quotient;
			}
			return quotient;
		}

		public static BigInteger operator %(BigInteger bi1, BigInteger bi2)
		{
			BigInteger quotient = new BigInteger();
			BigInteger remainder = new BigInteger(bi1);
			int lastPos = 69;
			bool dividendNeg = false;
			if (((int)bi1.data[lastPos] & -2147483648) != 0)
			{
				bi1 = -bi1;
				dividendNeg = true;
			}
			if (((int)bi2.data[lastPos] & -2147483648) != 0)
			{
				bi2 = -bi2;
			}
			if (bi1 < bi2)
			{
				return remainder;
			}
			if (bi2.dataLength == 1)
			{
				BigInteger.singleByteDivide(bi1, bi2, quotient, remainder);
			}
			else
			{
				BigInteger.multiByteDivide(bi1, bi2, quotient, remainder);
			}
			if (dividendNeg)
			{
				return -remainder;
			}
			return remainder;
		}

		public static BigInteger operator &(BigInteger bi1, BigInteger bi2)
		{
			BigInteger result = new BigInteger();
			int len = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;
			for (int i = 0; i < len; i++)
			{
				uint sum = bi1.data[i] & bi2.data[i];
				result.data[i] = sum;
			}
			result.dataLength = 70;
			while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
			{
				result.dataLength--;
			}
			return result;
		}

		public static BigInteger operator |(BigInteger bi1, BigInteger bi2)
		{
			BigInteger result = new BigInteger();
			int len = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;
			for (int i = 0; i < len; i++)
			{
				uint sum = bi1.data[i] | bi2.data[i];
				result.data[i] = sum;
			}
			result.dataLength = 70;
			while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
			{
				result.dataLength--;
			}
			return result;
		}

		public static BigInteger operator ^(BigInteger bi1, BigInteger bi2)
		{
			BigInteger result = new BigInteger();
			int len = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;
			for (int i = 0; i < len; i++)
			{
				uint sum = bi1.data[i] ^ bi2.data[i];
				result.data[i] = sum;
			}
			result.dataLength = 70;
			while (result.dataLength > 1 && result.data[result.dataLength - 1] == 0)
			{
				result.dataLength--;
			}
			return result;
		}

		public BigInteger max(BigInteger bi)
		{
			if (this > bi)
			{
				return new BigInteger(this);
			}
			return new BigInteger(bi);
		}

		public BigInteger min(BigInteger bi)
		{
			if (this < bi)
			{
				return new BigInteger(this);
			}
			return new BigInteger(bi);
		}

		public BigInteger abs()
		{
			if (((int)this.data[69] & -2147483648) != 0)
			{
				return -this;
			}
			return new BigInteger(this);
		}

		public override string ToString()
		{
			return this.ToString(10);
		}

		public string ToString(int radix)
		{
			if (radix < 2 || radix > 36)
			{
				throw new ArgumentException("Radix must be >= 2 and <= 36");
			}
			string charSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			string result = "";
			BigInteger a = this;
			bool negative = false;
			if (((int)a.data[69] & -2147483648) != 0)
			{
				negative = true;
				try
				{
					a = -a;
				}
				catch (Exception)
				{
				}
			}
			BigInteger quotient = new BigInteger();
			BigInteger remainder = new BigInteger();
			BigInteger biRadix = new BigInteger(radix);
			if (a.dataLength == 1 && a.data[0] == 0)
			{
				result = "0";
			}
			else
			{
				while (a.dataLength > 1 || (a.dataLength == 1 && a.data[0] != 0))
				{
					BigInteger.singleByteDivide(a, biRadix, quotient, remainder);
					result = ((remainder.data[0] >= 10) ? (charSet[(int)(remainder.data[0] - 10)] + result) : (remainder.data[0] + result));
					a = quotient;
				}
				if (negative)
				{
					result = "-" + result;
				}
			}
			return result;
		}

		public string ToHexString()
		{
			string result = this.data[this.dataLength - 1].ToString("X");
			for (int i = this.dataLength - 2; i >= 0; i--)
			{
				result += this.data[i].ToString("X8");
			}
			return result;
		}

		public BigInteger ModPow(BigInteger exp, BigInteger n)
		{
			if (((int)exp.data[69] & -2147483648) != 0)
			{
				throw new ArithmeticException("Positive exponents only.");
			}
			BigInteger resultNum = 1;
			bool thisNegative = false;
			BigInteger tempNum;
			if (((int)this.data[69] & -2147483648) != 0)
			{
				tempNum = -this % n;
				thisNegative = true;
			}
			else
			{
				tempNum = this % n;
			}
			if (((int)n.data[69] & -2147483648) != 0)
			{
				n = -n;
			}
			BigInteger constant = new BigInteger();
			int i = n.dataLength << 1;
			constant.data[i] = 1u;
			constant.dataLength = i + 1;
			constant /= n;
			int totalBits = exp.bitCount();
			int count = 0;
			for (int pos = 0; pos < exp.dataLength; pos++)
			{
				uint mask = 1u;
				for (int index = 0; index < 32; index++)
				{
					if ((exp.data[pos] & mask) != 0)
					{
						resultNum = this.BarrettReduction(resultNum * tempNum, n, constant);
					}
					mask <<= 1;
					tempNum = this.BarrettReduction(tempNum * tempNum, n, constant);
					if (tempNum.dataLength == 1 && tempNum.data[0] == 1)
					{
						if (thisNegative && (exp.data[0] & 1) != 0)
						{
							return -resultNum;
						}
						return resultNum;
					}
					count++;
					if (count == totalBits)
					{
						break;
					}
				}
			}
			if (thisNegative && (exp.data[0] & 1) != 0)
			{
				return -resultNum;
			}
			return resultNum;
		}

		private BigInteger BarrettReduction(BigInteger x, BigInteger n, BigInteger constant)
		{
			int k2 = n.dataLength;
			int kPlusOne = k2 + 1;
			int kMinusOne = k2 - 1;
			BigInteger q5 = new BigInteger();
			int i2 = kMinusOne;
			int j2 = 0;
			while (i2 < x.dataLength)
			{
				q5.data[j2] = x.data[i2];
				i2++;
				j2++;
			}
			q5.dataLength = x.dataLength - kMinusOne;
			if (q5.dataLength <= 0)
			{
				q5.dataLength = 1;
			}
			BigInteger q4 = q5 * constant;
			BigInteger q3 = new BigInteger();
			i2 = kPlusOne;
			j2 = 0;
			while (i2 < q4.dataLength)
			{
				q3.data[j2] = q4.data[i2];
				i2++;
				j2++;
			}
			q3.dataLength = q4.dataLength - kPlusOne;
			if (q3.dataLength <= 0)
			{
				q3.dataLength = 1;
			}
			BigInteger r3 = new BigInteger();
			int lengthToCopy = (x.dataLength > kPlusOne) ? kPlusOne : x.dataLength;
			for (i2 = 0; i2 < lengthToCopy; i2++)
			{
				r3.data[i2] = x.data[i2];
			}
			r3.dataLength = lengthToCopy;
			BigInteger r2 = new BigInteger();
			for (i2 = 0; i2 < q3.dataLength; i2++)
			{
				if (q3.data[i2] != 0)
				{
					ulong mcarry = 0uL;
					int t = i2;
					j2 = 0;
					while (j2 < n.dataLength && t < kPlusOne)
					{
						ulong val = (ulong)((long)q3.data[i2] * (long)n.data[j2] + r2.data[t] + (long)mcarry);
						r2.data[t] = (uint)(val & 4294967295u);
						mcarry = val >> 32;
						j2++;
						t++;
					}
					if (t < kPlusOne)
					{
						r2.data[t] = (uint)mcarry;
					}
				}
			}
			r2.dataLength = kPlusOne;
			while (r2.dataLength > 1 && r2.data[r2.dataLength - 1] == 0)
			{
				r2.dataLength--;
			}
			r3 -= r2;
			if (((int)r3.data[69] & -2147483648) != 0)
			{
				BigInteger val2 = new BigInteger();
				val2.data[kPlusOne] = 1u;
				val2.dataLength = kPlusOne + 1;
				r3 += val2;
			}
			while (r3 >= n)
			{
				r3 -= n;
			}
			return r3;
		}

		public BigInteger gcd(BigInteger bi)
		{
			BigInteger x = (((int)this.data[69] & -2147483648) == 0) ? this : (-this);
			BigInteger y = (((int)bi.data[69] & -2147483648) == 0) ? bi : (-bi);
			BigInteger g = y;
			while (x.dataLength > 1 || (x.dataLength == 1 && x.data[0] != 0))
			{
				g = x;
				x = y % x;
				y = g;
			}
			return g;
		}

		public static BigInteger GenerateRandom(int bits)
		{
			BigInteger bi = new BigInteger();
			bi.genRandomBits(bits, new Random());
			return bi;
		}

		public void genRandomBits(int bits, Random rand)
		{
			int dwords = bits >> 5;
			int remBits = bits & 0x1F;
			if (remBits != 0)
			{
				dwords++;
			}
			if (dwords > 70)
			{
				throw new ArithmeticException("Number of required bits > maxLength.");
			}
			for (int j = 0; j < dwords; j++)
			{
				this.data[j] = (uint)(rand.NextDouble() * 4294967296.0);
			}
			for (int j = dwords; j < 70; j++)
			{
				this.data[j] = 0u;
			}
			if (remBits != 0)
			{
				uint mask2 = (uint)(1 << remBits - 1);
				this.data[dwords - 1] |= mask2;
				mask2 = 4294967295u >> 32 - remBits;
				this.data[dwords - 1] &= mask2;
			}
			else
			{
				this.data[dwords - 1] |= 2147483648u;
			}
			this.dataLength = dwords;
			if (this.dataLength == 0)
			{
				this.dataLength = 1;
			}
		}

		public int bitCount()
		{
			while (this.dataLength > 1 && this.data[this.dataLength - 1] == 0)
			{
				this.dataLength--;
			}
			uint value = this.data[this.dataLength - 1];
			uint mask = 2147483648u;
			int bits = 32;
			while (bits > 0 && (value & mask) == 0)
			{
				bits--;
				mask >>= 1;
			}
			return bits + (this.dataLength - 1 << 5);
		}

		public bool FermatLittleTest(int confidence)
		{
			BigInteger thisVal = (((int)this.data[69] & -2147483648) == 0) ? this : (-this);
			if (thisVal.dataLength == 1)
			{
				if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
				{
					return false;
				}
				if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
				{
					return true;
				}
			}
			if ((thisVal.data[0] & 1) == 0)
			{
				return false;
			}
			int bits = thisVal.bitCount();
			BigInteger a = new BigInteger();
			BigInteger p_sub = thisVal - new BigInteger(1L);
			Random rand = new Random();
			int round = 0;
			bool result;
			while (true)
			{
				if (round < confidence)
				{
					bool done = false;
					while (!done)
					{
						int testBits;
						for (testBits = 0; testBits < 2; testBits = (int)(rand.NextDouble() * (double)bits))
						{
						}
						a.genRandomBits(testBits, rand);
						int byteLen = a.dataLength;
						if (byteLen > 1 || (byteLen == 1 && a.data[0] != 1))
						{
							done = true;
						}
					}
					BigInteger gcdTest = a.gcd(thisVal);
					if (gcdTest.dataLength == 1 && gcdTest.data[0] != 1)
					{
						result = false;
						break;
					}
					BigInteger expResult = a.ModPow(p_sub, thisVal);
					int resultLen = expResult.dataLength;
					if (resultLen > 1 || (resultLen == 1 && expResult.data[0] != 1))
					{
						return false;
					}
					round++;
					continue;
				}
				return true;
			}
			return result;
		}

		public bool RabinMillerTest(int confidence)
		{
			BigInteger thisVal = (((int)this.data[69] & -2147483648) == 0) ? this : (-this);
			if (thisVal.dataLength == 1)
			{
				if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
				{
					return false;
				}
				if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
				{
					return true;
				}
			}
			if ((thisVal.data[0] & 1) == 0)
			{
				return false;
			}
			BigInteger p_sub = thisVal - new BigInteger(1L);
			int s = 0;
			for (int index = 0; index < p_sub.dataLength; index++)
			{
				uint mask = 1u;
				int i = 0;
				while (i < 32)
				{
					if ((p_sub.data[index] & mask) == 0)
					{
						mask <<= 1;
						s++;
						i++;
						continue;
					}
					index = p_sub.dataLength;
					break;
				}
			}
			BigInteger t = p_sub >> s;
			int bits = thisVal.bitCount();
			BigInteger a = new BigInteger();
			Random rand = new Random();
			int round = 0;
			bool result2;
			while (true)
			{
				if (round < confidence)
				{
					bool done = false;
					while (!done)
					{
						int testBits;
						for (testBits = 0; testBits < 2; testBits = (int)(rand.NextDouble() * (double)bits))
						{
						}
						a.genRandomBits(testBits, rand);
						int byteLen = a.dataLength;
						if (byteLen > 1 || (byteLen == 1 && a.data[0] != 1))
						{
							done = true;
						}
					}
					BigInteger gcdTest = a.gcd(thisVal);
					if (gcdTest.dataLength == 1 && gcdTest.data[0] != 1)
					{
						result2 = false;
						break;
					}
					BigInteger b = a.ModPow(t, thisVal);
					bool result = false;
					if (b.dataLength == 1 && b.data[0] == 1)
					{
						result = true;
					}
					int j = 0;
					while (!result && j < s)
					{
						if (!(b == p_sub))
						{
							b = b * b % thisVal;
							j++;
							continue;
						}
						result = true;
						break;
					}
					if (!result)
					{
						return false;
					}
					round++;
					continue;
				}
				return true;
			}
			return result2;
		}

		public bool SolovayStrassenTest(int confidence)
		{
			BigInteger thisVal = (((int)this.data[69] & -2147483648) == 0) ? this : (-this);
			if (thisVal.dataLength == 1)
			{
				if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
				{
					return false;
				}
				if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
				{
					return true;
				}
			}
			if ((thisVal.data[0] & 1) == 0)
			{
				return false;
			}
			int bits = thisVal.bitCount();
			BigInteger a = new BigInteger();
			BigInteger p_sub = thisVal - (BigInteger)1;
			BigInteger p_sub1_shift = p_sub >> 1;
			Random rand = new Random();
			int round = 0;
			bool result;
			while (true)
			{
				if (round < confidence)
				{
					bool done = false;
					while (!done)
					{
						int testBits;
						for (testBits = 0; testBits < 2; testBits = (int)(rand.NextDouble() * (double)bits))
						{
						}
						a.genRandomBits(testBits, rand);
						int byteLen = a.dataLength;
						if (byteLen > 1 || (byteLen == 1 && a.data[0] != 1))
						{
							done = true;
						}
					}
					BigInteger gcdTest = a.gcd(thisVal);
					if (gcdTest.dataLength == 1 && gcdTest.data[0] != 1)
					{
						result = false;
						break;
					}
					BigInteger expResult = a.ModPow(p_sub1_shift, thisVal);
					if (expResult == p_sub)
					{
						expResult = -1;
					}
					BigInteger jacob = BigInteger.Jacobi(a, thisVal);
					if (expResult != jacob)
					{
						return false;
					}
					round++;
					continue;
				}
				return true;
			}
			return result;
		}

		public bool LucasStrongTest()
		{
			BigInteger thisVal = (((int)this.data[69] & -2147483648) == 0) ? this : (-this);
			if (thisVal.dataLength == 1)
			{
				if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
				{
					return false;
				}
				if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
				{
					return true;
				}
			}
			if ((thisVal.data[0] & 1) == 0)
			{
				return false;
			}
			return this.LucasStrongTestHelper(thisVal);
		}

		private bool LucasStrongTestHelper(BigInteger thisVal)
		{
			long D = 5L;
			long sign = -1L;
			long dCount = 0L;
			bool done = false;
			while (!done)
			{
				int num;
				switch (BigInteger.Jacobi(D, thisVal))
				{
				case -1:
					done = true;
					break;
				case 0:
					num = ((!((BigInteger)Math.Abs(D) < thisVal)) ? 1 : 0);
					goto IL_004e;
				default:
					{
						num = 1;
						goto IL_004e;
					}
					IL_004e:
					if (num == 0)
					{
						return false;
					}
					if (dCount == 20)
					{
						BigInteger root = thisVal.sqrt();
						if (root * root == thisVal)
						{
							return false;
						}
					}
					D = (Math.Abs(D) + 2) * sign;
					sign = -sign;
					break;
				}
				dCount++;
			}
			long Q = 1 - D >> 2;
			BigInteger p_add = thisVal + (BigInteger)1;
			int s = 0;
			for (int index = 0; index < p_add.dataLength; index++)
			{
				uint mask = 1u;
				int i = 0;
				while (i < 32)
				{
					if ((p_add.data[index] & mask) == 0)
					{
						mask <<= 1;
						s++;
						i++;
						continue;
					}
					index = p_add.dataLength;
					break;
				}
			}
			BigInteger t = p_add >> s;
			BigInteger constant2 = new BigInteger();
			int nLen = thisVal.dataLength << 1;
			constant2.data[nLen] = 1u;
			constant2.dataLength = nLen + 1;
			constant2 /= thisVal;
			BigInteger[] lucas = BigInteger.LucasSequenceHelper(1, Q, t, thisVal, constant2, 0);
			bool isPrime = false;
			if ((lucas[0].dataLength == 1 && lucas[0].data[0] == 0) || (lucas[1].dataLength == 1 && lucas[1].data[0] == 0))
			{
				isPrime = true;
			}
			for (int i = 1; i < s; i++)
			{
				if (!isPrime)
				{
					lucas[1] = thisVal.BarrettReduction(lucas[1] * lucas[1], thisVal, constant2);
					lucas[1] = (lucas[1] - (lucas[2] << 1)) % thisVal;
					if (lucas[1].dataLength == 1 && lucas[1].data[0] == 0)
					{
						isPrime = true;
					}
				}
				lucas[2] = thisVal.BarrettReduction(lucas[2] * lucas[2], thisVal, constant2);
			}
			if (isPrime)
			{
				BigInteger g = thisVal.gcd(Q);
				if (g.dataLength == 1 && g.data[0] == 1)
				{
					if (((int)lucas[2].data[69] & -2147483648) != 0)
					{
						BigInteger[] array = lucas;
						//(array = lucas)[2] = array[2] + thisVal;//(array = lucas)[2] = array[2] + thisVal;
                        (array)[2] = array[2] + thisVal;
                    }
					BigInteger temp = (BigInteger)(Q * BigInteger.Jacobi(Q, thisVal)) % thisVal;
					if (((int)temp.data[69] & -2147483648) != 0)
					{
						temp += thisVal;
					}
					if (lucas[2] != temp)
					{
						isPrime = false;
					}
				}
			}
			return isPrime;
		}

		public bool isProbablePrime(int confidence)
		{
			BigInteger thisVal = (((int)this.data[69] & -2147483648) == 0) ? this : (-this);
			int p = 0;
			bool result;
			while (true)
			{
				if (p < BigInteger.primesBelow2000.Length)
				{
					BigInteger divisor = BigInteger.primesBelow2000[p];
					if (!(divisor >= thisVal))
					{
						BigInteger resultNum = thisVal % divisor;
						if (resultNum.IntValue() == 0)
						{
							result = false;
							break;
						}
						p++;
						continue;
					}
				}
				if (thisVal.RabinMillerTest(confidence))
				{
					return true;
				}
				return false;
			}
			return result;
		}

		public bool isProbablePrime()
		{
			BigInteger thisVal = (((int)this.data[69] & -2147483648) == 0) ? this : (-this);
			if (thisVal.dataLength == 1)
			{
				if (thisVal.data[0] == 0 || thisVal.data[0] == 1)
				{
					return false;
				}
				if (thisVal.data[0] == 2 || thisVal.data[0] == 3)
				{
					return true;
				}
			}
			if ((thisVal.data[0] & 1) == 0)
			{
				return false;
			}
			int p = 0;
			bool result2;
			while (true)
			{
				if (p < BigInteger.primesBelow2000.Length)
				{
					BigInteger divisor = BigInteger.primesBelow2000[p];
					if (!(divisor >= thisVal))
					{
						BigInteger resultNum = thisVal % divisor;
						if (resultNum.IntValue() == 0)
						{
							result2 = false;
							break;
						}
						p++;
						continue;
					}
				}
				BigInteger p_sub = thisVal - new BigInteger(1L);
				int s = 0;
				for (int index = 0; index < p_sub.dataLength; index++)
				{
					uint mask = 1u;
					int i = 0;
					while (i < 32)
					{
						if ((p_sub.data[index] & mask) == 0)
						{
							mask <<= 1;
							s++;
							i++;
							continue;
						}
						index = p_sub.dataLength;
						break;
					}
				}
				BigInteger t = p_sub >> s;
				int bits = thisVal.bitCount();
				BigInteger a = 2;
				BigInteger b = a.ModPow(t, thisVal);
				bool result = false;
				if (b.dataLength == 1 && b.data[0] == 1)
				{
					result = true;
				}
				int j = 0;
				while (!result && j < s)
				{
					if (!(b == p_sub))
					{
						b = b * b % thisVal;
						j++;
						continue;
					}
					result = true;
					break;
				}
				if (result)
				{
					result = this.LucasStrongTestHelper(thisVal);
				}
				return result;
			}
			return result2;
		}

		public int IntValue()
		{
			return (int)this.data[0];
		}

		public long LongValue()
		{
			long val2 = 0L;
			val2 = this.data[0];
			try
			{
				val2 |= (long)((ulong)this.data[1] << 32);
			}
			catch (Exception)
			{
				if (((int)this.data[0] & -2147483648) != 0)
				{
					val2 = (int)this.data[0];
				}
			}
			return val2;
		}

		public static int Jacobi(BigInteger a, BigInteger b)
		{
			if ((b.data[0] & 1) == 0)
			{
				throw new ArgumentException("Jacobi defined only for odd integers.");
			}
			if (a >= b)
			{
				a %= b;
			}
			if (a.dataLength == 1 && a.data[0] == 0)
			{
				return 0;
			}
			if (a.dataLength == 1 && a.data[0] == 1)
			{
				return 1;
			}
			if (a < (BigInteger)0)
			{
				if (((b - (BigInteger)1).data[0] & 2) == 0)
				{
					return BigInteger.Jacobi(-a, b);
				}
				return -BigInteger.Jacobi(-a, b);
			}
			int e = 0;
			for (int index = 0; index < a.dataLength; index++)
			{
				uint mask = 1u;
				int i = 0;
				while (i < 32)
				{
					if ((a.data[index] & mask) == 0)
					{
						mask <<= 1;
						e++;
						i++;
						continue;
					}
					index = a.dataLength;
					break;
				}
			}
			BigInteger a2 = a >> e;
			int s = 1;
			if ((e & 1) != 0 && ((b.data[0] & 7) == 3 || (b.data[0] & 7) == 5))
			{
				s = -1;
			}
			if ((b.data[0] & 3) == 3 && (a2.data[0] & 3) == 3)
			{
				s = -s;
			}
			if (a2.dataLength == 1 && a2.data[0] == 1)
			{
				return s;
			}
			return s * BigInteger.Jacobi(b % a2, a2);
		}

		public static BigInteger genPseudoPrime(int bits, int confidence, Random rand)
		{
			BigInteger result = new BigInteger();
			bool done = false;
			while (!done)
			{
				result.genRandomBits(bits, rand);
				result.data[0] |= 1u;
				done = result.isProbablePrime(confidence);
			}
			return result;
		}

		public BigInteger genCoPrime(int bits, Random rand)
		{
			bool done = false;
			BigInteger result = new BigInteger();
			while (!done)
			{
				result.genRandomBits(bits, rand);
				BigInteger g = result.gcd(this);
				if (g.dataLength == 1 && g.data[0] == 1)
				{
					done = true;
				}
			}
			return result;
		}

		public BigInteger modInverse(BigInteger modulus)
		{
			BigInteger[] p = new BigInteger[2]
			{
				0,
				1
			};
			BigInteger[] q = new BigInteger[2];
			BigInteger[] r = new BigInteger[2]
			{
				0,
				0
			};
			int step = 0;
			BigInteger a = modulus;
			BigInteger b = this;
			while (b.dataLength > 1 || (b.dataLength == 1 && b.data[0] != 0))
			{
				BigInteger quotient = new BigInteger();
				BigInteger remainder = new BigInteger();
				if (step > 1)
				{
					BigInteger pval = (p[0] - p[1] * q[0]) % modulus;
					p[0] = p[1];
					p[1] = pval;
				}
				if (b.dataLength == 1)
				{
					BigInteger.singleByteDivide(a, b, quotient, remainder);
				}
				else
				{
					BigInteger.multiByteDivide(a, b, quotient, remainder);
				}
				q[0] = q[1];
				r[0] = r[1];
				q[1] = quotient;
				r[1] = remainder;
				a = b;
				b = remainder;
				step++;
			}
			if (r[0].dataLength > 1 || (r[0].dataLength == 1 && r[0].data[0] != 1))
			{
				throw new ArithmeticException("No inverse!");
			}
			BigInteger result = (p[0] - p[1] * q[0]) % modulus;
			if (((int)result.data[69] & -2147483648) != 0)
			{
				result += modulus;
			}
			return result;
		}

		public byte[] GetBytes()
		{
			if (this == (BigInteger)0)
			{
				return new byte[1];
			}
			int numBits = this.bitCount();
			int numBytes = numBits >> 3;
			if ((numBits & 7) != 0)
			{
				numBytes++;
			}
			byte[] result = new byte[numBytes];
			int numBytesInWord = numBytes & 3;
			if (numBytesInWord == 0)
			{
				numBytesInWord = 4;
			}
			int pos = 0;
			for (int j = this.dataLength - 1; j >= 0; j--)
			{
				uint val = this.data[j];
				for (int i = numBytesInWord - 1; i >= 0; i--)
				{
					result[pos + i] = (byte)(val & 0xFF);
					val >>= 8;
				}
				pos += numBytesInWord;
				numBytesInWord = 4;
			}
			return result;
		}

		public void setBit(uint bitNum)
		{
			uint bytePos = bitNum >> 5;
			byte bitPos = (byte)(bitNum & 0x1F);
			uint mask = (uint)(1 << (int)bitPos);
			this.data[bytePos] |= mask;
			if (bytePos >= this.dataLength)
			{
				this.dataLength = (int)(bytePos + 1);
			}
		}

		public void unsetBit(uint bitNum)
		{
			uint bytePos = bitNum >> 5;
			if (bytePos < this.dataLength)
			{
				byte bitPos = (byte)(bitNum & 0x1F);
				uint mask3 = (uint)(1 << (int)bitPos);
				uint mask2 = (uint)(-1 ^ (int)mask3);
				this.data[bytePos] &= mask2;
				if (this.dataLength > 1 && this.data[this.dataLength - 1] == 0)
				{
					this.dataLength--;
				}
			}
		}

		public BigInteger sqrt()
		{
			uint numBits2 = (uint)this.bitCount();
			numBits2 = (((numBits2 & 1) == 0) ? (numBits2 >> 1) : ((numBits2 >> 1) + 1));
			uint bytePos = numBits2 >> 5;
			byte bitPos = (byte)(numBits2 & 0x1F);
			BigInteger result = new BigInteger();
			uint mask;
			if (bitPos == 0)
			{
				mask = 2147483648u;
			}
			else
			{
				mask = (uint)(1 << (int)bitPos);
				bytePos++;
			}
			result.dataLength = (int)bytePos;
			for (int i = (int)(bytePos - 1); i >= 0; i--)
			{
				while (mask != 0)
				{
					result.data[i] ^= mask;
					if (result * result > this)
					{
						result.data[i] ^= mask;
					}
					mask >>= 1;
				}
				mask = 2147483648u;
			}
			return result;
		}

		public static BigInteger[] LucasSequence(BigInteger P, BigInteger Q, BigInteger k, BigInteger n)
		{
			if (k.dataLength == 1 && k.data[0] == 0)
			{
				return new BigInteger[3]
				{
					0,
					(BigInteger)2 % n,
					(BigInteger)1 % n
				};
			}
			BigInteger constant2 = new BigInteger();
			int nLen = n.dataLength << 1;
			constant2.data[nLen] = 1u;
			constant2.dataLength = nLen + 1;
			constant2 /= n;
			int s = 0;
			for (int index = 0; index < k.dataLength; index++)
			{
				uint mask = 1u;
				int i = 0;
				while (i < 32)
				{
					if ((k.data[index] & mask) == 0)
					{
						mask <<= 1;
						s++;
						i++;
						continue;
					}
					index = k.dataLength;
					break;
				}
			}
			BigInteger t = k >> s;
			return BigInteger.LucasSequenceHelper(P, Q, t, n, constant2, s);
		}

		private static BigInteger[] LucasSequenceHelper(BigInteger P, BigInteger Q, BigInteger k, BigInteger n, BigInteger constant, int s)
		{
			BigInteger[] result = new BigInteger[3];
			if ((k.data[0] & 1) == 0)
			{
				throw new ArgumentException("Argument k must be odd.");
			}
			int numbits = k.bitCount();
			uint mask = (uint)(1 << (numbits & 0x1F) - 1);
			BigInteger v5 = (BigInteger)2 % n;
			BigInteger Q_k2 = (BigInteger)1 % n;
			BigInteger v4 = P % n;
			BigInteger u2 = Q_k2;
			bool flag = true;
			for (int j = k.dataLength - 1; j >= 0; j--)
			{
				while (mask != 0 && (j != 0 || mask != 1))
				{
					if ((k.data[j] & mask) != 0)
					{
						u2 = u2 * v4 % n;
						v5 = (v5 * v4 - P * Q_k2) % n;
						v4 = n.BarrettReduction(v4 * v4, n, constant);
						v4 = (v4 - (Q_k2 * Q << 1)) % n;
						if (flag)
						{
							flag = false;
						}
						else
						{
							Q_k2 = n.BarrettReduction(Q_k2 * Q_k2, n, constant);
						}
						Q_k2 = Q_k2 * Q % n;
					}
					else
					{
						u2 = (u2 * v5 - Q_k2) % n;
						v4 = (v5 * v4 - P * Q_k2) % n;
						v5 = n.BarrettReduction(v5 * v5, n, constant);
						v5 = (v5 - (Q_k2 << 1)) % n;
						if (flag)
						{
							Q_k2 = Q % n;
							flag = false;
						}
						else
						{
							Q_k2 = n.BarrettReduction(Q_k2 * Q_k2, n, constant);
						}
					}
					mask >>= 1;
				}
				mask = 2147483648u;
			}
			u2 = (u2 * v5 - Q_k2) % n;
			v5 = (v5 * v4 - P * Q_k2) % n;
			if (flag)
			{
				flag = false;
			}
			else
			{
				Q_k2 = n.BarrettReduction(Q_k2 * Q_k2, n, constant);
			}
			Q_k2 = Q_k2 * Q % n;
			for (int j = 0; j < s; j++)
			{
				u2 = u2 * v5 % n;
				v5 = (v5 * v5 - (Q_k2 << 1)) % n;
				if (flag)
				{
					Q_k2 = Q % n;
					flag = false;
				}
				else
				{
					Q_k2 = n.BarrettReduction(Q_k2 * Q_k2, n, constant);
				}
			}
			result[0] = u2;
			result[1] = v5;
			result[2] = Q_k2;
			return result;
		}

		public static void MulDivTest(int rounds)
		{
			Random rand = new Random();
			byte[] val3 = new byte[64];
			byte[] val2 = new byte[64];
			int count = 0;
			BigInteger bn9;
			BigInteger bn8;
			BigInteger bn7;
			BigInteger bn6;
			BigInteger bn5;
			while (true)
			{
				if (count < rounds)
				{
					int t3;
					for (t3 = 0; t3 == 0; t3 = (int)(rand.NextDouble() * 65.0))
					{
					}
					int t2;
					for (t2 = 0; t2 == 0; t2 = (int)(rand.NextDouble() * 65.0))
					{
					}
					bool done2 = false;
					while (!done2)
					{
						for (int i = 0; i < 64; i++)
						{
							if (i < t3)
							{
								val3[i] = (byte)(rand.NextDouble() * 256.0);
							}
							else
							{
								val3[i] = 0;
							}
							if (val3[i] != 0)
							{
								done2 = true;
							}
						}
					}
					done2 = false;
					while (!done2)
					{
						for (int i = 0; i < 64; i++)
						{
							if (i < t2)
							{
								val2[i] = (byte)(rand.NextDouble() * 256.0);
							}
							else
							{
								val2[i] = 0;
							}
							if (val2[i] != 0)
							{
								done2 = true;
							}
						}
					}
					while (val3[0] == 0)
					{
						val3[0] = (byte)(rand.NextDouble() * 256.0);
					}
					while (val2[0] == 0)
					{
						val2[0] = (byte)(rand.NextDouble() * 256.0);
					}
					Console.WriteLine(count);
					bn9 = new BigInteger(val3, t3);
					bn8 = new BigInteger(val2, t2);
					bn7 = bn9 / bn8;
					bn6 = bn9 % bn8;
					bn5 = bn7 * bn8 + bn6;
					if (!(bn5 != bn9))
					{
						count++;
						continue;
					}
					break;
				}
				return;
			}
			Console.WriteLine("Error at " + count);
			Console.WriteLine(bn9 + "\n");
			Console.WriteLine(bn8 + "\n");
			Console.WriteLine(bn7 + "\n");
			Console.WriteLine(bn6 + "\n");
			Console.WriteLine(bn5 + "\n");
		}

		public static void RSATest(int rounds)
		{
			Random rand = new Random(1);
			byte[] val = new byte[64];
			BigInteger bi_e = new BigInteger("a932b948feed4fb2b692609bd22164fc9edb59fae7880cc1eaff7b3c9626b7e5b241c27a974833b2622ebe09beb451917663d47232488f23a117fc97720f1e7", 16);
			BigInteger bi_d = new BigInteger("4adf2f7a89da93248509347d2ae506d683dd3a16357e859a980c4f77a4e2f7a01fae289f13a851df6e9db5adaa60bfd2b162bbbe31f7c8f828261a6839311929d2cef4f864dde65e556ce43c89bbbf9f1ac5511315847ce9cc8dc92470a747b8792d6a83b0092d2e5ebaf852c85cacf34278efa99160f2f8aa7ee7214de07b7", 16);
			BigInteger bi_n = new BigInteger("e8e77781f36a7b3188d711c2190b560f205a52391b3479cdb99fa010745cbeba5f2adc08e1de6bf38398a0487c4a73610d94ec36f17f3f46ad75e17bc1adfec99839589f45f95ccc94cb2a5c500b477eb3323d8cfab0c8458c96f0147a45d27e45a4d11d54d77684f65d48f15fafcc1ba208e71e921b9bd9017c16a5231af7f", 16);
			Console.WriteLine("e =\n" + bi_e.ToString(10));
			Console.WriteLine("\nd =\n" + bi_d.ToString(10));
			Console.WriteLine("\nn =\n" + bi_n.ToString(10) + "\n");
			int count = 0;
			BigInteger bi_data;
			while (true)
			{
				if (count < rounds)
				{
					int t;
					for (t = 0; t == 0; t = (int)(rand.NextDouble() * 65.0))
					{
					}
					bool done = false;
					while (!done)
					{
						for (int i = 0; i < 64; i++)
						{
							if (i < t)
							{
								val[i] = (byte)(rand.NextDouble() * 256.0);
							}
							else
							{
								val[i] = 0;
							}
							if (val[i] != 0)
							{
								done = true;
							}
						}
					}
					while (val[0] == 0)
					{
						val[0] = (byte)(rand.NextDouble() * 256.0);
					}
					Console.Write("Round = " + count);
					bi_data = new BigInteger(val, t);
					BigInteger bi_encrypted = bi_data.ModPow(bi_e, bi_n);
					BigInteger bi_decrypted = bi_encrypted.ModPow(bi_d, bi_n);
					if (!(bi_decrypted != bi_data))
					{
						Console.WriteLine(" <PASSED>.");
						count++;
						continue;
					}
					break;
				}
				return;
			}
			Console.WriteLine("\nError at round " + count);
			Console.WriteLine(bi_data + "\n");
		}

		public static void RSATest2(int rounds)
		{
			Random rand = new Random();
			byte[] val = new byte[64];
			byte[] pseudoPrime3 = new byte[64]
			{
				133,
				132,
				100,
				253,
				112,
				106,
				159,
				240,
				148,
				12,
				62,
				44,
				116,
				52,
				5,
				201,
				85,
				179,
				133,
				50,
				152,
				113,
				249,
				65,
				33,
				95,
				2,
				158,
				234,
				86,
				141,
				140,
				68,
				204,
				238,
				238,
				61,
				44,
				157,
				44,
				18,
				65,
				30,
				241,
				197,
				50,
				195,
				170,
				49,
				74,
				82,
				216,
				232,
				175,
				66,
				244,
				114,
				161,
				42,
				13,
				151,
				177,
				49,
				179
			};
			byte[] pseudoPrime2 = new byte[64]
			{
				153,
				152,
				202,
				184,
				94,
				215,
				229,
				220,
				40,
				92,
				111,
				14,
				21,
				9,
				89,
				110,
				132,
				243,
				129,
				205,
				222,
				66,
				220,
				147,
				194,
				122,
				98,
				172,
				108,
				175,
				222,
				116,
				227,
				203,
				96,
				32,
				56,
				156,
				33,
				195,
				220,
				200,
				162,
				77,
				198,
				42,
				53,
				127,
				243,
				169,
				232,
				29,
				123,
				44,
				120,
				250,
				184,
				2,
				85,
				128,
				155,
				194,
				165,
				203
			};
			BigInteger bi_p = new BigInteger(pseudoPrime3);
			BigInteger bi_q = new BigInteger(pseudoPrime2);
			BigInteger bi_pq = (bi_p - (BigInteger)1) * (bi_q - (BigInteger)1);
			BigInteger bi_n = bi_p * bi_q;
			int count = 0;
			BigInteger bi_data;
			while (true)
			{
				if (count < rounds)
				{
					BigInteger bi_e = bi_pq.genCoPrime(512, rand);
					BigInteger bi_d = bi_e.modInverse(bi_pq);
					Console.WriteLine("\ne =\n" + bi_e.ToString(10));
					Console.WriteLine("\nd =\n" + bi_d.ToString(10));
					Console.WriteLine("\nn =\n" + bi_n.ToString(10) + "\n");
					int t;
					for (t = 0; t == 0; t = (int)(rand.NextDouble() * 65.0))
					{
					}
					bool done = false;
					while (!done)
					{
						for (int i = 0; i < 64; i++)
						{
							if (i < t)
							{
								val[i] = (byte)(rand.NextDouble() * 256.0);
							}
							else
							{
								val[i] = 0;
							}
							if (val[i] != 0)
							{
								done = true;
							}
						}
					}
					while (val[0] == 0)
					{
						val[0] = (byte)(rand.NextDouble() * 256.0);
					}
					Console.Write("Round = " + count);
					bi_data = new BigInteger(val, t);
					BigInteger bi_encrypted = bi_data.ModPow(bi_e, bi_n);
					BigInteger bi_decrypted = bi_encrypted.ModPow(bi_d, bi_n);
					if (!(bi_decrypted != bi_data))
					{
						Console.WriteLine(" <PASSED>.");
						count++;
						continue;
					}
					break;
				}
				return;
			}
			Console.WriteLine("\nError at round " + count);
			Console.WriteLine(bi_data + "\n");
		}

		public static void SqrtTest(int rounds)
		{
			Random rand = new Random();
			int count = 0;
			BigInteger a;
			while (true)
			{
				if (count < rounds)
				{
					int t;
					for (t = 0; t == 0; t = (int)(rand.NextDouble() * 1024.0))
					{
					}
					Console.Write("Round = " + count);
					a = new BigInteger();
					a.genRandomBits(t, rand);
					BigInteger b = a.sqrt();
					BigInteger c = (b + (BigInteger)1) * (b + (BigInteger)1);
					if (!(c <= a))
					{
						Console.WriteLine(" <PASSED>.");
						count++;
						continue;
					}
					break;
				}
				return;
			}
			Console.WriteLine("\nError at round " + count);
			Console.WriteLine(a + "\n");
		}

		public static void Main(string[] args)
		{
			byte[] pseudoPrime3 = new byte[65]
			{
				0,
				133,
				132,
				100,
				253,
				112,
				106,
				159,
				240,
				148,
				12,
				62,
				44,
				116,
				52,
				5,
				201,
				85,
				179,
				133,
				50,
				152,
				113,
				249,
				65,
				33,
				95,
				2,
				158,
				234,
				86,
				141,
				140,
				68,
				204,
				238,
				238,
				61,
				44,
				157,
				44,
				18,
				65,
				30,
				241,
				197,
				50,
				195,
				170,
				49,
				74,
				82,
				216,
				232,
				175,
				66,
				244,
				114,
				161,
				42,
				13,
				151,
				177,
				49,
				179
			};
			byte[] pseudoPrime2 = new byte[65]
			{
				0,
				153,
				152,
				202,
				184,
				94,
				215,
				229,
				220,
				40,
				92,
				111,
				14,
				21,
				9,
				89,
				110,
				132,
				243,
				129,
				205,
				222,
				66,
				220,
				147,
				194,
				122,
				98,
				172,
				108,
				175,
				222,
				116,
				227,
				203,
				96,
				32,
				56,
				156,
				33,
				195,
				220,
				200,
				162,
				77,
				198,
				42,
				53,
				127,
				243,
				169,
				232,
				29,
				123,
				44,
				120,
				250,
				184,
				2,
				85,
				128,
				155,
				194,
				165,
				203
			};
			Console.WriteLine("List of primes < 2000\n---------------------");
			int limit = 100;
			int count = 0;
			for (int i = 0; i < 2000; i++)
			{
				if (i >= limit)
				{
					Console.WriteLine();
					limit += 100;
				}
				BigInteger p = new BigInteger(-i);
				if (p.isProbablePrime())
				{
					Console.Write(i + ", ");
					count++;
				}
			}
			Console.WriteLine("\nCount = " + count);
			BigInteger bi = new BigInteger(pseudoPrime3);
			Console.WriteLine("\n\nPrimality testing for\n" + bi.ToString() + "\n");
			Console.WriteLine("SolovayStrassenTest(5) = " + bi.SolovayStrassenTest(5));
			Console.WriteLine("RabinMillerTest(5) = " + bi.RabinMillerTest(5));
			Console.WriteLine("FermatLittleTest(5) = " + bi.FermatLittleTest(5));
			Console.WriteLine("isProbablePrime() = " + bi.isProbablePrime());
			Console.Write("\nGenerating 512-bits random pseudoprime. . .");
			Random rand = new Random();
			BigInteger prime = BigInteger.genPseudoPrime(512, 5, rand);
			Console.WriteLine("\n" + prime);
		}
	}
}
