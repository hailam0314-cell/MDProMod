using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

internal static class DuelHostProgram
{
	private sealed class DuelCore : IDisposable
	{
		private readonly string _root;

		private readonly Dictionary<uint, CardInfo> _cards = new Dictionary<uint, CardInfo>();

		private static readonly Dictionary<uint, string[]> _cardTexts = new Dictionary<uint, string[]>();

		private static readonly Dictionary<int, string> _systemStrings = new Dictionary<int, string>();

		private static bool _optionTextsLoaded;

		private readonly Dictionary<string, (nint Ptr, int Length)> _scripts = new Dictionary<string, (nint, int)>(StringComparer.OrdinalIgnoreCase);

		private uint _creatingCard;

		private readonly HashSet<string> _scriptMisses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		private readonly nint _buffer = Marshal.AllocHGlobal(131072);

		private readonly ScriptReader _scriptReader;

		private readonly CardReader _cardReader;

		private readonly MessageHandler _messageHandler;

		private readonly object _gate = new object();

		private nint _duel;

		private bool _disposed;

		private bool _waiting;

		private byte[]? _lastPromptBytes;

		private int _lastPromptLength;

		private readonly bool _auto;

		private readonly bool _autoPassChain;

		private volatile int _timing = 1;

		private volatile int _surrenderedBy = -1;

		internal const int TimingAllValue = 0;

		internal const int TimingDefaultValue = 1;

		internal const int TimingOffValue = 2;

		private readonly uint _seed;

		private readonly List<WindBotAi> _ais = new List<WindBotAi>();

		private int _aiRetries;

		private long _promptSeq;

		private byte _autoAnswerFunction;

		private readonly ManualResetEventSlim _response = new ManualResetEventSlim(initialState: false);

		private int _endedEmitted;

		internal static int CardTextCount => _cardTexts.Count;

		internal static int SystemStringCount => _systemStrings.Count;

		public unsafe DuelCore(string root, string? deck0, string? deck1, bool auto, uint seed, IReadOnlyList<WindBotOptions>? aiOptions = null, bool autoPassChain = false)
		{
			_root = root;
			_auto = auto;
			_autoPassChain = autoPassChain;
			_seed = seed;
			LoadCards();
			LoadScripts();
			LoadOptionTexts(_root);
			_scriptReader = ReadScript;
			_cardReader = ReadCard;
			_messageHandler = (nint _, uint _) => 0u;
			Native.SetCallbacks(_scriptReader, _cardReader, _messageHandler);
			_duel = Native.CreateDuel(seed);
			if (_duel == IntPtr.Zero)
			{
				throw new InvalidOperationException("create_duel returned null");
			}
			Native.SetPlayerInfo(_duel, 0, 8000, 5, 1);
			Native.SetPlayerInfo(_duel, 1, 8000, 5, 1);
			AddDeck(deck0 ?? "starter.ydk", 0);
			AddDeck(deck1 ?? "starter.ydk", 1);
			if (aiOptions == null)
			{
				return;
			}
			WindBotAi.SetRandomSeed(seed);
			foreach (WindBotOptions aiOption in aiOptions)
			{
				WindBotAi windBotAi = new WindBotAi(aiOption);
				_ais.Add(windBotAi);
				Emit(new
				{
					type = "ai",
					ready = true,
					seat = windBotAi.Seat,
					deck = aiOption.Deck,
					seeded = windBotAi.SeededCards
				});
			}
		}

