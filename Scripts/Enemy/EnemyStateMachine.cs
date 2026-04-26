using UnityEngine;

public class EnemyStateMachine : MonoBehaviour
{
    // -------------------- STATES --------------------
    public enum State { Idle, Inspect, Attack, Chase, Dead }


    private State _startingState = State.Idle;

    public State currentState { get; private set; }

    // -------------------- REFERENCES --------------------
    private Enemy enemy;

    // -------------------- UNITY EVENTS --------------------
    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        ChangeState(_startingState);
    }

    private void Update()
    {
        
        if (GameManager.Instance?.IsPlayerDead == true) return;
        if (currentState == State.Dead) return;

        
    }

    // -------------------- STATE TRANSITIONS --------------------
    public void ChangeState(State newState)
    {
        if (currentState == newState) return;

        
        OnExitState(currentState);

        
        currentState = newState;

        
        OnEnterState(newState);
    }

    // -------------------- ENTER STATE --------------------
    private void OnEnterState(State state)
    {
        switch (state)
        {
            case State.Idle:
                enemy.movement.ResumeIdleBehavior();
                enemy.movement.ReturnToInitialPositionIfNeeded();
                break;

            case State.Inspect:
            
                break;

            case State.Attack:
               
                enemy.movement.StopMoving();
                break;

            case State.Chase:
               
                break;

            case State.Dead:
               
                enemy.movement.StopMoving();
                break;
        }
    }

    // -------------------- EXIT STATE --------------------
    private void OnExitState(State state)
    {
        
        switch (state)
        {
            case State.Attack:
            case State.Chase:
               
                break;

            case State.Inspect:
              
                break;
        }
    }


}