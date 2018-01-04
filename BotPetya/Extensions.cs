using System;
using System.Linq;
using System.Xml;

namespace BotPetya
{
	/// <summary>Методы расширения.</summary>
	static class Extensions
	{
		/// <summary>Сохраняет указанный параметр в xml-узел.</summary>
		/// <typeparam name="T">Тип параметра.</typeparam>
		/// <param name="node">Узел, в который необходимо сохранить параметр.</param>
		/// <param name="name">Имя параметра.</param>
		/// <param name="value">Значение параметра.</param>
		public static void WriteParameter<T>(this XmlNode node, string name, T value)
		{
			if(node == null)
				throw new ArgumentNullException("Xml node is null");
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException("Xml element name is null");

			var doc = node.OwnerDocument;
			var element = doc.CreateElement(name);
			var attribute = doc.CreateAttribute("Value");

			string val = null;
			switch(value)
			{
				case string v: val = v;
					break;
				case bool v: val = XmlConvert.ToString(v);
					break;
				case int v:
					val = XmlConvert.ToString(v);
					break;
				case double v:
					val = XmlConvert.ToString(v);
					break;
				case Enum v:
					val = Enum.GetName(typeof(T), v);
					break;
				case var v when v != null: throw new ArgumentException($"Unsupported type {v.GetType().Name}.", nameof(value));
			}
			attribute.Value = val ?? string.Empty;
			element.Attributes.Append(attribute);
			node.AppendChild(element);
		}

		public static T ReadParameter<T>(this XmlNode node, string name, T defValue)
		{
			var child = node.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
			if(child == null) return defValue;
			var valueAttr = child.Attributes?["Value"];
			if(valueAttr == null) return defValue;

			switch(defValue)
			{
				case string _: return (T)(object)valueAttr.Value;
				case bool _: return (T)(object)XmlConvert.ToBoolean(valueAttr.Value.ToLower());
				case int _: return (T)(object)XmlConvert.ToInt32(valueAttr.Value);
				case double _: return (T)(object)XmlConvert.ToDouble(valueAttr.Value);
				case Enum _: return (T)Enum.Parse(typeof(T), valueAttr.Value);
				default: return defValue;
			}
		}
	}
}