		public void Run(CancellationToken token)
		{
			try
			{
				Native.StartDuel(_duel, 0);
				byte[] array = new byte[131072];
				while (!token.IsCancellationRequested && !_disposed)
				{
					if (_surrenderedBy >= 0)
					{
						EmitSurrender();
						break;
					}
					uint num = Native.Process(_duel);
					int num2 = (int)(num & 0xFFFFFFF);
					uint num3 = num & 0xF0000000u;
					if (num2 > 0)
					{
						Native.GetMessage(_duel, _buffer);
						Marshal.Copy(_buffer, array, 0, num2);
						foreach (ClassicMessageSplitter.Message item in ClassicMessageSplitter.Split(array.AsSpan(0, num2)))
						{
							byte[] messageBuffer = item.Bytes;
							int messageLength = messageBuffer.Length;
							try
							{
								EmitMessage(messageBuffer, messageLength, num);
								if (!item.Framed)
								{
									Emit(new
									{
										type = "warn",
										message = "Unknown or truncated core message; skipping the rest of this batch",
										function = messageBuffer[0],
										length = messageLength
									});
									break;
								}
								if (NeedsResponse(messageBuffer[0]))
								{
									RefreshAiState();
								}
								foreach (WindBotAi ai in _ais)
								{
									if (!NeedsResponse(messageBuffer[0]) || ai.Seat == ReadAskedPlayer(messageBuffer, messageLength))
									{
										try
										{
											ai.FeedMessage(messageBuffer, messageLength);
										}
										catch (Exception ex)
										{
											Emit(new
											{
												type = "error",
												message = "AI feed failed: " + ex.Message,
												stack = ex.ToString()
											});
										}
									}
								}
								if (messageBuffer[0] == 1)
								{
									_aiRetries++;
									if (!_auto && !_autoPassChain)
									{
										if (_aiRetries > 60)
										{
											Emit(new
											{
												type = "error",
												message = "连续应答非法，停止对局"
											});
											return;
										}
										Emit(new
										{
											type = "retry",
											message = "上次应答被核心判为非法，请重新选择"
										});
									}
									else
									{
										if (_aiRetries > 5)
										{
											Emit(new
											{
												type = "error",
												message = "AI 连续应答非法，停止对局"
											});
											return;
										}
										AutoAnswer(messageBuffer, messageLength);
									}
									continue;
								}
								if (!NeedsResponse(messageBuffer[0]))
								{
									goto IL_0648;
								}
								WindBotAi windBotAi = _ais.Find((WindBotAi a) => a.Seat == ReadAskedPlayer(messageBuffer, messageLength));
								if (windBotAi != null)
								{
									List<byte[]> list = windBotAi.DrainResponses();
									if (list.Count > 0)
									{
										ApplyAiResponse(list[list.Count - 1]);
										_aiRetries = 0;
									}
									else
									{
										AutoAnswer(messageBuffer, messageLength);
									}
									Emit(new
									{
										type = "ai",
										function = messageBuffer[0],
										name = GameMessageName(messageBuffer[0]),
										seat = windBotAi.Seat,
										responses = list.Count,
										prompt = Convert.ToHexString(messageBuffer.AsSpan(0, Math.Min(messageLength, 24))),
										reply = ((list.Count > 0) ? Convert.ToHexString(list[list.Count - 1]) : ""),
										state = ((messageBuffer[0] == 11) ? windBotAi.DumpState() : null)
									});
									continue;
								}
								if (_auto)
								{
									AutoAnswer(messageBuffer, messageLength);
									continue;
								}
								byte b = messageBuffer[0];
								bool flag = b == 16 && IsEmptyChainWindow(messageBuffer, messageLength);
								bool forcedChain = b == 16 && !flag && IsForcedChainWindow(messageBuffer, messageLength);
								if (ShouldAutoPass(b, flag, forcedChain, _timing, _autoPassChain, out string reason))
								{
									AutoAnswer(messageBuffer, messageLength);
									Emit(new
									{
										type = "autoPass",
										function = b,
										name = GameMessageName(b),
										seat = ReadAskedPlayer(messageBuffer, messageLength),
										reason = reason
									});
									continue;
								}
								Dictionary<string, object> dictionary;
								try
								{
									dictionary = BuildPrompt(messageBuffer, messageLength);
								}
								catch (Exception ex2)
								{
									Emit(new
									{
										type = "error",
										message = $"prompt build failed for {GameMessageName(messageBuffer[0])} ({messageLength} bytes): {ex2.Message}",
										stack = ex2.ToString()
									});
									dictionary = new Dictionary<string, object>
									{
										["type"] = "prompt",
										["function"] = (int)messageBuffer[0],
										["name"] = GameMessageName(messageBuffer[0]),
										["waiting"] = true,
										["summary"] = "unparsed",
										["responseKind"] = "integer",
										["raw"] = Convert.ToHexString(messageBuffer.AsSpan(0, messageLength))
									};
								}
								lock (_gate)
								{
									_waiting = true;
								}
								_lastPromptBytes = messageBuffer;
								_lastPromptLength = messageLength;
								dictionary["seq"] = ++_promptSeq;
								Emit(new
								{
									type = "state",
									waiting = true,
									function = messageBuffer[0]
								});
								Emit(dictionary);
								_response.Wait(token);
								_response.Reset();
								if (_surrenderedBy >= 0)
								{
									EmitSurrender();
									return;
								}
								goto IL_0648;
								IL_0648:
								byte b2 = messageBuffer[0];
								if ((b2 == 5 || b2 == 200) ? true : false)
								{
									return;
								}
							}
							catch (Exception ex3)
							{
								Emit(new
								{
									type = "error",
									message = "message handling failed for " + GameMessageName(messageBuffer[0]) + ": " + ex3.Message,
									stack = ex3.ToString()
								});
								return;
							}
						}
					}
					else
					{
						if (num3 == 536870912)
						{
							break;
						}
						Thread.Yield();
					}
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (ObjectDisposedException)
			{
			}
			finally
			{
				if (Interlocked.Exchange(ref _endedEmitted, 1) == 0)
				{
					Emit(new
					{
						type = "ended"
					});
				}
			}
		}

		public void CancelWait()
		{
			_response.Set();
		}

		internal static int TimingOf(string? mode)
		{
			string text = mode?.Trim().ToLowerInvariant();
			if (!(text == "all"))
			{
				if (text == "off")
				{
					return 2;
				}
				return 1;
			}
			return 0;
		}

		public void SetTiming(string? mode)
		{
			_timing = TimingOf(mode);
		}

		public void Surrender(int seat)
		{
			if (seat >= 0 && seat <= 1)
			{
				_surrenderedBy = seat;
				_response.Set();
			}
		}

		internal static bool ShouldAutoPass(int function, bool emptyChain, bool forcedChain, int timing, bool autoPassChain, out string reason)
		{
			if (function == 16 && (autoPassChain || (timing == 2 && !forcedChain)))
			{
				reason = (autoPassChain ? "auto-pass-chain" : "timing-off");
				return true;
			}
			if (function == 16 && emptyChain && timing != 0)
			{
				reason = "empty-chain";
				return true;
			}
			if (function == 12 && timing == 2)
			{
				reason = "timing-off-effect";
				return true;
			}
			reason = "";
			return false;
		}

		private void EmitSurrender()
		{
			int num = 1 - _surrenderedBy;
			if (num < 0 || num > 1)
			{
				num = 1;
			}
			Emit(new
			{
				type = "surrendered",
				by = _surrenderedBy,
				winner = num
			});
			Emit(new
			{
				type = "message",
				function = 5,
				name = "Win",
				length = 3,
				result = 0,
				winner = (byte)num,
				hex = $"05{num:X2}00"
			});
		}

		private void RefreshAiState()
		{
			int[] array = new int[2] { 0, 1 };
			foreach (int num in array)
			{
				int[] array2 = new int[6] { 2, 4, 8, 16, 32, 64 };
				foreach (int num2 in array2)
				{
					int num3 = Native.QueryField(_duel, num, num2, 15728639, _buffer, 0);
					if (num3 <= 0 || num3 > 131072)
					{
						continue;
					}
					byte[] array3 = new byte[num3 + 3];
					array3[0] = 6;
					array3[1] = (byte)num;
					array3[2] = (byte)num2;
					Marshal.Copy(_buffer, array3, 3, num3);
					EmitMessage(array3, array3.Length, 0u);
					foreach (WindBotAi ai in _ais)
					{
						if (ai.Seat != num && (num2 == 1 || num2 == 64))
						{
							continue;
						}
						byte[] array4 = (byte[])array3.Clone();
						if (ai.Seat != num && num2 != 16)
						{
							int num4;
							for (int k = 3; k + 4 <= array4.Length; k += num4)
							{
								num4 = BitConverter.ToInt32(array4, k);
								if (num4 < 4 || num4 > array4.Length - k)
								{
									break;
								}
								if (num4 >= 16 && (array4[k + 15] & 5) == 0)
								{
									Array.Clear(array4, k + 4, num4 - 4);
								}
							}
						}
						try
						{
							ai.FeedMessage(array4, array4.Length);
						}
						catch (Exception ex)
						{
							Emit(new
							{
								type = "warn",
								message = $"AI query feed failed (player={num} location={num2}): {ex.Message}",
								records = num3 / 1
							});
						}
					}
				}
			}
		}

		public void SetInteger(uint value)
		{
			lock (_gate)
			{
				if (!_waiting)
				{
					return;
				}
				Native.SetResponseInteger(_duel, value);
				_waiting = false;
			}
			_response.Set();
			Emit(new
			{
				type = "state",
				waiting = false
			});
		}

		public void SetBytes(byte[] value)
		{
			lock (_gate)
			{
				if (!_waiting || value.Length > 64)
				{
					return;
				}
				nint num = Marshal.AllocHGlobal(64);
				try
				{
					Marshal.Copy(value, 0, num, value.Length);
					Native.SetResponseBytes(_duel, num);
				}
				finally
				{
					Marshal.FreeHGlobal(num);
				}
				_waiting = false;
			}
			_response.Set();
			Emit(new
			{
				type = "state",
				waiting = false
			});
		}

		private void SendInt(int value)
		{
			Native.SetResponseInteger(_duel, (uint)value);
			_waiting = false;
			Emit(new
			{
				type = "response",
				function = _autoAnswerFunction,
				value = value
			});
		}

		private void SendBytes(byte[] value)
		{
			nint num = Marshal.AllocHGlobal(Math.Max(64, value.Length));
			try
			{
				Marshal.Copy(value, 0, num, value.Length);
				Native.SetResponseBytes(_duel, num);
			}
			finally
			{
				Marshal.FreeHGlobal(num);
			}
			_waiting = false;
			Emit(new
			{
				type = "responseBytes",
				function = _autoAnswerFunction,
				hex = Convert.ToHexString(value)
			});
		}

		private static byte FirstPosition(byte mask)
		{
			byte[] array = new byte[4] { 1, 4, 8, 2 };
			foreach (byte b in array)
			{
				if ((mask & b) != 0)
				{
					return b;
				}
			}
			return 1;
		}

		private static byte[] SelectFirst(int count, int min, int max)
		{
			if (count <= 0 || max <= 0)
			{
				return new byte[1];
			}
			min = Math.Clamp(min, 0, Math.Min(max, count));
			byte[] array = new byte[min + 1];
			array[0] = (byte)min;
			for (int i = 0; i < min; i++)
			{
				array[i + 1] = (byte)i;
			}
			return array;
		}

		private static byte[] SelectTribute(BinaryReader reader, int count, int min, int max)
		{
			byte[] array = new byte[count];
			for (int i = 0; i < count; i++)
			{
				reader.ReadUInt32();
				reader.ReadByte();
				reader.ReadByte();
				reader.ReadByte();
				array[i] = reader.ReadByte();
			}
			max = Math.Min(Math.Max(min, max), count);
			List<byte> list = new List<byte>();
			int num = 0;
			for (int j = 0; j < count; j++)
			{
				if (list.Count >= max)
				{
					break;
				}
				if (num >= min)
				{
					break;
				}
				list.Add((byte)j);
				num += array[j];
			}
			byte[] array2 = new byte[list.Count + 1];
			array2[0] = (byte)list.Count;
			for (int k = 0; k < list.Count; k++)
			{
				array2[k + 1] = list[k];
			}
			return array2;
		}

		private static byte[] SelectPlace(byte player, byte count, uint flag)
		{
			byte[] array = new byte[Math.Max(1, (int)count) * 3];
			HashSet<int> hashSet = new HashSet<int>();
			for (int i = 0; i < Math.Max(1, (int)count); i++)
			{
				byte b = 0;
				byte b2 = 4;
				byte b3 = 0;
				bool flag2 = false;
				for (int j = 0; j < 2; j++)
				{
					if (flag2)
					{
						break;
					}
					for (int k = 0; k < 2; k++)
					{
						if (flag2)
						{
							break;
						}
						for (int l = 0; l < 7; l++)
						{
							if (flag2)
							{
								break;
							}
							int num = l + ((j != player) ? 16 : 0) + ((k != 0) ? 8 : 0);
							if ((flag & (uint)(1 << num)) != 0 && !hashSet.Contains(num))
							{
								b = (byte)j;
								b2 = (byte)((k == 0) ? 4u : 8u);
								b3 = (byte)l;
								hashSet.Add(num);
								flag2 = true;
							}
						}
					}
				}
				int num2 = i * 3;
				array[num2] = b;
				array[num2 + 1] = b2;
				array[num2 + 2] = b3;
			}
			return array;
		}

		private void AutoAnswer(byte[] buffer, int length)
		{
			using MemoryStream memoryStream = new MemoryStream(buffer, 0, length, writable: false);
			using BinaryReader binaryReader = new BinaryReader(memoryStream);
			byte b = (_autoAnswerFunction = binaryReader.ReadByte());
			switch (b)
			{
			case 10:
				if (memoryStream.Position + 2 <= memoryStream.Length)
				{
					binaryReader.ReadByte();
					byte b10 = binaryReader.ReadByte();
					binaryReader.ReadBytes(11 * b10);
					byte b11 = binaryReader.ReadByte();
					binaryReader.ReadBytes(8 * b11);
					byte b12 = binaryReader.ReadByte();
					binaryReader.ReadByte();
					SendInt((b11 > 0) ? 1 : ((b12 != 0) ? 2 : 3));
				}
				break;
			case 11:
				if (memoryStream.Position < memoryStream.Length)
				{
					binaryReader.ReadByte();
					for (int num11 = 0; num11 < 5; num11++)
					{
						byte b20 = binaryReader.ReadByte();
						binaryReader.ReadBytes(7 * b20);
					}
					byte b21 = binaryReader.ReadByte();
					binaryReader.ReadBytes(11 * b21);
					byte b22 = binaryReader.ReadByte();
					binaryReader.ReadByte();
					if (memoryStream.Position < memoryStream.Length)
					{
						binaryReader.ReadByte();
					}
					SendInt((b22 != 0) ? 6 : 7);
				}
				break;
			case 12:
				if (memoryStream.Position + 13 <= memoryStream.Length)
				{
					binaryReader.ReadByte();
					binaryReader.ReadUInt32();
					binaryReader.ReadUInt32();
					binaryReader.ReadUInt32();
					SendInt(0);
				}
				break;
			case 13:
				if (memoryStream.Position + 5 <= memoryStream.Length)
				{
					binaryReader.ReadByte();
					binaryReader.ReadUInt32();
					SendInt(0);
				}
				break;
			case 140:
			case 141:
			{
				if (memoryStream.Position + 6 > memoryStream.Length)
				{
					break;
				}
				binaryReader.ReadByte();
				byte b15 = binaryReader.ReadByte();
				int num4 = binaryReader.ReadInt32();
				int num5 = 0;
				for (int k = 0; k < 32; k++)
				{
					if (b15 <= 0)
					{
						break;
					}
					int num6 = 1 << k;
					if ((num4 & num6) != 0)
					{
						num5 |= num6;
						b15--;
					}
				}
				SendInt(num5);
				break;
			}
			case 142:
			{
				if (memoryStream.Position + 2 > memoryStream.Length)
				{
					break;
				}
				binaryReader.ReadByte();
				byte b14 = binaryReader.ReadByte();
				int num2 = 0;
				for (int j = 0; j < b14; j++)
				{
					if (memoryStream.Position + 4 > memoryStream.Length)
					{
						break;
					}
					int num3 = binaryReader.ReadInt32();
					if (num2 == 0 && num3 > 0 && num3 < 100000000 && _cards.ContainsKey((uint)num3))
					{
						num2 = num3;
					}
				}
				SendInt((num2 != 0) ? num2 : 14558127);
				break;
			}
			case 143:
				if (memoryStream.Position + 2 <= memoryStream.Length)
				{
					binaryReader.ReadByte();
					byte b9 = binaryReader.ReadByte();
					binaryReader.ReadBytes(4 * b9);
					SendInt(0);
				}
				break;
			case 14:
				if (memoryStream.Position + 2 <= memoryStream.Length)
				{
					binaryReader.ReadByte();
					byte b6 = binaryReader.ReadByte();
					binaryReader.ReadBytes(4 * b6);
					SendInt(0);
				}
				break;
			case 15:
				if (memoryStream.Position + 5 <= memoryStream.Length)
				{
					binaryReader.ReadByte();
					binaryReader.ReadByte();
					byte min = binaryReader.ReadByte();
					byte max3 = binaryReader.ReadByte();
					byte b18 = binaryReader.ReadByte();
					binaryReader.ReadBytes(8 * b18);
					SendBytes(SelectFirst(b18, min, max3));
				}
				break;
			case 16:
				if (memoryStream.Position + 11 <= memoryStream.Length)
				{
					binaryReader.ReadByte();
					byte b23 = binaryReader.ReadByte();
					binaryReader.ReadByte();
					binaryReader.ReadUInt32();
					binaryReader.ReadUInt32();
					// Whether a chain is forced is told per chain since ocgcore 2025; the first forced one is activated.
					int forcedIndex = -1;
					for (int num12 = 0; num12 < b23; num12++)
					{
						byte[] entry = binaryReader.ReadBytes(14);
						if (forcedIndex < 0 && entry.Length == 14 && entry[1] != 0)
						{
							forcedIndex = num12;
						}
					}
					SendInt(forcedIndex);
				}
				break;
			case 18:
			case 24:
				if (memoryStream.Position + 6 <= memoryStream.Length)
				{
					byte player = binaryReader.ReadByte();
					byte count2 = binaryReader.ReadByte();
					uint flag = binaryReader.ReadUInt32();
					SendBytes(SelectPlace(player, count2, flag));
				}
				break;
			case 19:
				if (memoryStream.Position + 6 <= memoryStream.Length)
				{
					binaryReader.ReadByte();
					binaryReader.ReadUInt32();
					byte mask = binaryReader.ReadByte();
					SendInt(FirstPosition(mask));
				}
				break;
			case 20:
				if (memoryStream.Position + 5 <= memoryStream.Length)
				{
					binaryReader.ReadByte();
					byte b7 = binaryReader.ReadByte();
					byte b8 = binaryReader.ReadByte();
					byte max2 = binaryReader.ReadByte();
					byte count = binaryReader.ReadByte();
					byte[] array = SelectTribute(binaryReader, count, b8, max2);
					if (array[0] < b8 && b7 != 0)
					{
						SendInt(-1);
					}
					else
					{
						SendBytes(array);
					}
				}
				break;
			case 22:
			{
				if (memoryStream.Position + 6 > memoryStream.Length)
				{
					break;
				}
				binaryReader.ReadByte();
				binaryReader.ReadUInt16();
				ushort num8 = binaryReader.ReadUInt16();
				byte b19 = binaryReader.ReadByte();
				ushort[] array4 = new ushort[b19];
				for (int m = 0; m < b19; m++)
				{
					binaryReader.ReadUInt32();
					binaryReader.ReadByte();
					binaryReader.ReadByte();
					binaryReader.ReadByte();
					array4[m] = binaryReader.ReadUInt16();
				}
				byte[] array5 = new byte[b19 * 2];
				ushort num9 = num8;
				for (int n = 0; n < b19; n++)
				{
					if (num9 <= 0)
					{
						break;
					}
					ushort num10 = Math.Min(array4[n], num9);
					array5[2 * n] = (byte)num10;
					array5[2 * n + 1] = (byte)(num10 >> 8);
					num9 -= num10;
				}
				SendBytes(array5);
				break;
			}
			case 23:
				if (memoryStream.Position + 8 <= memoryStream.Length)
				{
					binaryReader.ReadByte();
					binaryReader.ReadByte();
					binaryReader.ReadUInt32();
					byte val = binaryReader.ReadByte();
					binaryReader.ReadByte();
					byte b16 = binaryReader.ReadByte();
					binaryReader.ReadBytes(11 * b16);
					byte b17 = binaryReader.ReadByte();
					binaryReader.ReadBytes(11 * b17);
					int num7 = Math.Min(255, Math.Max((int)b16, (int)val));
					byte[] array3 = new byte[num7 + 1];
					array3[0] = (byte)num7;
					for (int l = 0; l < num7; l++)
					{
						array3[l + 1] = (byte)((l >= b16) ? ((uint)(l - b16)) : 0u);
					}
					SendBytes(array3);
				}
				break;
			case 25:
				if (memoryStream.Position + 2 <= memoryStream.Length)
				{
					binaryReader.ReadByte();
					byte b13 = binaryReader.ReadByte();
					binaryReader.ReadBytes(7 * b13);
					byte[] array2 = new byte[b13 + 1];
					array2[0] = b13;
					for (int i = 0; i < b13; i++)
					{
						array2[i + 1] = (byte)i;
					}
					SendBytes(array2);
				}
				break;
			case 26:
				if (memoryStream.Position + 5 <= memoryStream.Length)
				{
					binaryReader.ReadByte();
					byte b2 = binaryReader.ReadByte();
					byte num = binaryReader.ReadByte();
					byte b3 = binaryReader.ReadByte();
					byte max = binaryReader.ReadByte();
					byte b4 = binaryReader.ReadByte();
					binaryReader.ReadBytes(8 * b4);
					byte b5 = binaryReader.ReadByte();
					binaryReader.ReadBytes(8 * b5);
					if ((num != 0 || b2 != 0) && b3 == 0)
					{
						SendInt(-1);
					}
					else
					{
						SendBytes(SelectFirst(b4, b3, max));
					}
				}
				break;
			default:
				SendInt(0);
				break;
			}
			Emit(new
			{
				type = "state",
				waiting = false,
				auto = true,
				function = b
			});
		}

		private static Dictionary<string, object?> ReadSumEntry(BinaryReader reader, int index, bool must)
		{
			uint num = reader.ReadUInt32();
			byte b = reader.ReadByte();
			byte b2 = reader.ReadByte();
			byte b3 = reader.ReadByte();
			int num2 = reader.ReadInt32();
			int num3 = num2 & 0xFFFF;
			int num4 = num2 >> 16;
			if (num4 == 0)
			{
				num4 = num3;
			}
			return new Dictionary<string, object>
			{
				["index"] = index,
				["code"] = num,
				["controller"] = (int)b,
				["location"] = (int)b2,
				["sequence"] = (int)b3,
				["level1"] = num3,
				["level2"] = num4,
				["must"] = must
			};
		}

		private static Dictionary<string, object?> ReadCardEntry(BinaryReader reader, int size, int index = -1)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (size >= 4)
			{
				dictionary["code"] = reader.ReadUInt32();
			}
			if (size >= 5)
			{
				dictionary["controller"] = reader.ReadByte();
			}
			if (size >= 6)
			{
				dictionary["location"] = reader.ReadByte();
			}
			if (size >= 7)
			{
				dictionary["sequence"] = reader.ReadByte();
			}
			if (size == 11)
			{
				uint num = reader.ReadUInt32();
				dictionary["description"] = num;
				dictionary["effectText"] = ((num != 0) ? DescribeOption(num, index) : "");
			}
			else
			{
				if (size >= 8)
				{
					dictionary["position"] = reader.ReadByte();
				}
				for (int i = 8; i < size; i++)
				{
					reader.ReadByte();
				}
			}
			if (index >= 0)
			{
				dictionary["index"] = index;
			}
			return dictionary;
		}

		private static List<object> BuildPlaces(byte player, byte count, uint flag)
		{
			List<object> list = new List<object>();
			flag = ~flag;
			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					for (int k = 0; k < 7; k++)
					{
						int num = k + ((i != player) ? 16 : 0) + ((j != 0) ? 8 : 0);
						if ((flag & (uint)(1 << num)) != 0)
						{
							list.Add(new Dictionary<string, object>
							{
								["player"] = i,
								["location"] = ((j == 0) ? 4 : 8),
								["sequence"] = k
							});
						}
					}
				}
			}
			return list;
		}

