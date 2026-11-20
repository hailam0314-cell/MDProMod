using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

internal static class ClassicMessageSplitter
{
	internal sealed record Message(byte[] Bytes, bool Framed);

	private ref struct Cursor
	{
		private readonly ReadOnlySpan<byte> _data;

		public int Position { get; private set; }

		public Cursor(ReadOnlySpan<byte> data)
		{
			_data = data;
			Position = 0;
		}

		public byte Byte()
		{
			Skip(1);
			return _data[Position - 1];
		}

		public ushort UInt16()
		{
			int position = Position;
			Skip(2);
			ReadOnlySpan<byte> data = _data;
			int num = position;
			return BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(num, data.Length - num));
		}

		public void Entries(int size)
		{
			byte b = Byte();
			Skip(b * size);
		}

		public void Skip(int count)
		{
			if (count < 0 || count > _data.Length - Position)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			Position += count;
		}
	}

	public static List<Message> Split(ReadOnlySpan<byte> buffer)
	{
		List<Message> list = new List<Message>();
		int num2;
		for (int i = 0; i < buffer.Length; i += num2)
		{
			int num = i;
			ReadOnlySpan<byte> data = buffer.Slice(num, buffer.Length - num);
			num2 = MessageLength(data);
			if (num2 <= 0 || num2 > data.Length)
			{
				list.Add(new Message(data.ToArray(), Framed: false));
				break;
			}
			list.Add(new Message(data.Slice(0, num2).ToArray(), Framed: true));
		}
		return list;
	}

	public static int MessageLength(ReadOnlySpan<byte> data)
	{
		if (data.IsEmpty)
		{
			return -1;
		}
		try
		{
			Cursor cursor = new Cursor(data);
			switch (cursor.Byte())
			{
			case 2:
				cursor.Skip(6);
				break;
			case 4:
				cursor.Skip(18);
				break;
			case 5:
				cursor.Skip(2);
				break;
			case 10:
				cursor.Skip(1);
				cursor.Entries(11);
				cursor.Entries(8);
				cursor.Skip(2);
				break;
			case 11:
			{
				cursor.Byte();
				for (int l = 0; l < 5; l++)
				{
					cursor.Entries(7);
				}
				cursor.Entries(11);
				cursor.Skip(3);
				break;
			}
			case 12:
				cursor.Skip(13);
				break;
			case 13:
				cursor.Skip(5);
				break;
			case 14:
				cursor.Skip(1);
				cursor.Entries(4);
				break;
			case 15:
			case 20:
				cursor.Skip(4);
				cursor.Entries(8);
				break;
			case 16:
			{
				// ocgcore 2025+: player, count, special count, two hint timings; then per chain
				// flag, forced, code(4), location info(4), description(4).
				// (Before: a forced byte in the header and 13 bytes per chain.)
				cursor.Skip(1);
				byte b3 = cursor.Byte();
				cursor.Skip(9 + b3 * 14);
				break;
			}
			case 18:
			case 19:
			case 24:
				cursor.Skip(6);
				break;
			case 21:
			case 25:
				cursor.Skip(1);
				cursor.Entries(7);
				break;
			case 22:
				cursor.Skip(5);
				cursor.Entries(9);
				break;
			case 23:
				cursor.Skip(8);
				cursor.Entries(11);
				cursor.Entries(11);
				break;
			case 26:
				cursor.Skip(5);
				cursor.Entries(8);
				cursor.Entries(8);
				break;
			case 30:
			case 42:
				cursor.Skip(1);
				cursor.Entries(7);
				break;
			case 31:
				// ocgcore 2025+: player, skip-panel flag, count, cards.
				cursor.Skip(2);
				cursor.Entries(7);
				break;
			case 32:
			case 34:
			case 35:
			case 40:
			case 71:
			case 72:
			case 73:
			case 75:
			case 76:
			case 132:
			case 133:
				cursor.Skip(1);
				break;
			case 33:
			case 39:
			case 90:
				cursor.Skip(1);
				cursor.Entries(4);
				break;
			case 36:
				cursor.Skip(1);
				cursor.Entries(8);
				break;
			case 38:
				cursor.Skip(6);
				break;
			case 41:
				cursor.Skip(2);
				break;
			case 50:
			case 55:
			case 70:
				cursor.Skip(16);
				break;
			case 53:
			case 160:
				cursor.Skip(9);
				break;
			case 54:
			case 60:
			case 62:
			case 64:
			case 93:
			case 96:
			case 97:
			case 110:
			case 120:
				cursor.Skip(8);
				break;
			case 56:
			case 95:
			case 170:
				cursor.Skip(4);
				break;
			case 80:
			case 81:
				cursor.Skip(1);
				cursor.Entries(4);
				break;
			case 83:
				cursor.Entries(4);
				break;
			case 91:
			case 92:
			case 94:
			case 100:
				cursor.Skip(5);
				break;
			case 101:
			case 102:
				cursor.Skip(7);
				break;
			case 111:
				cursor.Skip(26);
				break;
			case 130:
			case 131:
				cursor.Skip(1);
				cursor.Entries(1);
				break;
			case 140:
			case 141:
				cursor.Skip(6);
				break;
			case 142:
			case 143:
			case 144:
				cursor.Skip(1);
				cursor.Entries(4);
				break;
			case 161:
			{
				cursor.Skip(1);
				cursor.Byte();
				byte b = cursor.Byte();
				cursor.Byte();
				byte b2 = cursor.Byte();
				cursor.Skip(4 + 4 * (b + b2));
				break;
			}
			case 162:
			{
				cursor.Skip(1);
				for (int i = 0; i < 2; i++)
				{
					cursor.Skip(4);
					for (int j = 0; j < 7; j++)
					{
						if (cursor.Byte() != 0)
						{
							cursor.Skip(2);
						}
					}
					for (int k = 0; k < 8; k++)
					{
						if (cursor.Byte() != 0)
						{
							cursor.Skip(1);
						}
					}
					cursor.Skip(6);
				}
				cursor.Entries(15);
				break;
			}
			case 163:
			case 164:
			{
				ushort num = cursor.UInt16();
				cursor.Skip(num + 1);
				break;
			}
			case 165:
				cursor.Skip(6);
				break;
			case 200:
				cursor.Skip(1);
				break;
			default:
				return -1;
			case 1:
			case 3:
			case 8:
			case 37:
			case 61:
			case 63:
			case 65:
			case 74:
			case 112:
			case 113:
			case 114:
			case 121:
			case 122:
			case 123:
				break;
			}
			return cursor.Position;
		}
		catch (ArgumentOutOfRangeException)
		{
			return -1;
		}
	}

	public static int SelfTest(string? jsonlPath = null)
	{
		(string, int[])[] array = new(string, int[])[13]
		{
			("2901000201011B000000", new int[2] { 3, 7 }),
			("0201002900000002010129000000", new int[2] { 7, 7 }),
			("2101079FFB3B049F544E00A3A9570586926800869268009F544E00869268002001", new int[2] { 31, 2 }),
			("1F0000019FFB3B04010203", new int[1] { 11 }),
			("1F000002E9BB4B02010202E9BB4B02010203", new int[1] { 18 }),
			("32002E0D050002000A0008020A0004000236002E0D050008020A", new int[2] { 17, 9 }),
			("100000000000000001000000", new int[1] { 12 }),
			("100000000000000001000000290100", new int[2] { 12, 3 }),
			("100001000000000000002100000000A520040004010101500A42", new int[1] { 26 }),
			("1000020000000000000000040000F3EAF40500101205000000000000F392C9030010160500000000", new int[1] { 40 }),
			("1000030000000000000000040000F392C90300100705000000000000F3EAF40500100C05000000000000F392C9030010100500000000", new int[1] { 54 }),
			("0B00000000000289327704000200FBDC140100020101F9F78605000400917F6F58000101", new int[1] { 36 }),
			("0B0003F3EAF405000200387D6804000201B2BD0800000203000003F3EAF405000200387D6804000201B2BD08000002030289327704000202CC11480400020402B2BD080000020320DB8B00CC11480400020400000000000101", new int[1] { 89 })
		};
		(string, int[])[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(string, int[]) tuple = array2[i];
			string item = tuple.Item1;
			int[] item2 = tuple.Item2;
			List<Message> source = Split(Convert.FromHexString(item));
			byte[] array3 = Convert.FromHexString(item);
			Console.WriteLine("splitter case " + item.Substring(0, Math.Min(12, item.Length)) + " -> " + string.Join(",", source.Select((Message m) => (m.Framed ? "F" : "U") + m.Bytes.Length)) + " expected " + string.Join(",", item2) + " | bytes=" + array3.Length + " len=" + MessageLength(array3.AsSpan()) + " head=" + Convert.ToHexString(array3.AsSpan(0, Math.Min(8, array3.Length))));
			if (source.Any((Message m) => !m.Framed) || !source.Select((Message m) => m.Bytes.Length).SequenceEqual(item2))
			{
				throw new InvalidOperationException("Splitter regression failed: " + item);
			}
		}
		List<Message> list = Split(Convert.FromHexString("FF010203"));
		if (list != null && list.Count == 1)
		{
			Message message = list[0];
			if ((object)message != null && !message.Framed)
			{
				list = Split(Convert.FromHexString("1000000000000000010000"));
				if (list != null && list.Count == 1)
				{
					message = list[0];
					if ((object)message != null && !message.Framed)
					{
						int num = array.Length;
						if (jsonlPath != null)
						{
							int num2 = 0;
							int num3 = 0;
							List<string> list2 = new List<string>();
							foreach (string item3 in File.ReadLines(jsonlPath))
							{
								if (!item3.StartsWith('{'))
								{
									continue;
								}
								using JsonDocument jsonDocument = JsonDocument.Parse(item3);
								JsonElement rootElement = jsonDocument.RootElement;
								if (!rootElement.TryGetProperty("type", out var value) || value.GetString() != "message")
								{
									continue;
								}
								byte[] array4 = Convert.FromHexString(rootElement.GetProperty("hex").GetString());
								List<Message> list3 = Split(array4);
								if (list3.Any((Message m) => !m.Framed))
								{
									num2++;
									if (list2.Count < 5)
									{
										list2.Add(array4[0].ToString());
									}
								}
								else
								{
									num3 += list3.Count;
								}
							}
							Console.WriteLine($"jsonl advisory: framed={num3} unframed={num2}" + ((list2.Count > 0) ? (" (functions: " + string.Join(",", list2) + ")") : ""));
							num += num3;
						}
						return num;
					}
				}
				throw new InvalidOperationException("Truncated chain header must remain an opaque tail");
			}
		}
		throw new InvalidOperationException("Unknown message must remain an opaque tail");
	}
}
