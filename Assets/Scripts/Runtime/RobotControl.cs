using SimParser;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Runtime {
public class RobotControl : MonoBehaviour {
  private List<(ArticulationBody body, JointControl jc)> _articulationChain;
  private IEnumerator<RobotState> _robotStates;

  public int jointCount;
  public string simulationFilepath;
  public float stiffness = 1;
  public float damping = 0;
  public float forceLimit = float.MaxValue;

  void Start() {
    // Set up chain.
    ArticulationBody[] chain = GetComponentsInChildren<ArticulationBody>();

    const int defDynamicVal = 10;
    foreach (ArticulationBody joint in chain) {
      joint.gameObject.AddComponent<JointControl>();
      joint.jointFriction = defDynamicVal;
      joint.angularDamping = defDynamicVal;
      ArticulationDrive currentDrive = joint.xDrive;
      currentDrive.forceLimit = forceLimit;
      currentDrive.stiffness = stiffness;
      currentDrive.damping = damping;
      joint.xDrive = currentDrive;
    }

    _articulationChain =
        chain.Select(c => (c, c.GetComponent<JointControl>())).ToList();

    // Get the robot state parser.
    SimulationParser parser = new(jointCount, simulationFilepath);
    _robotStates = parser.GetEnumerator();
  }
  void Update() {
    if (!_robotStates.MoveNext())
      return;

    // Get next pose from sim parser.
    RobotState nextPose = _robotStates.Current;

    // Update pose
    UpdatePose(nextPose.JointPositions.Select(d => (float)d).AsReadOnlyList());

    // TODO: Update location
    // UpdateLocation(nextPose.location);
  }

  private void UpdatePose(IList<float> jointPositions) {
    for (int i = 1; i < _articulationChain.Count; ++i) {
      float position = jointPositions[i - 1];
      _articulationChain[i].jc.position = position;
    }
  }

  public void UpdateControlParams(JointControl joint) {
    ArticulationDrive drive = joint.joint.xDrive;
    drive.stiffness = stiffness;
    drive.damping = damping;
    joint.joint.xDrive = drive;
  }
}
}