		private static List<object> BuildPositions(byte mask)
		{
			List<object> list = new List<object>();
			if ((mask & 1) != 0)
			{
				list.Add(new Dictionary<string, object>
				{
					["index"] = 1,
					["label"] = "Face-up Attack"
				});
			}
			if ((mask & 2) != 0)
			{
				list.Add(new Dictionary<string, object>
				{
					["index"] = 2,
					["label"] = "Face-down Attack"
				});
			}
			if ((mask & 4) != 0)
			{
				list.Add(new Dictionary<string, object>
				{
					["index"] = 4,
					["label"] = "Face-up Defense"
				});
			}
			if ((mask & 8) != 0)
			{
				list.Add(new Dictionary<string, object>
				{
					["index"] = 8,
					["label"] = "Face-down Defense"
				});
			}
			return list;
		}

		private static Dictionary<string, object?> BuildPrompt(byte[] buffer, int length)
		{
			using MemoryStream memoryStream = new MemoryStream(buffer, 0, length, writable: false);
			using BinaryReader binaryReader = new BinaryReader(memoryStream);
			byte b = binaryReader.ReadByte();
			Dictionary<string, object> dictionary = new Dictionary<string, object>
			{
				["type"] = "prompt",
				["function"] = b,
				["name"] = GameMessageName(b),
				["waiting"] = true,
				["raw"] = Convert.ToHexString(buffer.AsSpan(0, length)),
				["responseKind"] = "integer",
				["summary"] = $"{GameMessageName(b)} (function {b})"
			};
			try
			{
				switch (b)
				{
				case 10:
				{
					byte b39 = binaryReader.ReadByte();
					byte b40 = binaryReader.ReadByte();
					List<object> list17 = new List<object>();
					for (int num21 = 0; num21 < b40; num21++)
					{
						list17.Add(ReadCardEntry(binaryReader, 11, num21));
					}
					byte b41 = binaryReader.ReadByte();
					List<object> list18 = new List<object>();
					for (int num22 = 0; num22 < b41; num22++)
					{
						list18.Add(ReadCardEntry(binaryReader, 8, num22));
					}
					byte b42 = binaryReader.ReadByte();
					byte b43 = binaryReader.ReadByte();
					dictionary["player"] = b39;
					dictionary["toMain2"] = b42 != 0;
					dictionary["toEnd"] = b43 != 0;
					dictionary["summary"] = $"SelectBattleCmd: activate={b40}, attack={b41}, main2={((b42 != 0) ? "yes" : "no")}, end={((b43 != 0) ? "yes" : "no")}";
					List<object> list19 = new List<object>();
					if (b42 != 0)
					{
						list19.Add(new Dictionary<string, object>
						{
							["index"] = 2,
							["label"] = "Main Phase 2"
						});
					}
					if (b43 != 0)
					{
						list19.Add(new Dictionary<string, object>
						{
							["index"] = 3,
							["label"] = "End Phase"
						});
					}
					dictionary["options"] = list19;
					dictionary["activateCards"] = list17;
					dictionary["attackCards"] = list18;
					break;
				}
				case 11:
				{
					byte b23 = binaryReader.ReadByte();
					byte b24 = binaryReader.ReadByte();
					List<object> list7 = new List<object>();
					for (int num12 = 0; num12 < b24; num12++)
					{
						list7.Add(ReadCardEntry(binaryReader, 7, num12));
					}
					byte b25 = binaryReader.ReadByte();
					List<object> list8 = new List<object>();
					for (int num13 = 0; num13 < b25; num13++)
					{
						list8.Add(ReadCardEntry(binaryReader, 7, num13));
					}
					byte b26 = binaryReader.ReadByte();
					List<object> list9 = new List<object>();
					for (int num14 = 0; num14 < b26; num14++)
					{
						list9.Add(ReadCardEntry(binaryReader, 7, num14));
					}
					byte b27 = binaryReader.ReadByte();
					List<object> list10 = new List<object>();
					for (int num15 = 0; num15 < b27; num15++)
					{
						list10.Add(ReadCardEntry(binaryReader, 7, num15));
					}
					byte b28 = binaryReader.ReadByte();
					List<object> list11 = new List<object>();
					for (int num16 = 0; num16 < b28; num16++)
					{
						list11.Add(ReadCardEntry(binaryReader, 7, num16));
					}
					byte b29 = binaryReader.ReadByte();
					List<object> list12 = new List<object>();
					for (int num17 = 0; num17 < b29; num17++)
					{
						list12.Add(ReadCardEntry(binaryReader, 11, num17));
					}
					byte b30 = binaryReader.ReadByte();
					byte b31 = binaryReader.ReadByte();
					if (memoryStream.Position < memoryStream.Length)
					{
						binaryReader.ReadByte();
					}
					dictionary["player"] = b23;
					dictionary["toBattle"] = b30 != 0;
					dictionary["toEnd"] = b31 != 0;
					dictionary["summonCards"] = list7;
					dictionary["spsummonCards"] = list8;
					dictionary["positionCards"] = list9;
					dictionary["monsterSetCards"] = list10;
					dictionary["spellSetCards"] = list11;
					dictionary["activateCards"] = list12;
					dictionary["summary"] = $"SelectIdleCmd: summon={b24}, spsummon={b25}, position={b26}, mset={b27}, sset={b28}, activate={b29}, battle={((b30 != 0) ? "yes" : "no")}, end={((b31 != 0) ? "yes" : "no")}";
					List<object> list13 = new List<object>();
					if (b30 != 0)
					{
						list13.Add(new Dictionary<string, object>
						{
							["index"] = 6,
							["label"] = "Battle Phase"
						});
					}
					if (b31 != 0)
					{
						list13.Add(new Dictionary<string, object>
						{
							["index"] = 7,
							["label"] = "End Phase"
						});
					}
					dictionary["options"] = list13;
					break;
				}
				case 12:
				{
					byte b4 = binaryReader.ReadByte();
					uint num = binaryReader.ReadUInt32();
					uint num2 = binaryReader.ReadUInt32();
					uint num3 = binaryReader.ReadUInt32();
					int num4 = (int)(num2 & 0xFF);
					int num5 = (int)((num2 >> 8) & 0xFF);
					int num6 = (int)((num2 >> 16) & 0xFF);
					dictionary["player"] = b4;
					dictionary["code"] = num;
					dictionary["controller"] = num4;
					dictionary["location"] = num5;
					dictionary["sequence"] = num6;
					dictionary["description"] = num3;
					dictionary["summary"] = $"SelectEffectYN: card={num}, controller={num4}, location={num5}, sequence={num6}";
					dictionary["options"] = new List<object>
					{
						new Dictionary<string, object>
						{
							["index"] = 0,
							["label"] = "No"
						},
						new Dictionary<string, object>
						{
							["index"] = 1,
							["label"] = "Yes"
						}
					};
					break;
				}
				case 13:
				{
					byte b20 = binaryReader.ReadByte();
					uint num11 = binaryReader.ReadUInt32();
					dictionary["player"] = b20;
					dictionary["description"] = num11;
					dictionary["summary"] = $"SelectYesNo: description={num11}";
					dictionary["options"] = new List<object>
					{
						new Dictionary<string, object>
						{
							["index"] = 0,
							["label"] = "No"
						},
						new Dictionary<string, object>
						{
							["index"] = 1,
							["label"] = "Yes"
						}
					};
					break;
				}
				case 14:
				{
					byte b5 = binaryReader.ReadByte();
					byte b6 = binaryReader.ReadByte();
					List<object> list2 = new List<object>();
					for (int j = 0; j < b6; j++)
					{
						uint num7 = binaryReader.ReadUInt32();
						list2.Add(new Dictionary<string, object>
						{
							["index"] = j,
							["description"] = num7,
							["label"] = DescribeOption(num7, j)
						});
					}
					dictionary["player"] = b5;
					dictionary["summary"] = $"SelectOption: {b6} options";
					dictionary["options"] = list2;
					break;
				}
				case 15:
				{
					byte b55 = binaryReader.ReadByte();
					byte b56 = binaryReader.ReadByte();
					byte b57 = binaryReader.ReadByte();
					byte b58 = binaryReader.ReadByte();
					byte b59 = binaryReader.ReadByte();
					List<object> list24 = new List<object>();
					for (int num32 = 0; num32 < b59; num32++)
					{
						list24.Add(ReadCardEntry(binaryReader, 8, num32));
					}
					dictionary["player"] = b55;
					dictionary["cancelable"] = b56 != 0;
					dictionary["min"] = b57;
					dictionary["max"] = b58;
					dictionary["cards"] = list24;
					dictionary["summary"] = $"SelectCard: choose {b57}..{b58} from {b59} cards";
					dictionary["responseKind"] = "bytes";
					break;
				}
				case 16:
				{
					byte b44 = binaryReader.ReadByte();
					byte b45 = binaryReader.ReadByte();
					byte b46 = binaryReader.ReadByte();
					bool flag3 = false;
					uint num23 = binaryReader.ReadUInt32();
					uint num24 = binaryReader.ReadUInt32();
					List<object> list20 = new List<object>();
					for (int num25 = 0; num25 < b45; num25++)
					{
						if (memoryStream.Length - memoryStream.Position < 14)
						{
							break;
						}
						byte b47 = binaryReader.ReadByte();
						bool entryForced = binaryReader.ReadByte() != 0;
						flag3 |= entryForced;
						uint num26 = binaryReader.ReadUInt32();
						uint num27 = binaryReader.ReadUInt32();
						uint num28 = binaryReader.ReadUInt32();
						Dictionary<string, object> item2 = new Dictionary<string, object>
						{
							["index"] = num25,
							["flag"] = b47,
							["forced"] = entryForced,
							["code"] = num26,
							["controller"] = (int)(num27 & 0xFF),
							["location"] = (int)((num27 >> 8) & 0xFF),
							["sequence"] = (int)((num27 >> 16) & 0xFF),
							["position"] = (int)((num27 >> 24) & 0xFF),
							["description"] = num28,
							["effectText"] = ((num28 != 0) ? DescribeOption(num28, num25) : "")
						};
						list20.Add(item2);
					}
					dictionary["player"] = b44;
					dictionary["count"] = b45;
					dictionary["special"] = b46;
					dictionary["forced"] = flag3;
					dictionary["cards"] = list20;
					dictionary["hints"] = new List<object> { num23, num24 };
					dictionary["summary"] = $"SelectChain: count={b45}, special={b46}, forced={(flag3 ? "yes" : "no")}, cards={list20.Count}";
					List<object> list21 = new List<object>
					{
						new Dictionary<string, object>
						{
							["index"] = -1,
							["label"] = "Pass"
						}
					};
					for (int num29 = 0; num29 < list20.Count; num29++)
					{
						list21.Add(new Dictionary<string, object>
						{
							["index"] = num29,
							["label"] = "Chain #" + (num29 + 1)
						});
					}
					dictionary["options"] = list21;
					break;
				}
				case 18:
				case 24:
				{
					byte b37 = binaryReader.ReadByte();
					byte b38 = binaryReader.ReadByte();
					uint flag2 = binaryReader.ReadUInt32();
					List<object> list16 = BuildPlaces(b37, b38, flag2);
					dictionary["player"] = b37;
					dictionary["count"] = b38;
					dictionary["places"] = list16;
					dictionary["summary"] = $"SelectPlace: choose {b38} zones ({list16.Count} available)";
					dictionary["responseKind"] = "bytes";
					break;
				}
				case 19:
				{
					byte b21 = binaryReader.ReadByte();
					uint value = binaryReader.ReadUInt32();
					byte b22 = binaryReader.ReadByte();
					dictionary["player"] = b21;
					dictionary["summary"] = $"SelectPosition: card={value}, mask=0x{b22:X2}";
					dictionary["options"] = BuildPositions(b22);
					break;
				}
				case 20:
				{
					byte b15 = binaryReader.ReadByte();
					byte b16 = binaryReader.ReadByte();
					byte b17 = binaryReader.ReadByte();
					byte b18 = binaryReader.ReadByte();
					byte b19 = binaryReader.ReadByte();
					List<object> list6 = new List<object>();
					for (int n = 0; n < b19; n++)
					{
						Dictionary<string, object> item = new Dictionary<string, object>
						{
							["index"] = n,
							["code"] = binaryReader.ReadUInt32(),
							["controller"] = binaryReader.ReadByte(),
							["location"] = binaryReader.ReadByte(),
							["sequence"] = binaryReader.ReadByte(),
							["releases"] = binaryReader.ReadByte()
						};
						list6.Add(item);
					}
					dictionary["player"] = b15;
					dictionary["cancelable"] = b16 != 0;
					dictionary["min"] = b17;
					dictionary["max"] = b18;
					dictionary["cards"] = list6;
					dictionary["summary"] = $"SelectTribute: choose {b17}..{b18} from {b19} cards";
					dictionary["responseKind"] = "bytes_or_integer_cancel";
					break;
				}
				case 22:
				{
					byte b9 = binaryReader.ReadByte();
					binaryReader.ReadUInt16();
					ushort num8 = binaryReader.ReadUInt16();
					byte b10 = binaryReader.ReadByte();
					binaryReader.ReadBytes(9 * b10);
					dictionary["player"] = b9;
					dictionary["quantity"] = num8;
					dictionary["count"] = b10;
					dictionary["summary"] = $"SelectCounter: distribute {num8} among {b10} counters";
					dictionary["responseKind"] = "bytes";
					break;
				}
				case 23:
				{
					bool flag = binaryReader.ReadByte() != 0;
					byte b32 = binaryReader.ReadByte();
					int num18 = binaryReader.ReadInt32();
					byte b33 = binaryReader.ReadByte();
					byte b34 = binaryReader.ReadByte();
					if (b33 < 1)
					{
						b33 = 1;
					}
					if (b34 < 1)
					{
						b34 = 99;
					}
					byte b35 = binaryReader.ReadByte();
					List<object> list14 = new List<object>();
					for (int num19 = 0; num19 < b35; num19++)
					{
						list14.Add(ReadSumEntry(binaryReader, num19, must: true));
					}
					byte b36 = binaryReader.ReadByte();
					List<object> list15 = new List<object>();
					for (int num20 = 0; num20 < b36; num20++)
					{
						list15.Add(ReadSumEntry(binaryReader, num20, must: false));
					}
					dictionary["player"] = b32;
					dictionary["overFlow"] = flag;
					dictionary["sum"] = num18;
					dictionary["min"] = b33;
					dictionary["max"] = b34;
					dictionary["must"] = b35;
					dictionary["mustCards"] = list14;
					dictionary["count"] = b36;
					dictionary["cards"] = list15;
					dictionary["summary"] = $"SelectSum: sum={num18}, must={b35}, min={b33}, max={b34}, cards={b36}";
					dictionary["responseKind"] = "bytes";
					break;
				}
				case 25:
				{
					byte b11 = binaryReader.ReadByte();
					byte b12 = binaryReader.ReadByte();
					List<object> list4 = new List<object>();
					for (int l = 0; l < b12; l++)
					{
						list4.Add(ReadCardEntry(binaryReader, 7, l));
					}
					dictionary["player"] = b11;
					dictionary["count"] = b12;
					dictionary["cards"] = list4;
					dictionary["summary"] = $"SortCard: {b12} cards";
					dictionary["responseKind"] = "bytes";
					break;
				}
				case 26:
				{
					byte b48 = binaryReader.ReadByte();
					byte b49 = binaryReader.ReadByte();
					byte b50 = binaryReader.ReadByte();
					byte b51 = binaryReader.ReadByte();
					byte b52 = binaryReader.ReadByte();
					byte b53 = binaryReader.ReadByte();
					List<object> list22 = new List<object>();
					for (int num30 = 0; num30 < b53; num30++)
					{
						list22.Add(ReadCardEntry(binaryReader, 8, num30));
					}
					byte b54 = binaryReader.ReadByte();
					List<object> list23 = new List<object>();
					for (int num31 = 0; num31 < b54; num31++)
					{
						list23.Add(ReadCardEntry(binaryReader, 8, num31));
					}
					dictionary["player"] = b48;
					dictionary["finish"] = b49 != 0;
					dictionary["cancelable"] = b50 != 0;
					dictionary["min"] = b51;
					dictionary["max"] = b52;
					dictionary["count"] = b53;
					dictionary["cards"] = list22;
					dictionary["unselectCards"] = list23;
					dictionary["responseKind"] = "bytes_or_integer_cancel";
					dictionary["summary"] = $"SelectUnselectCard: finish={b49 != 0}, cancel={b50 != 0}, min={b51}, max={b52}, cards={b53}";
					dictionary["responseKind"] = "bytes_or_integer_cancel";
					break;
				}
				case 140:
				case 141:
				{
					byte b13 = binaryReader.ReadByte();
					byte b14 = binaryReader.ReadByte();
					int num9 = binaryReader.ReadInt32();
					List<object> list5 = new List<object>();
					for (int m = 0; m < 32; m++)
					{
						int num10 = 1 << m;
						if ((num9 & num10) != 0)
						{
							list5.Add(num10);
						}
					}
					dictionary["player"] = b13;
					dictionary["kind"] = ((b == 140) ? "race" : "attrib");
					dictionary["pick"] = b14;
					dictionary["values"] = list5;
					dictionary["responseKind"] = "integer";
					dictionary["summary"] = $"{((b == 140) ? "AnnounceRace" : "AnnounceAttrib")}: 宣言 {b14} 个（可选 {list5.Count} 项）";
					break;
				}
				case 142:
				{
					byte b7 = binaryReader.ReadByte();
					byte b8 = binaryReader.ReadByte();
					List<object> list3 = new List<object>();
					for (int k = 0; k < b8; k++)
					{
						list3.Add(binaryReader.ReadInt32());
					}
					dictionary["player"] = b7;
					dictionary["kind"] = "card";
					dictionary["pick"] = 1;
					dictionary["opcodes"] = list3;
					dictionary["responseKind"] = "integer";
					dictionary["summary"] = $"AnnounceCard: 宣言一张卡（过滤条件 {b8} 条）";
					break;
				}
				case 143:
				{
					byte b2 = binaryReader.ReadByte();
					byte b3 = binaryReader.ReadByte();
					List<object> list = new List<object>();
					for (int i = 0; i < b3; i++)
					{
						list.Add(binaryReader.ReadInt32());
					}
					dictionary["player"] = b2;
					dictionary["kind"] = "number";
					dictionary["pick"] = 1;
					dictionary["values"] = list;
					dictionary["responseKind"] = "integer";
					dictionary["summary"] = $"AnnounceNumber: 从 {b3} 个数字里宣言一个";
					break;
				}
				default:
					dictionary["summary"] = $"Unhandled prompt function {b} ({GameMessageName(b)}), raw={dictionary["raw"]}";
					dictionary["responseKind"] = "integer_or_bytes";
					break;
				}
			}
			catch (Exception ex)
			{
				dictionary["summary"] = $"Prompt parse failed for function {b}: {ex.Message}";
				dictionary["responseKind"] = "integer_or_bytes";
			}
			return dictionary;
		}

