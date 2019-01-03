#define DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ExitGames.Client.Photon
{
	/// <summary>
	/// Contains several (more or less) useful static methods, mostly used for debugging.
	/// </summary>
	public class SupportClass
	{
		public delegate int IntegerMillisecondsDelegate();

		/// <summary>
		/// Class to wrap static access to the random.Next() call in a thread safe manner.
		/// </summary>
		public class ThreadSafeRandom
		{
			private static readonly Random _r = new Random();

			public static int Next()
			{
				lock (ThreadSafeRandom._r)
				{
					return ThreadSafeRandom._r.Next();
				}
			}
		}

		protected internal static IntegerMillisecondsDelegate IntegerMilliseconds = () => Environment.TickCount;

		public static uint CalculateCrc(byte[] buffer, int length)
		{
			uint crc = 4294967295u;
			uint poly = 3988292384u;
			byte current2 = 0;
			for (int bufferIndex = 0; bufferIndex < length; bufferIndex++)
			{
				current2 = buffer[bufferIndex];
				crc ^= current2;
				for (int i = 0; i < 8; i++)
				{
					crc = (((crc & 1) == 0) ? (crc >> 1) : (crc >> 1 ^ poly));
				}
			}
			return crc;
		}

		public static List<MethodInfo> GetMethods(Type type, Type attribute)
		{
			List<MethodInfo> fittingMethods = new List<MethodInfo>();
			if (type == null)
			{
				return fittingMethods;
			}
			MethodInfo[] declaredMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			MethodInfo[] array = declaredMethods;
			foreach (MethodInfo methodInfo in array)
			{
				if (attribute == null || methodInfo.IsDefined(attribute, false))
				{
					fittingMethods.Add(methodInfo);
				}
			}
			return fittingMethods;
		}

		/// <summary>
		/// Gets the local machine's "milliseconds since start" value (precision is described in remarks).
		/// </summary>
		/// <remarks>
		/// This method uses Environment.TickCount (cheap but with only 16ms precision).
		/// PhotonPeer.LocalMsTimestampDelegate is available to set the delegate (unless already connected).
		/// </remarks>
		/// <returns>Fraction of the current time in Milliseconds (this is not a proper datetime timestamp).</returns>
		public static int GetTickCount()
		{
			return SupportClass.IntegerMilliseconds();
		}

		/// <summary>
		/// Creates a background thread that calls the passed function in 100ms intervals, as long as that returns true.
		/// </summary>
		/// <param name="myThread"></param>
		public static void CallInBackground(Func<bool> myThread)
		{
			SupportClass.CallInBackground(myThread, 100);
		}

		/// <summary>
		/// Creates a background thread that calls the passed function in 100ms intervals, as long as that returns true.
		/// </summary>
		/// <param name="myThread"></param>
		/// <param name="millisecondsInterval">Milliseconds to sleep between calls of myThread.</param>
		public static void CallInBackground(Func<bool> myThread, int millisecondsInterval)
		{
			Thread x = new Thread((ThreadStart)delegate
			{
				while (myThread())
				{
					Thread.Sleep(millisecondsInterval);
				}
			});
			x.IsBackground = true;
			x.Start();
		}

		/// <summary>
		/// Writes the exception's stack trace to the received stream.
		/// </summary>
		/// <param name="throwable">Exception to obtain information from.</param>
		/// <param name="stream">Output sream used to write to.</param>
		public static void WriteStackTrace(Exception throwable, TextWriter stream)
		{
			if (stream != null)
			{
				stream.WriteLine(throwable.ToString());
				stream.WriteLine(throwable.StackTrace);
				stream.Flush();
			}
			else
			{
				Debug.WriteLine(throwable.ToString());
				Debug.WriteLine(throwable.StackTrace);
			}
		}

		/// <summary>
		/// Writes the exception's stack trace to the received stream. Writes to: System.Diagnostics.Debug.
		/// </summary>
		/// <param name="throwable">Exception to obtain information from.</param>
		public static void WriteStackTrace(Exception throwable)
		{
			SupportClass.WriteStackTrace(throwable, null);
		}

		/// <summary>
		/// This method returns a string, representing the content of the given IDictionary.
		/// Returns "null" if parameter is null.
		/// </summary>
		/// <param name="dictionary">
		/// IDictionary to return as string.
		/// </param>
		/// <returns>
		/// The string representation of keys and values in IDictionary.
		/// </returns>
		public static string DictionaryToString(IDictionary dictionary)
		{
			return SupportClass.DictionaryToString(dictionary, true);
		}

		/// <summary>
		/// This method returns a string, representing the content of the given IDictionary.
		/// Returns "null" if parameter is null.
		/// </summary>
		/// <param name="dictionary">IDictionary to return as string.</param>
		/// <param name="includeTypes"> </param>
		public static string DictionaryToString(IDictionary dictionary, bool includeTypes)
		{
			if (dictionary == null)
			{
				return "null";
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("{");
			foreach (object key in dictionary.Keys)
			{
				if (sb.Length > 1)
				{
					sb.Append(", ");
				}
				Type valueType;
				string value;
				if (dictionary[key] == null)
				{
					valueType = typeof(object);
					value = "null";
				}
				else
				{
					valueType = dictionary[key].GetType();
					value = dictionary[key].ToString();
				}
				if (typeof(IDictionary) == valueType || typeof(Hashtable) == valueType)
				{
					value = SupportClass.DictionaryToString((IDictionary)dictionary[key]);
				}
				if (typeof(string[]) == valueType)
				{
					value = string.Format("{{{0}}}", string.Join(",", (string[])dictionary[key]));
				}
				if (includeTypes)
				{
					sb.AppendFormat("({0}){1}=({2}){3}", key.GetType().Name, key, valueType.Name, value);
				}
				else
				{
					sb.AppendFormat("{0}={1}", key, value);
				}
			}
			sb.Append("}");
			return sb.ToString();
		}

		[Obsolete("Use DictionaryToString() instead.")]
		public static string HashtableToString(Hashtable hash)
		{
			return SupportClass.DictionaryToString(hash);
		}

		/// <summary>
		/// Inserts the number's value into the byte array, using Big-Endian order (a.k.a. Network-byte-order).
		/// </summary>
		/// <param name="buffer">Byte array to write into.</param>
		/// <param name="index">Index of first position to write to.</param>
		/// <param name="number">Number to write.</param>
		[Obsolete("Use Protocol.Serialize() instead.")]
		public static void NumberToByteArray(byte[] buffer, int index, short number)
		{
			Protocol.Serialize(number, buffer, ref index);
		}

		/// <summary>
		/// Inserts the number's value into the byte array, using Big-Endian order (a.k.a. Network-byte-order).
		/// </summary>
		/// <param name="buffer">Byte array to write into.</param>
		/// <param name="index">Index of first position to write to.</param>
		/// <param name="number">Number to write.</param>
		[Obsolete("Use Protocol.Serialize() instead.")]
		public static void NumberToByteArray(byte[] buffer, int index, int number)
		{
			Protocol.Serialize(number, buffer, ref index);
		}

		/// <summary>
		/// Converts a byte-array to string (useful as debugging output).
		/// Uses BitConverter.ToString(list) internally after a null-check of list.
		/// </summary>
		/// <param name="list">Byte-array to convert to string.</param>
		/// <returns>
		/// List of bytes as string.
		/// </returns>
		public static string ByteArrayToString(byte[] list)
		{
			if (list == null)
			{
				return string.Empty;
			}
			return BitConverter.ToString(list);
		}
	}
}
