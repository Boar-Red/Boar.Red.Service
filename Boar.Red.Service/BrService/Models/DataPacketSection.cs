using BaoXia.Utils;
using BaoXia.Utils.Extensions;

namespace Boar.Red.Service.BrService.Models
{
	public class DataPacketSection
	{
		////////////////////////////////////////////////
		// @自身属性
		////////////////////////////////////////////////

		#region 自身属性

		public string? Name { get; set; }

		public DataPacketType Type { get; set; } = DataPacketType.Bytes;


		protected byte[]? _bytesValue;

		public byte[]? BytesValue
		{
			get
			{
				return _bytesValue;
			}
			set
			{
				if (_bytesValue == value)
				{
					return;
				}

				_bytesValue = value;
				_isUnbytesValueValid = false;
				_textValue = null;
				_dictionaryValue = null;
				_objectValue = null;
			}
		}

		protected bool _isUnbytesValueValid;

		protected string? _textValue;

		protected Dictionary<string, string?>? _dictionaryValue;

		protected object? _objectValue;

		#endregion


		////////////////////////////////////////////////
		// @自身实现
		////////////////////////////////////////////////

		#region 自身实现

		public DataPacketSection(
		    string? name,
		    //
		    DataPacketType valueType,
		    //
		    byte[]? bytesValue,
		    //
		    string? textValue,
		    //
		    Dictionary<string, string?>? dictionaryValue,
		    //
		    object? objectValue)
		{
			Name = name;

			Type = valueType;

			BytesValue = bytesValue;

			_textValue = textValue;

			_dictionaryValue = dictionaryValue;

			_objectValue = objectValue;
		}

		public DataPacketSection(
		    string? name,
		    //
		    byte[]? bytesValue)
		{
			Name = name;

			Type = DataPacketType.Bytes;

			BytesValue = bytesValue;
		}

		public DataPacketSection(
		    string? name,
		    //
		    string? textValue)
		{
			Name = name;

			Type = DataPacketType.Text;

			_textValue = textValue;
		}

		public DataPacketSection(
		    string? name,
		    //
		    Dictionary<string, string?>? dictionaryValue)
		{
			Name = name;

			Type = DataPacketType.Dictionary;

			_dictionaryValue = dictionaryValue;
		}

		public DataPacketSection(
		    string? name,
		    //
		    object? objectValue)
		{
			Name = name;

			Type = DataPacketType.Object;

			_objectValue = objectValue;
		}

		public DataPacketSection(
		    string? name,
		    //
		    byte typeByte,
		    byte[]? bytesValue)
		{
			Name = name;

			Type = (DataPacketType)typeByte;

			if (bytesValue == null
			    || bytesValue.Length < 1)
			{
				return;
			}

			// 记录原始字节数据：
			// !!!
			BytesValue = bytesValue;
			// !!!
		}

		public string? GetTextValue(
		    System.Text.Encoding dataPacketTextEncoding)
		{
			if (Type != DataPacketType.Text)
			{
				return null;
			}

			if (_isUnbytesValueValid)
			{
				return _textValue;
			}

			// 读取文本数据：
			var bytesOperator = new BytesOperator(BytesValue);
			bytesOperator.TryToReadString(
			    out var textValue,
			    dataPacketTextEncoding);

			////////////////////////////////////////////////
			// !!!
			_isUnbytesValueValid = true;
			_textValue = textValue;
			// !!!
			////////////////////////////////////////////////

			return _textValue;
		}

