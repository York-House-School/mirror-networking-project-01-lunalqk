using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(Health), typeof(Actor), typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        public PatrolPath PatrolPath { get; set; }

        int m_PathDestinationNodeIndex;

        bool IsPathValid() {
            return PatrolPath && PatrolPath.PathNodes.Count > 0;
        }

        public void SetPathDestinationToClosestNode() {
            if (IsPathValid()) {
                int closestPathNodeIndex = 0;
                for (int i = 0; i < PatrolPath.PathNodes.Count; i++) {
                    float distanceToPathNode = PatrolPath.GetDistanceToNode(transform.position, i);
                    if (distanceToPathNode < PatrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex)) {
                        closestPathNodeIndex = i;
                    }
                }

                m_PathDestinationNodeIndex = closestPathNodeIndex;
            } else {
                m_PathDestinationNodeIndex = 0;
            }
        }

        public Vector3 GetDestinationOnPath() {
            if (IsPathValid()) {
                return PatrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
            } else {
                return transform.position;
            }
        }
    }
}