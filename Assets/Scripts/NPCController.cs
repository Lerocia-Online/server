using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Lerocia.Characters;
using Lerocia.Characters.NPCs;
using System.Linq;

public class NPCController : MonoBehaviour {
  public NPC Npc;
  private Transform _target;
  private NavMeshAgent _agent;
  public List<string> TargetTypes;
  private int _destinationIndex;
  private bool _destinationReached;
  private float _destinationReachedTime;

  private void Start() {
    _agent = GetComponent<NavMeshAgent>();
    _destinationIndex = 0;
  }

  private void Update() {
    if (TargetTypes != null) {
      TryToFindTarget();
    }

    if (_target == null) {
      SetWanderDestination();
    }
  }

  private void TryToFindTarget() {
    float closestDistance = float.MaxValue;
    bool foundTarget = false;
    foreach (Character character in ConnectedCharacters.Characters.Values) {
      if (TargetTypes.Contains(character.CharacterPersonality)) {
        float distance = Vector3.Distance(character.Avatar.transform.position, transform.position);

        if (distance < Npc.LookRadius && distance < closestDistance) {
          closestDistance = distance;
          foundTarget = true;
          _target = character.Avatar.transform;
        }
      }
    }

    if (foundTarget) {
      _agent.SetDestination(_target.position);
      if (closestDistance <= _agent.stoppingDistance) {
        //TODO Attack target
        FaceTarget();
      }
    } else {
      _target = null;
    }
  }

  private void SetWanderDestination() {
    if (Npc.Destinations.Any()) {
      if (!_destinationReached) {
        _agent.SetDestination(Npc.Destinations[_destinationIndex].Position);
        float distance = Vector3.Distance(Npc.Destinations[_destinationIndex].Position, transform.position);
        if (distance <= _agent.stoppingDistance) {
          _destinationReached = true;
          _destinationReachedTime = Time.time;
        }
      } else {
        if (Time.time - _destinationReachedTime >= Npc.Destinations[_destinationIndex].Duration) {
          if (_destinationIndex >= Npc.Destinations.Count - 1) {
            _destinationIndex = 0;
          } else {
            _destinationIndex++;
          }

          _destinationReached = false;
          _agent.SetDestination(Npc.Destinations[_destinationIndex].Position);
        }
      }
    } else {
      _agent.SetDestination(Npc.Origin);
    }
  }

  private void FaceTarget() {
    Vector3 direction = (_target.position - transform.position).normalized;
    Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
  }

  public void Destroy() {
    Destroy(gameObject);
  }

  private void OnDrawGizmosSelected() {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, Npc.LookRadius);
  }
}