		public Dictionary<string, string?>? GetDictionaryValue(
		    System.Text.Encoding dataPacketTextEncoding)
		{
			if (Type != DataPacketType.Dictionary)
			{
				return null;
			}

			if (_isUnbytesValueValid)
			{
				return _dictionaryValue;
			}

			// 读取字典数据：
			Dictionary<string, string?>? dictionaryValue = null;
			var bytesOperator = new BytesOperator(BytesValue);
			// 1/2，读取字典元素数量：
			if (bytesOperator.TryToReadInt(out var itemsCount)
			    && itemsCount > 0)
			{
				// 2/2，读取元素键值对：
				for (var itemIndex = 0;
				    itemIndex < itemsCount;
				    itemIndex++)
				{
					if (bytesOperator.TryToReadString(
					    out var itemName,
					    dataPacketTextEncoding))
					{
						if (bytesOperator.TryToReadString(
						    out var itemValue,
						    dataPacketTextEncoding))
						{
							if (!string.IsNullOrEmpty(itemName))
							{
								dictionaryValue ??= [];
								dictionaryValue.AddOrSet(itemName, itemValue);
							}
						}
					}
				}
			}

			////////////////////////////////////////////////
			// !!!
			_isUnbytesValueValid = true;
			_dictionaryValue = dictionaryValue;
			// !!!
			////////////////////////////////////////////////

			return _dictionaryValue;
		}

		public object? GetObjectValue(Type objectType, System.Text.Encoding dataPacketTextEncoding)
		{
			if (Type != DataPacketType.Object)
			{
				return default;
			}

			// 读取文本数据：
			object? objectValue = default;
			var objectJsonText = BytesValue != null ? dataPacketTextEncoding.GetString(BytesValue) : null;
			if (!string.IsNullOrEmpty(objectJsonText))
			{
				// !!!
				objectValue = objectJsonText.ToObjectByJsonDeserialize(objectType);
				// !!!
			}

			////////////////////////////////////////////////
			// !!!
			_isUnbytesValueValid = true;
			_objectValue = objectValue;
			// !!!
			////////////////////////////////////////////////

			return objectValue;
		}

		public ObjectType? GetObjectValue<ObjectType>(
		    System.Text.Encoding dataPacketTextEncoding)
		{
			if (GetObjectValue(
			    typeof(ObjectType),
			    dataPacketTextEncoding) is ObjectType objectValue)
			{
				return objectValue;
			}
			return default;
		}

		public ArraySegment<byte> GetValueBytes(System.Text.Encoding dataPacketTextEncoding)
		{
			ArraySegment<byte> bytes;
			switch (Type)
			{
				default:
				case DataPacketType.Bytes:
					{
						if (BytesValue != null)
						{
							bytes = new ArraySegment<byte>(BytesValue);
						}
						else
						{
							bytes = new ArraySegment<byte>();
						}
					}
					break;
				case DataPacketType.Text:
					{
						if (_textValue?.Length > 0)
						{
							// 写入字符串值：
							bytes = dataPacketTextEncoding.GetBytes(_textValue);
						}
						else
						{
							bytes = new ArraySegment<byte>();
						}
					}
					break;
				case DataPacketType.Dictionary:
					{
						// 写入字典数据：
						var bytesOperator = new BytesOperator();
						var dictionaryValue = _dictionaryValue;

						// 1/3，写入字典元素数量：
						var itemsCount
						    = dictionaryValue != null
						    ? dictionaryValue.Count
						    : 0;
						{
							bytesOperator.Write(itemsCount);
						}

						// 2/3，写入元素键值对：
						if (dictionaryValue != null)
						{
							foreach (var item in dictionaryValue)
							{
								bytesOperator.Write(item.Key);
								bytesOperator.Write(item.Value);
							}
						}

						// 3/3，获取最终的字节数组：
						// !!!
						bytes = bytesOperator.ToBytes();
						// !!!
					}
					break;
				case DataPacketType.Object:
					{
						string? objectJsonText = _objectValue?.ToJsonString();
						if (objectJsonText?.Length > 0)
						{
							// 写入字符串值：
							bytes = dataPacketTextEncoding.GetBytes(objectJsonText);
						}
						else
						{
							bytes = new ArraySegment<byte>();
						}
					}
					break;
			}
			return bytes;
		}

		#endregion


		////////////////////////////////////////////////
		// @实现“Object”
		////////////////////////////////////////////////

		#region 实现“Object”

		public override bool Equals(object? obj)
		{
			if (obj is DataPacketSection contentSection)
			{
				if (StringExtension.EqualsIgnoreCase(this.Name, contentSection.Name)
				    == true)
				{
					return true;
				}
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Name?.GetHashCode() ?? 0;
		}

		#endregion
	}
}
