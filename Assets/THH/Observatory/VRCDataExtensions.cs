using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace THH.Utility
{
	public static class VRCDataExtensions
	{
		public static DataToken GetField(this DataDictionary dictionary, string path) {
			if(string.IsNullOrWhiteSpace(path)) {
				Debug.LogWarning($"Failed {nameof(GetField)}, because path is null or whitespace!");
				return new DataToken(DataError.ValueUnsupported, path);
			}

			if(path == "*") {
				VRCJson.TrySerializeToJson(dictionary, JsonExportType.Beautify, out var result);
				return result;
			}

			var pathComponents = path.Split('.');

			var currentContainer = dictionary;

			for(int i = 0; i < pathComponents.Length; i++) {
				var currentPathComponent = pathComponents[i];

				if(i == pathComponents.Length - 1) {
					return currentContainer[currentPathComponent];
				} else {
					if(!currentContainer.ContainsKey(currentPathComponent)) {
						return new DataToken(DataError.KeyDoesNotExist, path);
					}
					currentContainer = currentContainer[currentPathComponent].DataDictionary;
				}
			}

			return new DataToken(DataError.KeyDoesNotExist);
		}


		[RecursiveMethod]
		public static string Format(this DataToken token, int indentLevel = 0) {
			switch(token.TokenType) {
				case TokenType.DataDictionary:
					return FormatDataDictionary(token.DataDictionary, indentLevel + 1);
				case TokenType.DataList:
					return FormatDataList(token.DataList, indentLevel + 1);
				case TokenType.Reference:
					return $"\"[Reference: {token.GetType()}]\"";
				case TokenType.Error:
					return $"\"ERROR: {token.Error}\"";
				case TokenType.Null:
					return "null";
				case TokenType.String:
					return $"\"{token}\"";
				default:
					return token.ToString();
			}
		}

		public static string Format(this DataDictionary dataDictionary) => FormatDataDictionary(dataDictionary, 1);
		public static string Format(this DataList dataList) => FormatDataList(dataList, 1);

		[RecursiveMethod]
		public static string FormatDataList(DataList dataList, int indentLevel) {
			if(dataList.Count == 0) return "[ ]";

			string[] strings = new string[dataList.Count];

			for(int i = 0; i < dataList.Count; i++) {
				strings[i] = Format(dataList[i], indentLevel);
			}

			return $"[\n{Indent(indentLevel)}{string.Join($",\n{Indent(indentLevel)}", strings)}\n{Indent(indentLevel - 1)}]";
		}

		[RecursiveMethod]
		public static string FormatDataDictionary(DataDictionary dataDictionary, int indentLevel) {
			if(dataDictionary.Count == 0) return "{ }";

			string[] strings = new string[dataDictionary.Count];
			var keys = dataDictionary.GetKeys();


			for(int i = 0; i < dataDictionary.Count; i++) {
				var key = keys[i];
				strings[i] = $"\"{key.String}\": {Format(dataDictionary[key], indentLevel)}";
			}

			return $"{{\n{Indent(indentLevel)}{string.Join($",\n{Indent(indentLevel)}", strings)}\n{Indent(indentLevel - 1)}}}";
		}

		private static string Indent(int n) {
			return new string(' ', n * 4);
		}
	}
}