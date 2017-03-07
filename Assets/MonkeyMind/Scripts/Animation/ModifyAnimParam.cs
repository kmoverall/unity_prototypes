using UnityEngine;
using System.Collections;

public class ModifyAnimParam : StateMachineBehaviour {


    public enum Timing { OnEnter, OnExit }
    public enum Modification { Increment, Set }

    [SerializeField]
    Timing triggerEvent;
    [SerializeField]
    Modification modFunction;
    [SerializeField]
    string paramName;
    [SerializeField]
    int value;

	 // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) 
    {
        if (triggerEvent != Timing.OnEnter)
            return;

        Modify(animator);
    }

	// OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
	//override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateExit is called when a transition ends and the state machine finishes evaluating this state
	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (triggerEvent != Timing.OnExit)
            return;

        Modify(animator);
    }

	// OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
	//override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
	//override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

    void Modify(Animator animator)
    {
        if (modFunction == Modification.Increment)
            animator.SetInteger(paramName, animator.GetInteger(paramName) + value);
        if (modFunction == Modification.Set)
            animator.SetInteger(paramName, value);
    }
}
