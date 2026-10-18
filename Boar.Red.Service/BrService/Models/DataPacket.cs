using BaoXia.Utils;
using BaoXia.Utils.Cache;
using BaoXia.Utils.Extensions;
using BaoXia.Utils.Security.Cryptography;
using Boar.Red.Service.BrService.Constants;
using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Serialization;
using static Boar.Red.Service.BrService.Constants.NamingConventions;

namespace Boar.Red.Service.BrService.Models;

public class DataPacket : IEnumerable<DataPacketSection>
{
	////////////////////////////////////////////////
	// @静态常量
	////////////////////////////////////////////////

	#region 静态常量

	public class FirstByte
	{
		/// <summary>
		/// 当前字节数组，即将开始数据包“头部参数”的结构。
		/// </summary>
		public const byte DataPacketHeader = 0x01;

		/// <summary>
		/// 当前字节数组，即将开始数据包“数据包元素”的结构。
		/// </summary>
		public const byte DataPacketSections = 0x02;

		/// <summary>
		/// 当前字节数组，即将开始压缩数据包。
		/// </summary>
		public const byte DataPacketCompressed = 0x10;
	}

	public enum CompressionMethod
	{
		None,
		Brotli,
		GZip,
		Deflate
	}

	public class CompressionMethodNames
	{
		public const string Brotli = "Brotli";

		public const string GZip = "GZip";

		public const string Deflate = "Deflate";
	}

	#endregion


	////////////////////////////////////////////////
	// @静态变量
	////////////////////////////////////////////////

	#region 静态变量

	private static readonly ObjectPool<BytesOperator> _bytesOperatorPool = new(() => new BytesOperator());

	#endregion


	////////////////////////////////////////////////
	// @自身属性
	////////////////////////////////////////////////

	#region 自身属性

	/// <summary>
	/// 当前Socket的接收结果。
	/// </summary>
	[JsonIgnore]
	public WebSocketReceiveResult? WebSocketReceiveResult { get; set; }

	public System.Text.Encoding TextEncoding { get; set; } = DataPacketTextEncoding.Default;

	public DataPackageEncryptionMethod EncryptionMethod { get; set; }

	public string? EncryptionAesKey { get; set; }

	public Dictionary<string, string?>? HeaderParams { get; set; }

	public bool IsAnyHeaderParam
	{
		get
		{
			if (HeaderParams?.Count > 0)
			{
				return true;
			}
			return false;
		}
	}

	protected List<DataPacketSection> _sections = [];

	public int Count
	{
		get { return _sections.Count; }
	}

	public bool IsTextPacket
	{
		get
		{
			if (_sections.Count == 1
			    && _sections[0].Type == DataPacketType.Text)
			{
				return true;
			}
			return false;
		}
	}

	public WebSocketMessageType WebSocketMessageType
	{
		get
		{
			var webSocketMessageType
			    = IsTextPacket
			    ? WebSocketMessageType.Text
			    : WebSocketMessageType.Binary;
			{ }
			return webSocketMessageType;
		}
	}

	public DataPacketSection this[int sectionIndex]
	{
		get
		{
			if (sectionIndex < 0)
			{
				throw new IndexOutOfRangeException("无法获取指定索引对应的数据块，指定的索引值小于“0”。");
			}
			else if (sectionIndex >= _sections.Count)
			{
				throw new IndexOutOfRangeException("无法获取指定索引对应的数据块，指定的索引值大于等于数据库数。");
			}
			return _sections[sectionIndex];
		}
		set
		{
			_sections[sectionIndex] = value;
		}
	}

	public DataPacketSection? this[string? sectionName]
	{
		get
		{
			return GetDataPacketSectionWithSectionName(sectionName);
		}
		set
		{
			SetDataSection(sectionName, value);
		}
	}

	#endregion


	////////////////////////////////////////////////
	// @类方法
	////////////////////////////////////////////////

	#region 类方法

	public static ObjectPoolItemContainer<BytesOperator>? GetBytesOperatorToUsing(
	    out BytesOperator bytesOperator)
	{
		var bytesOperatorHolder = _bytesOperatorPool.GetObjectToUsing(out var bytesOperatorNullable);
		{
			if (bytesOperatorNullable == null)
			{
				throw new ApplicationException("无法创建“BytesOperator”，程序异常。");
			}
			// !!!
			bytesOperator = bytesOperatorNullable;
			// !!!
			////////////////////////////////////////////////
			bytesOperator.Clear();
			////////////////////////////////////////////////
		}
		return bytesOperatorHolder;
	}

