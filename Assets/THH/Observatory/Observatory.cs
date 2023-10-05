
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRRefAssist;

namespace THH.Observatory
{
	[DefaultExecutionOrder(100), UdonBehaviourSyncMode(BehaviourSyncMode.None), Singleton]
	public class Observatory : UdonSharpBehaviour
	{
		private readonly DataList observers = new DataList();
		private bool observersChanged;

		public readonly DataDictionary children = new DataDictionary();

		public readonly DataDictionary observedStateFields = new DataDictionary();

		private readonly DataList stateHosts = new DataList();
		public readonly DataList changedStateHosts = new DataList();

		private readonly DataList observersToNotify = new DataList();

		public void OnStateHostChanged(StateHost stateHost) {
			if(changedStateHosts.Contains(stateHost)) return;

			changedStateHosts.Add(stateHost);
		}

		public void RegisterStateHost(StateHost stateHost) {
			if(stateHosts.Contains(stateHost)) return;

			stateHosts.Add(stateHost);
		}

		public void RegisterObserver(DataDictionary observer) {
			if(observers.Contains(observer)) return;
			
			observers.Add(observer);
			observersChanged = true;
		}

		private void Update() {
			if(observersChanged) {
				TopoSort();
				observersChanged = false;
			}

			if(changedStateHosts.Count == 0) return;

			foreach(var stateHost_token in changedStateHosts.ToArray()) {
				var stateHost = (StateHost)stateHost_token.Reference;

				if(!observedStateFields.ContainsKey(stateHost)) continue;

				var changedPaths = stateHost.Diff();

				foreach(var changedPath_token in changedPaths.ToArray()) {
					var changedPath = changedPath_token.String;

					// TODO: optimize diffing to only diff observed paths (relatively unnessecary since most paths should be observered at all times in a regular use-case)

					var observedPathsDictionary = observedStateFields[stateHost].DataDictionary;

					if(!observedPathsDictionary.ContainsKey(changedPath)) continue;

					var observers = observedPathsDictionary[changedPath].DataList;

					foreach(var observer_token in observers.ToArray()) {
						var observer = observer_token.DataDictionary;

						if(observersToNotify.Contains(observer)) continue;

						observersToNotify.Add(observer);

						RecursivelyAddChildrenToNotify(observer);
					}
				}
			}

			var sorted = Sort();

			foreach(var observerToNotify_token in sorted.ToArray()) {
				var observerToNotify = observerToNotify_token.DataDictionary;

				var eventName = observerToNotify["eventName"].String;
				var host = (ObserverHost)observerToNotify["host"].Reference;

				host.Observer = observerToNotify;
				host.SendCustomEvent(eventName);
			}

			changedStateHosts.Clear();
			observersToNotify.Clear();
		}

		[RecursiveMethod]
		private void RecursivelyAddChildrenToNotify(DataDictionary currentObserver) {
			var _children = children[currentObserver];

			if(_children.TokenType != TokenType.Error) {
				foreach(var child in _children.DataList.ToArray()) {
					var observerChild = child.DataDictionary;
					observersToNotify.Add(observerChild);
					RecursivelyAddChildrenToNotify(observerChild);
				}
			}
		}

		private DataList Sort() {
			var list = new DataList();
			var indices = new DataList();

			foreach (var observer_token in observersToNotify.ToArray()) {
				var observer = observer_token.DataDictionary;

				indices.Add(topologicallySortedObservers.IndexOf(observer));
			}

			indices.Sort();

			foreach(var index_token in indices.ToArray()) {
				var index = (int)index_token.Int;

				list.Add(topologicallySortedObservers[index]);
			}

			return list;
		}

		private DataList topologicallySortedObservers = new DataList();

		private void TopoSort() {
			var nodes = new DataList();
			var visited = new DataList();

			foreach(var observer_token in observers.ToArray()) {
				var visitedWalk = new DataList();

				if(visited.Contains(observer_token)) continue;
				TopoSort_Recurse(observer_token.DataDictionary, visitedWalk);

				visited.AddRange(visitedWalk);

				visitedWalk.Reverse();
				nodes.AddRange(visitedWalk);
			}

			topologicallySortedObservers = nodes;
		}

		[RecursiveMethod]
		private void TopoSort_Recurse(DataDictionary current, DataList visited) {
			if(visited.Contains(current))
				return;

			if(current["type"].String == "stateField") {
				return;
			} else {
				var _children = children[current];

				if(_children.TokenType != TokenType.Error) {
					foreach(var child in _children.DataList.ToArray()) {
						TopoSort_Recurse(child.DataDictionary, visited);
					}
				}

				visited.Add(current);
			}
		}
	}
}