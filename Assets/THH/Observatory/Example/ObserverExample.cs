
using THH.Observatory;
using THH.Utility;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ObserverExample : ObserverHost
{
    public StateHost Target;

	private DataDictionary transformer;

	public void Start() {
		var test1_dependencies = new DataDictionary[] {
			CreateStateDependency(Target, "*")
		};
		CreateEffect(nameof(Test1), test1_dependencies);

		var test2_dependencies = new DataDictionary[] {
			CreateStateDependency(Target, "o1_l1_i1.o1_l2_i1.a")
		};
		CreateEffect(nameof(Test2), test2_dependencies);

		var test3_dependencies = new DataDictionary[] {
			CreateStateDependency(Target, "o1_l1_i1.o1_l2_i2.b")
		};
		CreateEffect(nameof(Test3), test3_dependencies);

		var transformer1_Dependencies = new DataDictionary[] {
			CreateStateDependency(Target, "o1_l1_i1.o1_l2_i1.a"),
			CreateStateDependency(Target, "o1_l1_i1.o1_l2_i2.b")
		};
		transformer = CreateEffect(nameof(Transform), transformer1_Dependencies);

		var transformer2_Dependencies = new DataDictionary[] {
			transformer
		};
		CreateEffect(nameof(AfterTransform), transformer2_Dependencies);
	}

	public void Test1() {
		Debug.Log($"[{name}/{nameof(Test1)}]: State changed");
	}

	public void Test2() {
		Debug.Log($"[{name}/{nameof(Test2)}]: Property 'a' changed from {Target.PreviousState.GetField("o1_l1_i1.o1_l2_i1.a")} to {Target.State.GetField("o1_l1_i1.o1_l2_i1.a")}");
	}

	public void Test3() {
		Debug.Log($"[{name}/{nameof(Test3)}]: Property 'b' changed from {Target.PreviousState.GetField("o1_l1_i1.o1_l2_i2.b")} to {Target.State.GetField("o1_l1_i1.o1_l2_i2.b")}");
	}

	public void Transform() {
		var a = (int)Target.State.GetField("o1_l1_i1.o1_l2_i1.a").Double;
		var b = (int)Target.State.GetField("o1_l1_i1.o1_l2_i2.b").Double;

		transformer["value"] = a + b;
	}

	public void AfterTransform() {
		Debug.Log($"[{name}/{nameof(AfterTransform)}]: Transformed: {transformer["value"]}");
	}
}