	public static DataPacket? CreateDataPacketWithBytes(byte[]? bytes, System.Text.Encoding textEncoding, string encryptionAesKey)
	{
		if (bytes == null || bytes.Length < 1)
		{
			return null;
		}

		var bytesOperator = new BytesOperator(bytes);

		////////////////////////////////////////////////
		// !!! 1/，读取数据块的格式标志： !!!
		////////////////////////////////////////////////
		byte firstByteToDataPacketFormat;
		do
		{
			if (!bytesOperator.TryToReadByte(out firstByteToDataPacketFormat))
			{
				return null;
			}

			////////////////////////////////////////////////
			// !!! 2/，尝试解压缩数据包。 !!!
			////////////////////////////////////////////////
			if (firstByteToDataPacketFormat == FirstByte.DataPacketCompressed)
			{
				// 1/3，读取压缩方法名称：
				if (!bytesOperator.TryToReadString(out var dataPacketCompressionMethodName)
				    || string.IsNullOrEmpty(dataPacketCompressionMethodName))
				{
					return null;
				}

				// 2/3，使用对应的解压缩方法解压缩原始数据包：
				var dataPacketBytesCompressedBeginIndex
				    = bytesOperator.CursorPosition;
				var dataPacketBytesCompressedCount
				    = bytesOperator.Count
				    - dataPacketBytesCompressedBeginIndex;
				ArraySegment<byte>? bytesDeompressed = null;

				if (CompressionMethodNames.GZip.EqualsIgnoreCase(dataPacketCompressionMethodName))
				{
					bytesDeompressed = bytes.BytesByDecompressWithGZip(
					    dataPacketBytesCompressedBeginIndex,
					    dataPacketBytesCompressedCount);
				}
				else if (CompressionMethodNames.Brotli.EqualsIgnoreCase(dataPacketCompressionMethodName))
				{
					bytesDeompressed = bytes.BytesByDecompressWithBrotli(
					    dataPacketBytesCompressedBeginIndex,
					    dataPacketBytesCompressedCount);
				}
				else if (CompressionMethodNames.Deflate.EqualsIgnoreCase(dataPacketCompressionMethodName))
				{
					bytesDeompressed = bytes.BytesByDecompressWithDeflate(
					    dataPacketBytesCompressedBeginIndex,
					    dataPacketBytesCompressedCount);
				}

				// 3/3，如果成功解压缩数据包，则替换原始数据，重新加载数据包内容，否则返回“null”：
				if (bytesDeompressed != null
				    && bytesDeompressed.Value.Count > 0)
				{
					bytesOperator.Bytes = bytesDeompressed.Value;
				}
				else
				{
					return null;
				}
			}
			else
			{
				break;
			}
		} while (true);

		////////////////////////////////////////////////
		// !!! 3/，⚠ 纯文本内容的数据包，除了文本值，其他的都不传输。 ⚠!!!
		////////////////////////////////////////////////
		DataPacket? dataPacket;
		if (firstByteToDataPacketFormat != FirstByte.DataPacketHeader)
		{
			// !!!
			dataPacket = new();
			// !!!
			{
				var textValue = textEncoding.GetString(bytes);
				dataPacket.AddDataSection(new DataPacketSection(
				    NamingConventions.DataPacketSectionNames.ApiRequestInfo,
				    textValue));
			}
		}
		////////////////////////////////////////////////
		//  !!! 4/，⚠ 非纯文本的数据包，需要按数据包格式进行读取。 ⚠!!!
		////////////////////////////////////////////////
		else
		{
			////////////////////////////////////////////////
			// 1/，读取数据包头部参数：
			////////////////////////////////////////////////
			var isDataPacketReadCompleted = true;
			var dataPacketTextEncoding = textEncoding;
			DataPackageEncryptionMethod encryptionMethod = DataPackageEncryptionMethod.None;
			string? encryptionNonce = null;
			Dictionary<string, string?>? dataPacketHeaderParams = null;
			while (true)
			{
				if (bytesOperator.CursorPosition >= bytesOperator.Count)
				{
					break;
				}
				var currentByte = bytesOperator.Bytes[bytesOperator.CursorPosition];
				if (currentByte == FirstByte.DataPacketSections)
				{
					// !!!
					bytesOperator.ReadByte();
					// !!!
					break;
				}
				if (!bytesOperator.TryToReadString(
				    out var headerParamName,
				    textEncoding))
				{
					// !!!
					isDataPacketReadCompleted = false;
					break;
					// !!!
				}
				if (!bytesOperator.TryToReadString(
				    out var headerParamValue,
				    textEncoding))
				{
					// !!!
					isDataPacketReadCompleted = false;
					break;
					// !!!
				}
				if (string.IsNullOrEmpty(headerParamName) != true)
				{
					// 固定的头部参数：
					if (headerParamName.EqualsIgnoreCase(nameof(TextEncoding)))
					{
						dataPacketTextEncoding = TextEncodingUtil.EncodingNamed(headerParamValue);
					}
					// 自定义的头部参数：
					else
					{
						dataPacketHeaderParams ??= [];
						dataPacketHeaderParams.AddOrSet(
						    headerParamName,
						    headerParamValue);
						if (DataPacketHeaderName.EncryptionMethod
						    .EqualsIgnoreCase(headerParamName))
						{
							if (DataPackageEncryptionMethodNames.AesCtr
							    .EqualsIgnoreCase(headerParamValue))
							{
								// !!!
								encryptionMethod = DataPackageEncryptionMethod.AesCtr;
								// !!!
							}
						}
						else if (DataPacketHeaderName.EncryptionNonce
						    .EqualsIgnoreCase(headerParamName))
						{
							// !!!
							encryptionNonce = headerParamValue;
							// !!!
						}
					}
				}
			}
			if (isDataPacketReadCompleted != true)
			{
				return null;
			}
			// !!!
			dataPacket = new(
			    dataPacketHeaderParams,
			    dataPacketTextEncoding,
			    encryptionMethod,
			    encryptionAesKey);
			// !!!
			byte[]? encryptionAesKeyBytes = null;
			byte[]? encryptionNonceBytes = null;
			if (encryptionMethod == DataPackageEncryptionMethod.AesCtr)
			{
				if (encryptionAesKey?.Length > 0)
				{
					encryptionAesKeyBytes = Encoding.UTF8.GetBytes(encryptionAesKey);
				}
				if (encryptionNonce?.Length > 0)
				{
					encryptionNonceBytes
					    = BytesUtil.CreateBytesFromBase64String(encryptionNonce);
				}
			}

			////////////////////////////////////////////////
			// 2/，读取数据包数据块：
			////////////////////////////////////////////////
			{
				// 1/5，读取数据块名称：
				while (bytesOperator.TryToReadString(
				    out var dataPacketSectionName,
				    dataPacketTextEncoding))
				{
					// 2/5，读取数据块的值类型：
					if (!isDataPacketReadCompleted
					    || !bytesOperator.TryToReadByte(out byte dataPacketSectionValueTypeByte))
					{
						isDataPacketReadCompleted = false;
						break;
					}
					// 3/5，读取数据块字节长度：
					if (!isDataPacketReadCompleted
					    || !bytesOperator.TryToReadInt(out int dataPacketSectionBytesLength)
					    || dataPacketSectionBytesLength < 0)
					{
						isDataPacketReadCompleted = false;
						break;
					}
					// 4/5，读取读取数据块内容字节：
					if (!isDataPacketReadCompleted
					    || !bytesOperator.TryToReadBytes(
					    dataPacketSectionBytesLength,
					    out Span<byte> dataPacketSectionBytes))
					{
						isDataPacketReadCompleted = false;
						break;
					}
					// 如果需要，则解密数据：
					switch (encryptionMethod)
					{
						default:
						case DataPackageEncryptionMethod.None:
							break;
						case DataPackageEncryptionMethod.AesCtr:
							{
								if (encryptionAesKeyBytes == null
								|| encryptionAesKeyBytes.Length < 1)
								{
									throw new Exception(
									    "当前使用Aes/Ctr算法解密数据，但是指定的“加密密钥字节数组（AesKeyBytes）”为“空”。");
								}
								if (encryptionNonceBytes == null
								    || encryptionNonceBytes.Length < 1)
								{
									throw new Exception(
									    "当前使用Aes/Ctr算法解密数据，但是生成的“随机数字节数组（NonceBytes）”为“空”。");
								}
								var dataPacketSectionByteArray
								    = dataPacketSectionBytes.ToArray();
								dataPacketSectionBytes
								    = AesCtr.DecryptBytes(
								    dataPacketSectionByteArray,
								    encryptionAesKeyBytes,
								    encryptionNonceBytes);
							}
							break;
					}


					// 5/5，创建&加入新的数据块：
					dataPacket.AddDataSection(new DataPacketSection(
					    dataPacketSectionName,
					    //
					    dataPacketSectionValueTypeByte,
					    //
					    dataPacketSectionBytes.ToArray()));
				}
			}
			if (isDataPacketReadCompleted != true)
			{
				return null;
			}
		}
		return dataPacket;
	}

