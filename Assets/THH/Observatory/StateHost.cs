
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace THH.Observatory 
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public sealed class StateHost : UdonSharpBehaviour
	{
		[SerializeField, HideInInspector] private Observatory observatory;

		public DataDictionary PreviousState { get; private set; } = new DataDictionary();
		public DataDictionary State { get; private set; } = new DataDictionary();
		public DataDictionary StateWorkingVersion { get; private set; } = new DataDictionary();

		[UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(state_CHANGE))]
		private string state_SYNC = string.Empty;

		private string state_CHANGE {
			get => state_SYNC;
			set {
				state_SYNC = value;
				PreviousState = State.DeepClone();
				VRCJson.TryDeserializeFromJson(value, out var result);
				State = result.DataDictionary;
				StateWorkingVersion = State.DeepClone();

				observatory.OnStateHostChanged(this);
			}
		}

		public void Sync() {
			if(!Networking.IsOwner(gameObject)) {
				Networking.SetOwner(Networking.LocalPlayer, gameObject);
			}

			RequestSerialization();

#if UNITY_EDITOR
			// "Bug" with ClientSim, OnPreSerialization is not called in the Editor
			OnPreSerialization();
#endif
		}

		public override void OnPreSerialization() {
			VRCJson.TrySerializeToJson(StateWorkingVersion, JsonExportType.Minify, out var result);
			if(state_SYNC == result)
				return;

			state_CHANGE = result.String;
		}

		public DataToken GetProperty(string path) {
			if(string.IsNullOrWhiteSpace(path)) {
				Debug.LogWarning($"Failed {nameof(GetProperty)}, because path is null or whitespace!");
				return new DataToken(DataError.ValueUnsupported, path);
			}

			if (path == "*") {
				VRCJson.TrySerializeToJson(StateWorkingVersion, JsonExportType.Beautify, out var result);
				return result;
			}

			var pathComponents = path.Split('.');

			var currentContainer = StateWorkingVersion;

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

			return new DataToken(DataError.None);
		}

		public void SetProperty(string path, DataToken value) {
			if(string.IsNullOrWhiteSpace(path)) {
				Debug.LogWarning($"Failed {nameof(SetProperty)}, because path is null or whitespace!");
				return;
			}

			if (value.TokenType == TokenType.Reference) {
				Debug.LogWarning($"Failed {nameof(SetProperty)}, because path is null or whitespace!");
				return;
			}

			var pathComponents = path.Split('.');

			var currentContainer = StateWorkingVersion;

			for(int i = 0; i < pathComponents.Length; i++) {
				var currentPathComponent = pathComponents[i];

				if(i == pathComponents.Length - 1) {
					currentContainer[currentPathComponent] = value;
				} else {
					if(!currentContainer.ContainsKey(currentPathComponent)) {
						currentContainer[currentPathComponent] = new DataDictionary();
					}
					currentContainer = currentContainer[currentPathComponent].DataDictionary;
				}
			}
		}

		public void RemoveProperty(string path) {
			if(string.IsNullOrWhiteSpace(path)) {
				Debug.LogWarning($"Failed {nameof(RemoveProperty)}, because path is null or whitespace!");
				return;
			}

			var pathComponents = path.Split('.');

			var currentContainer = StateWorkingVersion;

			for(int i = 0; i < pathComponents.Length; i++) {
				var currentPathComponent = pathComponents[i];

				if(i == pathComponents.Length - 1) {
					currentContainer.Remove(currentPathComponent);
				} else {
					if(!currentContainer.ContainsKey(currentPathComponent)) {
						Debug.LogError("Path does not exist");
						return;
					}
					currentContainer = currentContainer[currentPathComponent].DataDictionary;
				}
			}
		}

		public void InitState() {
			observatory.RegisterStateHost(this);
			State = StateWorkingVersion.DeepClone();
			PreviousState = StateWorkingVersion.DeepClone();

			if (Networking.IsMaster) {
				Sync();
			}
		}

		public DataList Diff() {
			var diffs = new DataList();

			Diff_Recursive(PreviousState, State, diffs, "");
			
			if(diffs.Count > 0) {
				diffs.Add("*");
			}

			return diffs;
		}

		[RecursiveMethod]
		public DataList Diff_Recursive(DataDictionary left, DataDictionary right, DataList diffs, string currentPath) {
			var lKeys = left.GetKeys();
			var rKeys = right.GetKeys().ShallowClone();

			for(int i = 0; i < lKeys.Count; i++) {
				var currentKey = lKeys[i];
				rKeys.Remove(currentKey);

				var path = string.IsNullOrEmpty(currentPath) ? currentKey.String : $"{currentPath}.{currentKey.String}";
				var leftToken = left[currentKey];
				if(right.ContainsKey(currentKey)) {
					var rightToken = right[currentKey];

					if(!TokensEqual(leftToken, rightToken)) {
						diffs.Add(path);
					} else {
						if(leftToken.TokenType == TokenType.DataDictionary) {
							Diff_Recursive(leftToken.DataDictionary, rightToken.DataDictionary, diffs, path);
						}
					}
				} else {
					diffs.Add(path);
				}
			}

			for(int i = 0; i < rKeys.Count; i++) {
				var currentKey = rKeys[i];

				var path = string.IsNullOrEmpty(currentPath) ? currentKey.String : $"{currentPath}.{currentKey.String}";

				diffs.Add(path);
			}

			return diffs;
		}

		[RecursiveMethod]
		private bool DictionariesEqual(DataDictionary left, DataDictionary right) {
			var leftKeys = left.GetKeys();
			var rightKeys = right.GetKeys();

			if (leftKeys.Count != rightKeys.Count) { 
				return false; 
			}

			for(int i = 0; i < left.Count; i++) {
				var leftToken = left[i];
				var rightToken = left[i];

				if(!TokensEqual(leftToken, rightToken)) {
					return false;
				}
			}
			return true;
		}

		[RecursiveMethod]
		private bool ListsEqual(DataList left, DataList right) {
			if(left.Count != right.Count) {
				return false;
			} else {
				for(int i = 0; i < left.Count; i++) {
					var leftToken = left[i];
					var rightToken = left[i];

					if (!TokensEqual(leftToken, rightToken)) {
						return false;
					}
				}
				return true;
			}
		}

		[RecursiveMethod]
		private bool TokensEqual(DataToken left, DataToken right) {
			if(left.TokenType != right.TokenType) {
				return false;
			} else if(left.TokenType == TokenType.DataDictionary) {
				return DictionariesEqual(left.DataDictionary, right.DataDictionary);
			} else if(left.TokenType == TokenType.DataList) {
				return ListsEqual(left.DataList, right.DataList);
			} else {
				return left.Equals(right);
			}
		}
	}
}