		private static bool NeedsResponse(byte value)
		{
			if ((uint)(value - 10) <= 6u || (uint)(value - 18) <= 8u || (uint)(value - 140) <= 3u)
			{
				return true;
			}
			return false;
		}

		private static int ReadAskedPlayer(byte[] buffer, int length)
		{
			if (buffer[0] != 23 || length < 3)
			{
				if (length < 2)
				{
					return 0;
				}
				return buffer[1];
			}
			return buffer[2];
		}

		private static bool IsEmptyChainWindow(byte[] buffer, int length)
		{
			if (length < 3 || buffer[0] != 16)
			{
				return false;
			}
			return buffer[2] == 0;
		}

		private static bool IsForcedChainWindow(byte[] buffer, int length)
		{
			if (length >= 12 && buffer[0] == 16)
			{
				for (int i = 0; i < buffer[2] && 12 + i * 14 + 1 < length; i++)
				{
					if (buffer[12 + i * 14 + 1] != 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		private void ApplyAiResponse(byte[] args)
		{
			if (_duel == IntPtr.Zero || args.Length == 0)
			{
				return;
			}
			if (args.Length == 4)
			{
				Native.SetResponseInteger(_duel, BitConverter.ToUInt32(args, 0));
				return;
			}
			nint num = Marshal.AllocHGlobal(64);
			try
			{
				Marshal.Copy(args, 0, num, args.Length);
				Native.SetResponseBytes(_duel, num);
			}
			finally
			{
				Marshal.FreeHGlobal(num);
			}
		}

		private void EmitMessage(byte[] buffer, int length, uint result)
		{
			byte b = buffer[0];
			byte? winner = ((b == 5 && length > 1) ? new byte?(buffer[1]) : ((byte?)null));
			Emit(new
			{
				type = "message",
				function = b,
				name = GameMessageName(b),
				length = length,
				result = result,
				winner = winner,
				hex = Convert.ToHexString(buffer.AsSpan(0, length))
			});
		}

		private void AddDeck(string path, int player)
		{
			string path2 = (Path.IsPathRooted(path) ? path : Path.Combine(_root, path));
			int num = 0;
			List<int> list = new List<int>();
			List<int> list2 = new List<int>();
			foreach (string item in File.ReadLines(path2))
			{
				string text = item.Trim();
				switch (text)
				{
				case "#main":
					num = 1;
					continue;
				case "#extra":
					num = 2;
					continue;
				case "!side":
					num = 3;
					continue;
				}
				int result;
				bool flag = !int.TryParse(text, out result);
				if (!flag)
				{
					bool flag2 = ((num == 0 || num == 3) ? true : false);
					flag = flag2;
				}
				if (!flag)
				{
					if (num == 2)
					{
						list2.Add(result);
					}
					else
					{
						list.Add(result);
					}
				}
			}
			Random random = new Random((int)_seed ^ ((player + 1) * -1640531527));
			for (int num2 = list.Count - 1; num2 > 0; num2--)
			{
				int num3 = random.Next(num2 + 1);
				List<int> list3 = list;
				int index = num2;
				List<int> list4 = list;
				int index2 = num3;
				int value = list[num3];
				int value2 = list[num2];
				list3[index] = value;
				list4[index2] = value2;
			}
			WindBotAi.DeckCounts[(player & 1) * 2] = list.Count;
			WindBotAi.DeckCounts[(player & 1) * 2 + 1] = list2.Count;
			foreach (int item2 in list)
			{
				NewCard(item2, player, 1);
			}
			foreach (int item3 in list2)
			{
				NewCard(item3, player, 64);
			}
		}

		private void NewCard(int code, int player, byte location)
		{
			_creatingCard = (uint)code;
			try
			{
				Native.NewCard(_duel, (uint)code, (byte)player, (byte)player, location, 0, 8);
			}
			finally
			{
				_creatingCard = 0u;
			}
		}

		private void LoadCards()
		{
			string path = Path.Combine(_root, "Data", "locales", "zh-CN", "cards.cdb");
			if (!File.Exists(path))
			{
				path = Path.Combine(_root, "cards.cdb");
			}
			int num = Sqlite.Open(path, out var db);
			if (num != 0)
			{
				throw new InvalidOperationException($"cards.cdb open failed: {num}");
			}
			try
			{
				num = Sqlite.Prepare(db, "SELECT id,alias,setcode,type,level,attribute,race,atk,def FROM datas", out var statement);
				if (num != 0)
				{
					throw new InvalidOperationException($"cards.cdb query failed: {num}");
				}
				try
				{
					while (Sqlite.Step(statement) == 100)
					{
						_cards[(uint)Sqlite.Int(statement, 0)] = new CardInfo((uint)Sqlite.Int(statement, 0), (uint)Sqlite.Int(statement, 1), (ulong)Sqlite.Int64(statement, 2), (uint)Sqlite.Int(statement, 3), (uint)Sqlite.Int(statement, 4), (uint)Sqlite.Int(statement, 5), (uint)Sqlite.Int(statement, 6), Sqlite.Int(statement, 7), Sqlite.Int(statement, 8));
					}
				}
				finally
				{
					Sqlite.Finalize(statement);
				}
			}
			finally
			{
				Sqlite.Close(db);
			}
		}

		internal static void LoadOptionTexts(string root)
		{
			if (_optionTextsLoaded)
			{
				return;
			}
			_optionTextsLoaded = true;
			string path = Path.Combine(root, "Data", "locales", "zh-CN", "cards.cdb");
			if (!File.Exists(path))
			{
				path = Path.Combine(root, "cards.cdb");
			}
			if (File.Exists(path) && Sqlite.Open(path, out var db) == 0)
			{
				try
				{
					if (Sqlite.Prepare(db, "SELECT id,str1,str2,str3,str4,str5,str6,str7,str8,str9,str10,str11,str12,str13,str14,str15,str16 FROM texts", out var statement) == 0)
					{
						try
						{
							while (Sqlite.Step(statement) == 100)
							{
								string[] array = new string[16];
								for (int i = 0; i < array.Length; i++)
								{
									array[i] = Sqlite.Text(statement, i + 1) ?? "";
								}
								_cardTexts[(uint)Sqlite.Int(statement, 0)] = array;
							}
						}
						finally
						{
							Sqlite.Finalize(statement);
						}
					}
				}
				finally
				{
					Sqlite.Close(db);
				}
			}
			string path2 = Path.Combine(root, "Data", "locales", "zh-CN", "strings.conf");
			if (!File.Exists(path2))
			{
				path2 = Path.Combine(root, "strings.conf");
			}
			if (!File.Exists(path2))
			{
				return;
			}
			foreach (string item in File.ReadLines(path2, Encoding.UTF8))
			{
				if (!item.StartsWith("!system ", StringComparison.Ordinal))
				{
					continue;
				}
				string[] array2 = item.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
				if (array2.Length >= 3)
				{
					string text = array2[1];
					int result;
					int num = (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt32(text, 16) : (int.TryParse(text, out result) ? result : (-1)));
					if (num >= 0)
					{
						_systemStrings[num] = array2[2];
					}
				}
			}
		}

		internal static string DescribeOption(uint description, int index)
		{
			if (description != 0 && description < 10000)
			{
				if (_systemStrings.TryGetValue((int)description, out string value) && value.Length > 0)
				{
					return value;
				}
			}
			else if (description >= 10000)
			{
				uint key = description >> 4;
				int num = (int)(description & 0xF);
				if (_cardTexts.TryGetValue(key, out string[] value2) && num >= 0 && num < value2.Length && value2[num].Length > 0)
				{
					return value2[num];
				}
			}
			return "";
		}

		private void LoadScripts()
		{
			string text = Path.Combine(_root, "Data", "script.zip");
			if (!File.Exists(text))
			{
				text = Path.Combine(_root, "script.zip");
			}
			using ZipArchive zipArchive = ZipFile.OpenRead(text);
			foreach (ZipArchiveEntry item in zipArchive.Entries.Where((ZipArchiveEntry e) => e.FullName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase)))
			{
				using Stream stream = item.Open();
				using MemoryStream memoryStream = new MemoryStream();
				stream.CopyTo(memoryStream);
				byte[] array = memoryStream.ToArray();
				nint num = Marshal.AllocHGlobal(array.Length);
				Marshal.Copy(array, 0, num, array.Length);
				_scripts[Normalize(item.FullName)] = (num, array.Length);
			}
		}

		private static string Normalize(string name)
		{
			return name.Replace('\\', '/').TrimStart('.', '/');
		}

		private unsafe nint ReadScript(byte* name, int* length)
		{
			string text = Normalize(Marshal.PtrToStringUTF8((nint)name) ?? "");
			if (Environment.GetEnvironmentVariable("DUELHOST_LOG_SCRIPTS") == "1")
			{
				Console.Error.WriteLine($"SCRIPT-REQ {text} for c{_creatingCard}");
			}
			if (_creatingCard != 0 && _cards.TryGetValue(_creatingCard, out var value) && value.Alias != 0)
			{
				string value2 = $"script/c{value.Alias}.lua";
				if (text.Equals(value2, StringComparison.OrdinalIgnoreCase))
				{
					string text2 = $"script/c{_creatingCard}.lua";
					if (_scripts.TryGetValue(text2, out (nint, int) value3))
					{
						Console.Error.WriteLine("SCRIPT-OVERRIDE " + text + " -> " + text2);
						*length = value3.Item2;
						return value3.Item1;
					}
				}
			}
			if (!_scripts.TryGetValue(text, out (nint, int) value4))
			{
				if (_scriptMisses.Add(text))
				{
					Console.Error.WriteLine("SCRIPT-MISS " + text);
				}
				*length = 0;
				return IntPtr.Zero;
			}
			*length = value4.Item2;
			return value4.Item1;
		}

		private unsafe uint ReadCard(uint code, NativeCardData* output)
		{
			*output = default(NativeCardData);
			if (!_cards.TryGetValue(code, out var value))
			{
				return 0u;
			}
			output->Code = value.Id;
			output->Alias = value.Alias;
			output->Type = value.Type;
			output->Level = value.Level;
			output->Attribute = value.Attribute;
			output->Race = value.Race;
			bool flag = (value.Type & 0x4000000) != 0;
			output->Attack = value.Attack;
			output->Defense = ((!flag) ? value.Defense : 0);
			output->LinkMarker = (flag ? ((uint)value.Defense) : 0u);
			for (int i = 0; i < 4; i++)
			{
				output->Setcode[i] = (ushort)((value.Setcode >> 16 * i) & 0xFFFF);
			}
			return code;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			_response.Set();
			if (_duel != IntPtr.Zero)
			{
				Native.EndDuel(_duel);
			}
			foreach (var value in _scripts.Values)
			{
				Marshal.FreeHGlobal(value.Ptr);
			}
			Marshal.FreeHGlobal(_buffer);
		}
	}

	private readonly record struct CardInfo(uint Id, uint Alias, ulong Setcode, uint Type, uint Level, uint Attribute, uint Race, int Attack, int Defense);

	private struct NativeCardData
	{
		public uint Code;

		public uint Alias;

		public unsafe fixed ushort Setcode[16];

		public uint Type;

		public uint Level;

		public uint Attribute;

		public uint Race;

		public int Attack;

		public int Defense;

		public uint LScale;

		public uint RScale;

		public uint LinkMarker;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate nint ScriptReader(byte* name, int* length);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate uint CardReader(uint code, NativeCardData* data);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate uint MessageHandler(nint duel, uint messageType);

	private static class Sqlite
	{
		[DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern int sqlite3_open_v2([MarshalAs(UnmanagedType.LPUTF8Str)] string file, out nint db, int flags, nint vfs);

		[DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern int sqlite3_prepare_v2(nint db, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, int n, out nint statement, nint tail);

		[DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern int sqlite3_step(nint statement);

		[DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern int sqlite3_column_int(nint statement, int column);

		[DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern long sqlite3_column_int64(nint statement, int column);

		[DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern nint sqlite3_column_text(nint statement, int column);

		[DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern int sqlite3_finalize(nint statement);

		[DllImport("sqlite3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern int sqlite3_close(nint db);

		[DllImport("kernel32", SetLastError = true)]
		private static extern bool SetDllDirectory(string lpPathName);

		static Sqlite()
		{
			SetDllDirectory(_rootPath);
		}

		public static int Open(string path, out nint db)
		{
			return sqlite3_open_v2(path, out db, 1, IntPtr.Zero);
		}

		public static int Prepare(nint db, string sql, out nint statement)
		{
			return sqlite3_prepare_v2(db, sql, -1, out statement, IntPtr.Zero);
		}

		public static int Step(nint statement)
		{
			return sqlite3_step(statement);
		}

		public static int Int(nint statement, int col)
		{
			return sqlite3_column_int(statement, col);
		}

		public static long Int64(nint statement, int col)
		{
			return sqlite3_column_int64(statement, col);
		}

		public static int Finalize(nint statement)
		{
			return sqlite3_finalize(statement);
		}

		public static int Close(nint db)
		{
			return sqlite3_close(db);
		}

		public static string? Text(nint statement, int col)
		{
			nint num = sqlite3_column_text(statement, col);
			if (num != IntPtr.Zero)
			{
				return Marshal.PtrToStringUTF8(num);
			}
			return null;
		}
	}

	private static class Native
	{
		[DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
		private static extern void set_script_reader(ScriptReader callback);

		[DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
		private static extern void set_card_reader(CardReader callback);

		[DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
		private static extern void set_message_handler(MessageHandler callback);

		[DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
		private static extern nint create_duel(uint seed);

		[DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
		private static extern void start_duel(nint duel, int options);

		[DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
		private static extern void end_duel(nint duel);

		[DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
		private static extern void set_player_info(nint duel, int player, int lp, int start, int draw);

		[DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
		private static extern void new_card(nint duel, uint code, byte owner, byte player, byte location, byte sequence, byte position);

		[DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
		private static extern uint process(nint duel);

		[DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
		private static extern int get_message(nint duel, nint buffer);

		[DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
		private static extern int query_field_card(nint duel, int player, int location, int queryFlag, nint buffer, int useCache);

		[DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
		private static extern void set_responsei(nint duel, uint value);

		[DllImport("ocgcore", CallingConvention = CallingConvention.Cdecl)]
		private static extern void set_responseb(nint duel, nint buffer);

		public static void SetCallbacks(ScriptReader s, CardReader c, MessageHandler m)
		{
			set_script_reader(s);
			set_card_reader(c);
			set_message_handler(m);
		}

		public static int QueryField(nint duel, int player, int location, int flag, nint buffer, int cache)
		{
			return query_field_card(duel, player, location, flag, buffer, cache);
		}

		public static nint CreateDuel(uint s)
		{
			return create_duel(s);
		}

		public static void StartDuel(nint d, int o)
		{
			start_duel(d, o);
		}

		public static void EndDuel(nint d)
		{
			end_duel(d);
		}

		public static void SetPlayerInfo(nint d, int p, int lp, int st, int dr)
		{
			set_player_info(d, p, lp, st, dr);
		}

		public static void NewCard(nint d, uint c, byte o, byte p, byte l, byte s, byte pos)
		{
			new_card(d, c, o, p, l, s, pos);
		}

		public static uint Process(nint d)
		{
			return process(d);
		}

		public static void GetMessage(nint d, nint b)
		{
			get_message(d, b);
		}

		public static void SetResponseInteger(nint d, uint v)
		{
			set_responsei(d, v);
		}

		public static void SetResponseBytes(nint d, nint b)
		{
			set_responseb(d, b);
		}
	}

	private const string DefaultRoot = "D:\\MDProMod\\tools\\ygo_tcp_probe\\staging";

	private static readonly object Sync = new object();

	private static string _rootPath = "D:\\MDProMod\\tools\\ygo_tcp_probe\\staging";

	private static DuelCore? _duel;

	private static CancellationTokenSource? _cts;

	private static Task? _runTask;

	public static int Main(string[] args)
	{
		if (args.Contains("--test-splitter"))
		{
			int verified = ClassicMessageSplitter.SelfTest(GetOption(args, "--test-jsonl"));
			Emit(new
			{
				type = "splitterTest",
				passed = true,
				verified = verified
			});
			return 0;
		}
		if (args.Contains("--timing-selftest"))
		{
			return RunTimingSelfTest();
		}
		int num = Array.IndexOf(args, "--describe-option");
		if (num >= 0)
		{
			DuelCore.LoadOptionTexts(GetOption(args, "--root") ?? "D:\\MDProMod\\tools\\ygo_tcp_probe\\staging");
			Console.WriteLine($"文字表：卡片 {DuelCore.CardTextCount} 张，system {DuelCore.SystemStringCount} 条");
			for (int i = num + 1; i < args.Length && !args[i].StartsWith("--"); i++)
			{
				if (uint.TryParse(args[i], out var result))
				{
					Console.WriteLine($"{result} -> {DuelCore.DescribeOption(result, 0)}");
				}
			}
			return 0;
		}
		string root = (_rootPath = GetOption(args, "--root") ?? "D:\\MDProMod\\tools\\ygo_tcp_probe\\staging");
		string option = GetOption(args, "--smoke-ai");
		uint result2;
		if (option != null)
		{
			return RunAiSmoke(root, option, GetOption(args, "--smoke-ai-name") ?? "AI_BlueEyes", GetOption(args, "--smoke-ai-dialog") ?? "cirno.zh-CN", uint.TryParse(GetOption(args, "--smoke-seed"), out result2) ? result2 : 305419896u, GetOption(args, "--smoke-ai-deck0") ?? "Data/Windbot/Decks/AI_Albaz.ydk", GetOption(args, "--smoke-ai-name0") ?? "AI_Albaz");
		}
		Emit(new
		{
			type = "ready",
			root = root
		});
		using StreamReader streamReader = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		string text;
		while ((text = streamReader.ReadLine()) != null)
		{
			string text2 = text;
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			try
			{
				using JsonDocument jsonDocument = JsonDocument.Parse(text2);
				Handle(jsonDocument.RootElement, root);
			}
			catch (Exception ex)
			{
				Emit(new
				{
					type = "error",
					message = ex.Message
				});
			}
		}
		Stop();
		return 0;
	}

	private static int RunAiSmoke(string root, string aiDeckFile, string aiDeckName, string aiDialog, uint seed, string deck0File, string deck0Name)
	{
		Emit(new
		{
			type = "ready",
			root = root,
			smokeAi = aiDeckFile,
			deck0 = deck0File,
			seed = seed
		});
		WindBotOptions windBotOptions = new WindBotOptions(1, aiDeckName, aiDeckName, aiDeckFile, aiDialog);
		WindBotOptions windBotOptions2 = new WindBotOptions(0, "MDProBotA", deck0Name, deck0File, aiDialog);
		using DuelCore duelCore = new DuelCore(root, deck0File, aiDeckFile, auto: true, seed, new WindBotOptions[2] { windBotOptions, windBotOptions2 });
		using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(3.0));
		duelCore.Run(cancellationTokenSource.Token);
		return 0;
	}

	private static void Handle(JsonElement command, string root)
	{
		JsonElement value;
		string text = (command.TryGetProperty("cmd", out value) ? value.GetString() : null);
		switch (text)
		{
		case "start":
		{
			Stop();
			JsonElement value4;
			string deck = (command.TryGetProperty("deck0", out value4) ? value4.GetString() : null);
			JsonElement value5;
			string deck2 = (command.TryGetProperty("deck1", out value5) ? value5.GetString() : null);
			JsonElement value6;
			bool auto = command.TryGetProperty("auto", out value6) && value6.ValueKind == JsonValueKind.True;
			JsonElement value7;
			bool autoPassChain = command.TryGetProperty("autoPassChain", out value7) && value7.ValueKind == JsonValueKind.True;
			JsonElement value8;
			uint seed = ((command.TryGetProperty("seed", out value8) && value8.ValueKind == JsonValueKind.Number) ? value8.GetUInt32() : ((uint)Environment.TickCount));
			WindBotOptions windBotOptions = null;
			if (command.TryGetProperty("ai", out var value9) && value9.ValueKind == JsonValueKind.True)
			{
				JsonElement value10;
				int seat2 = ((!command.TryGetProperty("aiSeat", out value10) || value10.ValueKind != JsonValueKind.Number) ? 1 : value10.GetInt32());
				JsonElement value11;
				string text2 = (command.TryGetProperty("aiName", out value11) ? value11.GetString() : "MDProBot");
				JsonElement value12;
				string text3 = (command.TryGetProperty("aiDeck", out value12) ? value12.GetString() : null);
				JsonElement value13;
				string text4 = (command.TryGetProperty("aiDeckFile", out value13) ? value13.GetString() : null);
				windBotOptions = new WindBotOptions(Dialog: (command.TryGetProperty("aiDialog", out var value14) ? value14.GetString() : "default") ?? "default", Seat: seat2, Name: text2 ?? "MDProBot", Deck: text3 ?? "", DeckFile: text4 ?? "");
			}
			WindBotOptions[] aiOptions = ((windBotOptions == null) ? Array.Empty<WindBotOptions>() : new WindBotOptions[1] { windBotOptions });
			_duel = new DuelCore(root, deck, deck2, auto, seed, aiOptions, autoPassChain);
			JsonElement value15;
			string timing2 = (command.TryGetProperty("timing", out value15) ? value15.GetString() : null);
			_duel.SetTiming(timing2);
			Emit(new
			{
				type = "started",
				deck0 = deck,
				deck1 = deck2
			});
			_cts = new CancellationTokenSource();
			DuelCore duel = _duel;
			CancellationTokenSource cts = _cts;
			_runTask = Task.Run(delegate
			{
				duel.Run(cts.Token);
			}, cts.Token);
			break;
		}
		case "response":
			_duel?.SetInteger((uint)command.GetProperty("value").GetInt32());
			break;
		case "responseBytes":
			_duel?.SetBytes(Convert.FromHexString(command.GetProperty("hex").GetString() ?? ""));
			break;
		case "stop":
			Stop();
			break;
		case "timing":
		{
			JsonElement value3;
			string timing = (command.TryGetProperty("mode", out value3) ? value3.GetString() : "default");
			_duel?.SetTiming(timing);
			break;
		}
		case "surrender":
		{
			JsonElement value2;
			int seat = ((command.TryGetProperty("player", out value2) && value2.ValueKind == JsonValueKind.Number) ? value2.GetInt32() : 0);
			_duel?.Surrender(seat);
			break;
		}
		default:
			Emit(new
			{
				type = "error",
				message = "unknown cmd: " + text
			});
			break;
		}
	}

	private static void Stop()
	{
		CancellationTokenSource cts = _cts;
		DuelCore duel = _duel;
		Task runTask = _runTask;
		_cts = null;
		_duel = null;
		_runTask = null;
		try
		{
			cts?.Cancel();
		}
		catch
		{
		}
		duel?.CancelWait();
		try
		{
			runTask?.Wait(2000);
		}
		catch
		{
		}
		duel?.Dispose();
		cts?.Dispose();
	}

	private static void Emit(object value)
	{
		lock (Sync)
		{
			Console.WriteLine(JsonSerializer.Serialize(value));
			Console.Out.Flush();
		}
	}

	private static string? GetOption(string[] args, string key)
	{
		int num = Array.IndexOf(args, key);
		if (num < 0 || num + 1 >= args.Length)
		{
			return null;
		}
		return args[num + 1];
	}

	private static int RunTimingSelfTest()
	{
		int assertions = 0;
		Pass(16, emptyChain: true, forcedChain: false, 1, autoPassChain: false, "empty-chain");
		Pass(16, emptyChain: false, forcedChain: false, 1, autoPassChain: false, "");
		Pass(12, emptyChain: false, forcedChain: false, 1, autoPassChain: false, "");
		Pass(16, emptyChain: true, forcedChain: false, 0, autoPassChain: false, "");
		Pass(16, emptyChain: false, forcedChain: false, 0, autoPassChain: false, "");
		Pass(12, emptyChain: false, forcedChain: false, 0, autoPassChain: false, "");
		Pass(16, emptyChain: true, forcedChain: false, 2, autoPassChain: false, "timing-off");
		Pass(16, emptyChain: false, forcedChain: false, 2, autoPassChain: false, "timing-off");
		Pass(12, emptyChain: false, forcedChain: false, 2, autoPassChain: false, "timing-off-effect");
		Pass(16, emptyChain: false, forcedChain: true, 2, autoPassChain: false, "");
		Pass(12, emptyChain: false, forcedChain: true, 2, autoPassChain: false, "timing-off-effect");
		Pass(13, emptyChain: false, forcedChain: false, 2, autoPassChain: false, "");
		Pass(11, emptyChain: false, forcedChain: false, 2, autoPassChain: false, "");
		Pass(16, emptyChain: false, forcedChain: false, 0, autoPassChain: true, "auto-pass-chain");
		Check(DuelCore.TimingOf("all") == 0 && DuelCore.TimingOf("off") == 2 && DuelCore.TimingOf("default") == 1 && DuelCore.TimingOf("乱七八糟") == 1, "模式名解析");
		Emit(new
		{
			type = "timingSelftest",
			passed = true,
			assertions = assertions
		});
		return 0;
		void Check(bool condition, string message)
		{
			assertions++;
			if (!condition)
			{
				throw new Exception("自检失败：" + message);
			}
		}
		bool Pass(int function, bool emptyChain, bool forcedChain, int timing, bool autoPassChain, string expected)
		{
			string reason;
			bool flag = DuelCore.ShouldAutoPass(function, emptyChain, forcedChain, timing, autoPassChain, out reason);
			Check(flag == expected.Length > 0, $"{function}/{emptyChain}/{timing} 结果不对（{flag}）");
			Check(reason == expected, $"{function}/{emptyChain}/{timing} 原因应为「{expected}」实际「{reason}」");
			return flag;
		}
	}

	private static string GameMessageName(byte value)
	{
		return value switch
		{
			1 => "Retry", 
			2 => "Hint", 
			3 => "Waiting", 
			4 => "Start", 
			5 => "Win", 
			6 => "UpdateData", 
			7 => "UpdateCard", 
			8 => "RequestDeck", 
			10 => "SelectBattleCmd", 
			11 => "SelectIdleCmd", 
			12 => "SelectEffectYN", 
			13 => "SelectYesNo", 
			14 => "SelectOption", 
			15 => "SelectCard", 
			16 => "SelectChain", 
			18 => "SelectPlace", 
			19 => "SelectPosition", 
			20 => "SelectTribute", 
			21 => "SortChain", 
			22 => "SelectCounter", 
			23 => "SelectSum", 
			24 => "SelectDisfield", 
			25 => "SortCard", 
			26 => "SelectUnselectCard", 
			30 => "ConfirmDecktop", 
			31 => "ConfirmCards", 
			32 => "ShuffleDeck", 
			33 => "ShuffleHand", 
			34 => "RefreshDeck", 
			35 => "SwapGraveDeck", 
			36 => "ShuffleSetCard", 
			37 => "ReverseDeck", 
			38 => "DeckTop", 
			39 => "ShuffleExtra", 
			40 => "NewTurn", 
			41 => "NewPhase", 
			42 => "ConfirmExtratop", 
			50 => "Move", 
			53 => "PosChange", 
			54 => "Set", 
			55 => "Swap", 
			56 => "FieldDisabled", 
			60 => "Summoning", 
			61 => "Summoned", 
			62 => "SpSummoning", 
			63 => "SpSummoned", 
			64 => "FlipSummoning", 
			65 => "FlipSummoned", 
			70 => "Chaining", 
			71 => "Chained", 
			72 => "ChainSolving", 
			73 => "ChainSolved", 
			74 => "ChainEnd", 
			75 => "ChainNegated", 
			76 => "ChainDisabled", 
			80 => "CardSelected", 
			81 => "RandomSelected", 
			83 => "BecomeTarget", 
			90 => "Draw", 
			91 => "Damage", 
			92 => "Recover", 
			93 => "Equip", 
			94 => "LpUpdate", 
			95 => "Unequip", 
			96 => "CardTarget", 
			97 => "CancelTarget", 
			100 => "PayLpCost", 
			101 => "AddCounter", 
			102 => "RemoveCounter", 
			110 => "Attack", 
			111 => "Battle", 
			112 => "AttackDisabled", 
			113 => "DamageStepStart", 
			114 => "DamageStepEnd", 
			120 => "MissedEffect", 
			121 => "BeChainTarget", 
			122 => "CreateRelation", 
			123 => "ReleaseRelation", 
			130 => "TossCoin", 
			131 => "TossDice", 
			132 => "RockPaperScissors", 
			133 => "HandResult", 
			140 => "AnnounceRace", 
			141 => "AnnounceAttrib", 
			142 => "AnnounceCard", 
			143 => "AnnounceNumber", 
			144 => "AnnounceCardFilter", 
			160 => "CardHint", 
			161 => "TagSwap", 
			162 => "ReloadField", 
			163 => "AiName", 
			164 => "ShowHint", 
			165 => "PlayerHint", 
			170 => "MatchKill", 
			180 => "CustomMsg", 
			200 => "DuelWinner", 
			_ => $"Unknown_{value}", 
		};
	}
}