	#endregion


	////////////////////////////////////////////////
	// @自身实现
	////////////////////////////////////////////////

	#region 自身实现

	public DataPacket()
	{ }

	public DataPacket(Dictionary<string, string?>? headerParams,
	    System.Text.Encoding? textEncoding, DataPackageEncryptionMethod encryptionMethod, string? encryptionAesKey)
	{
		HeaderParams = headerParams;

		if (textEncoding != null)
		{
			TextEncoding = textEncoding;
		}

		EncryptionMethod = encryptionMethod;
		EncryptionAesKey = encryptionAesKey;
	}

	public void AddHeaderParam(
	    string name,
	    string? value)
	{
		HeaderParams ??= [];
		HeaderParams.Add(name, value);
	}

	public void SetHeaderParam(
	    string name,
	    string? value)
	{
		HeaderParams ??= [];
		HeaderParams.AddOrSet(name, value);
	}

	public void RemoveHeaderParam(
	    string name)
	{
		HeaderParams?.Remove(name);
	}

	public void ClearHeaderParam()
	{
		HeaderParams?.Clear();
	}

	public string? GetHeaderParam(string? name)
	{
		if (string.IsNullOrEmpty(name) != true
		    && HeaderParams?.TryGetValue(name, out var @value) == true)
		{
			return @value;
		}
		return null;
	}

