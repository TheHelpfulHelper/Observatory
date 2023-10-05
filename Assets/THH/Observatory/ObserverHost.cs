
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace THH.Observatory
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public abstract class ObserverHost : UdonSharpBehaviour 
	{
		[SerializeField, HideInInspector] public Observatory observatory;

		[System.NonSerialized]
		public DataDictionary Observer;

		public static DataDictionary CreateStateDependency(StateHost stateHost, string path) {
			var dependency = new DataDictionary();

			dependency["type"] = "stateField";
			dependency["stateHost"] = stateHost;
			dependency["path"] = path;

			return dependency;
		}

		private DataDictionary CreateObserverBase(string eventName, DataDictionary[] dependencies) {
			var observer = new DataDictionary();

			observer["host"] = this;
			observer["eventName"] = eventName;

			foreach(var dependency in dependencies) {

				var dependencyType = dependency["type"].String;
				if(dependencyType == "stateField") {
					var stateHost = (StateHost)dependency["stateHost"].Reference;
					var path = dependency["path"].String;

					if(!observatory.observedStateFields.ContainsKey(stateHost)) {
						observatory.observedStateFields[stateHost] = new DataDictionary();
					}

					var stateHostDictionary = observatory.observedStateFields[stateHost].DataDictionary;

					if(!stateHostDictionary.ContainsKey(path)) {
						var list = new DataList();
						stateHostDictionary[path] = list;
					}

					stateHostDictionary[path].DataList.Add(observer);
				} else {
					var observerDependency = dependency;
					if(!observatory.children.ContainsKey(observerDependency)) {
						observatory.children[observerDependency] = new DataList();
					}

					observatory.children[observerDependency].DataList.Add(observer);
				}
			}

			return observer;
		}

		public DataDictionary CreateEffect(string eventName, DataDictionary[] dependencies) {
			var observer = CreateObserverBase(eventName, dependencies);

			observer["type"] = "effect";

			observatory.RegisterObserver(observer);

			return observer;
		}
	}
}