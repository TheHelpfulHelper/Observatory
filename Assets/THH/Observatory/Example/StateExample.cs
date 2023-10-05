
using THH.Observatory;
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None), DefaultExecutionOrder(-1)]
public class StateExample : UdonSharpBehaviour
{
	public StateHost State;

	private void Start() {
		State.SetProperty("o1_l1_i1.o1_l2_i1.a", 0);
		State.SetProperty("o1_l1_i1.o1_l2_i2.b", 0);
		State.SetProperty("o1_l1_i1.o1_l2_i2.o1_l3_i1.c", 0);
		State.SetProperty("o1_l1_i1.o1_l2_i2.o1_l3_i2.d", 0);
		State.SetProperty("o2_l1_i1.o2_l2_i1.e", 0);
		State.SetProperty("o2_l1_i1.o2_l2_i2.f", 0);
		State.SetProperty("o2_l1_i1.o2_l2_i2.o2_l3_i1.g", 0);
		State.SetProperty("o2_l1_i1.o2_l2_i2.o2_l3_i2.h", 0);

		State.InitState();
	}

	public void ChangePropertyA() {
		State.SetProperty("o1_l1_i1.o1_l2_i1.a", Random.Range(1, 1000));
		State.Sync();
    }

	public void ChangePropertyB() {
		State.SetProperty("o1_l1_i1.o1_l2_i2.b", Random.Range(1, 1000));
		State.Sync();
	}
}