	public void AddDataSection(
	    DataPacketSection? contentSection)
	{
		if (contentSection != null)
		{
			_sections.Add(contentSection);
		}
	}
	public void AddDataSection(
	    string? name,
	    byte[]? bytesValue)
	{
		AddDataSection(new DataPacketSection(name, bytesValue));
	}

	public void AddDataSection(
	    string? name,
	    string? textValue)
	{
		AddDataSection(new DataPacketSection(name, textValue));
	}

	public void AddDataSection(
	    string? name,
	    Dictionary<string, string?>? dictionaryValue)
	{
		AddDataSection(new DataPacketSection(name, dictionaryValue));
	}

	public void AddDataSection(
	    string? name,
	    object? objectValue)
	{
		AddDataSection(new DataPacketSection(name, objectValue));
	}

	public void AddDataSectionRange(IEnumerable<DataPacketSection>? dataPacketSections)
	{
		if (dataPacketSections?.IsNotEmpty() == true)
		{
			_sections.AddRange(dataPacketSections);
		}
	}

	public void SetDataSection(string? name, DataPacketSection? contentSection)
	{
		RemoveDataSectionWithSectionName(name);
		AddDataSection(contentSection);
	}

	public void SetDataSection(
	    string? name,
	    string? textValue)
	{
		SetDataSection(
		    name,
		    new DataPacketSection(name, textValue));
	}

	public void SetDataSection(
	    string? name,
	    byte[]? bytesValue)
	{
		SetDataSection(
		    name,
		    new DataPacketSection(name, bytesValue));
	}

	public void RemoveDataSection(DataPacketSection? contentSection)
	{
		if (contentSection != null)
		{
			_sections.Remove(contentSection);
		}
	}

	public void RemoveDataSectionWithSectionName(string? sectionName)
	{
		for (var sectionIndex = _sections.Count - 1;
		    sectionIndex >= 0;
		    sectionIndex--)
		{
			var section = _sections[sectionIndex];
			if (StringExtension.EqualsIgnoreCase(
			    section.Name,
			    sectionName))
			{
				_sections.RemoveAt(sectionIndex);
			}
		}
	}

