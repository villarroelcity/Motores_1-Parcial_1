using UnityEngine;

namespace EnElCamino
{
    /// <summary>
    /// Small state-based threat used by the final escape sequence.
    /// It intentionally avoids requiring a baked NavMesh so the challenge works in the existing scene.
    /// </summary>
    public class StationThreatAI : MonoBehaviour
    {
        private enum State { Waiting, Chasing, Disabled }

        [SerializeField] private float chaseSpeed = 2.2f;
        [SerializeField] private float catchDistance = 2.2f;

        private Transform target;
        private Transform exit;
        private GameFlow gameFlow;
        private State state = State.Waiting;
        private Vector3 patrolAnchor;
        private float patrolTime;

        public void Configure(Transform chaseTarget, GameFlow flow, Transform finishExit)
        {
            target = chaseTarget;
            gameFlow = flow;
            exit = finishExit;
            patrolAnchor = transform.position;
            state = State.Chasing;
        }

        private void Update()
        {
            if (state == State.Disabled || target == null || gameFlow == null || gameFlow.finished || gameFlow.gameOver)
                return;

            if (state == State.Waiting)
            {
                patrolTime += Time.deltaTime;
                transform.position = patrolAnchor + new Vector3(Mathf.Sin(patrolTime) * 1.5f, 0f, Mathf.Cos(patrolTime) * 1.5f);
                return;
            }

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.05f)
            {
                Vector3 step = direction.normalized * chaseSpeed * Time.deltaTime;
                transform.position += step;
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            if (direction.magnitude <= catchDistance)
            {
                state = State.Disabled;
                gameFlow.GameOver();
            }
        }

        public void StopThreat()
        {
            state = State.Disabled;
        }
    }
}