	public void ClearDataSection()
	{
		_sections.Clear();
	}

	public DataPacketSection? GetDataPacketSectionWithSectionName(string? sectionName)
	{
		foreach (var section in _sections)
		{
			if (StringExtension.EqualsIgnoreCase(
			    section.Name,
			    sectionName))
			{
				return section;
			}
		}
		return null;
	}

	public byte[] ToBytes(
	    System.Text.Encoding textEncoding,
	    DataPackageEncryptionMethod encryptionMethod,
	    string? encryptionAesKey,
	    int dataPacketSizeToCompression,
	    string? dataPacketCompressionMethodName)
	{
		// !!! 强制更新“TextEncoding” !!!
		{
			TextEncoding = textEncoding;
			EncryptionMethod = encryptionMethod;
			EncryptionAesKey = encryptionAesKey;
		}
		// !!!

		////////////////////////////////////////////////
		// !!!⚠ 纯文本内容的数据包，除了文本值，其他的都不传输。 ⚠!!!
		////////////////////////////////////////////////
		byte[]? bytes = null;
		if (IsTextPacket
		    && IsAnyHeaderParam == false)
		{
			bytes = [.. _sections[0].GetValueBytes(TextEncoding)];
			// !!!
			return bytes;
			// !!!
		}

		using var _ = GetBytesOperatorToUsing(out var bytesOperator);
		if (bytesOperator == null)
		{
			throw new ApplicationException("无法获取有效的“bytesOperator”。");
		}

		////////////////////////////////////////////////
		// 1/5，写入数据包基本信息。
		////////////////////////////////////////////////
		byte[]? encryptionAesKeyBytes = null;
		string? encryptionNonce = null;
		byte[]? encryptionNonceBytes = null;
		{
			// 1/，写入数据块的格式标志： 
			bytesOperator.Write(FirstByte.DataPacketHeader);

			// 2/，写入数据块的头部参数，固定参数：
			bytesOperator.Write(
			    nameof(TextEncoding),
			    TextEncoding);
			bytesOperator.Write(
			    TextEncodingUtil.NameOfEncoding(TextEncoding),
			    TextEncoding);

			// 3/，写入数据库的头部参数，自定义参数：
			var headerParams = HeaderParams;

			switch (encryptionMethod)
			{
				default:
				case DataPackageEncryptionMethod.None:
					break;
				case DataPackageEncryptionMethod.AesCtr:
					{
						if (encryptionAesKey?.Length > 0)
						{
							encryptionAesKeyBytes
							    = Encoding.UTF8.GetBytes(encryptionAesKey);
							encryptionNonceBytes
							    = BytesUtil.CreateRandomBytesInLength(
							    DataPackageEncryptionConstants.AesNonceBytesCount);
							encryptionNonce
							    = BytesUtil.CreateBase64StringOfBytes(encryptionNonceBytes);

							headerParams ??= [];
							headerParams.Add(
							    DataPacketHeaderName.EncryptionMethod,
							    DataPackageEncryptionMethodNames.AesCtr);
							headerParams.Add(
							    DataPacketHeaderName.EncryptionNonce,
							    encryptionNonce);
						}
					}
					break;
			}

			if (headerParams?.Count > 0)
			{
				foreach (var headerParam in headerParams)
				{
					var headerParamName = headerParam.Key;
					var headerParamValue = headerParam.Value;
					if (headerParamName?.Length > 0)
					{
						bytesOperator.Write(
						    headerParamName,
						    TextEncoding);
						bytesOperator.Write(
						    headerParamValue,
						    TextEncoding);
					}
				}
			}
		}

		////////////////////////////////////////////////
		// 2/5，写入头部参数和数据块的分隔符号。
		////////////////////////////////////////////////
		{
			bytesOperator.Write(FirstByte.DataPacketSections);
		}

		////////////////////////////////////////////////
		// 3/5，写入数据块。
		////////////////////////////////////////////////
		{
			// 写入每个数据块。
			foreach (var dataSection in _sections)
			{
				// 1/4，写入数据块名称：
				bytesOperator.Write(
				    dataSection.Name,
				    TextEncoding);
				// 2/4，写入值类型：
				bytesOperator.Write(
				    (byte)dataSection.Type);
				// 3/4，写入数据块内容字节：
				var dataSectionBytes = dataSection.GetValueBytes(TextEncoding);
				// 如果需要，则加密数据：
				switch (encryptionMethod)
				{
					default:
					case DataPackageEncryptionMethod.None:
						break;
					case DataPackageEncryptionMethod.AesCtr:
						{
							if (encryptionAesKeyBytes == null
							    || encryptionAesKeyBytes.Length < 1)
							{
								throw new Exception(
								    "当前使用Aes/Ctr算法加密数据，但是指定的“加密密钥字节数组（AesKeyBytes）”为“空”。");
							}
							if (encryptionNonceBytes == null
							    || encryptionNonceBytes.Length < 1)
							{
								throw new Exception(
								    "当前使用Aes/Ctr算法加密数据，但是生成的“随机数字节数组（NonceBytes）”为“空”。");
							}
							dataSectionBytes
							    = AesCtr.EncryptBytes(
							    dataSectionBytes,
							    encryptionAesKeyBytes,
							    encryptionNonceBytes,
							    out var _);
						}
						break;
				}
				var dataSectionBytesLength = dataSectionBytes.Count;
				{
					// !!! 注意长度为“0”，也要写入
					bytesOperator.Write(dataSectionBytesLength);
					// !!!
				}
				// 4/4，写入数据块字节：
				if (dataSectionBytesLength > 0)
				{
					bytesOperator.Write(dataSectionBytes);
				}
			}
		}

		////////////////////////////////////////////////
		// 4/5，获取原始字节。
		////////////////////////////////////////////////
		var bytesOperatorBytes = bytesOperator.Bytes;

		////////////////////////////////////////////////
		// 5/5，如果开启了压缩方法，则对整个数据包进行压缩。
		////////////////////////////////////////////////
		if (dataPacketSizeToCompression > 0
		    && bytesOperatorBytes.Count > dataPacketSizeToCompression)
		{
			byte[]? bytesCompressed = null;
			string? bytesCompressMethodName = null;
			if (bytesOperatorBytes.Array is byte[] originalBytes)
			{
				if (CompressionMethodNames.GZip
				    .EqualsIgnoreCase(dataPacketCompressionMethodName))
				{
					bytesCompressed
					    = ByteArrayExtension.BytesByCompressWithGZip(
					    originalBytes,
					    bytesOperatorBytes.Offset,
					    bytesOperatorBytes.Count);
					bytesCompressMethodName
					    = CompressionMethodNames.GZip;
				}
				else if (CompressionMethodNames.Deflate
				    .EqualsIgnoreCase(dataPacketCompressionMethodName))
				{
					bytesCompressed
					    = ByteArrayExtension.BytesByCompressWithDeflate(
					    originalBytes,
					    bytesOperatorBytes.Offset,
					    bytesOperatorBytes.Count);
					bytesCompressMethodName
					    = CompressionMethodNames.Deflate;
				}
				else // if (CompressionMethodNames.Brotli
				     // .EqualsIgnoreCase(dataPacketCompressionMethodName))
				{
					bytesCompressed
					    = ByteArrayExtension.BytesByCompressWithBrotli(
					    originalBytes,
					    bytesOperatorBytes.Offset,
					    bytesOperatorBytes.Count);
					bytesCompressMethodName
					    = CompressionMethodNames.Brotli;
				}
			}
			if (bytesCompressed != null)
			{
				bytesOperator.Clear();

				// 1/4，写入压缩标志：
				{
					bytesOperator.Write(FirstByte.DataPacketCompressed);
				}
				// 2/4，写入压缩算法名称：
				if (bytesCompressMethodName != null)
				{
					bytesOperator.Write(bytesCompressMethodName);
				}
				// 3/4，写入压缩后的字节数组：
				{
					bytesOperator.Write(bytesCompressed);
				}
				// 4/4，更新最终的数据包字节数组：
				// !!!
				bytes = [.. bytesOperator.Bytes];
				// !!!
			}
		}
		// !!!
		bytes ??= [.. bytesOperatorBytes];
		// !!!
		return bytes;
	}

	#endregion


	////////////////////////////////////////////////
	// @实现“IEnumerator”
	////////////////////////////////////////////////

	#region 实现“IEnumerator”

	public IEnumerator<DataPacketSection> GetEnumerator()
	{
		return _sections.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _sections.GetEnumerator();
	}

	#endregion
}
