using SimParser;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Runtime {
public class RobotControl : MonoBehaviour {
  private List<(ArticulationBody body, JointControl jc)> _articulationChain;
  private ArticulationBody _selfBody;
  private IEnumerator<RobotState> _robotStates;

  public int jointCount;
  public string simulationFilepath;
  public float stiffness = 100000;
  public float damping;
  public float forceLimit = float.MaxValue;
  public bool runSimulationFile;

  void Start() {
    // Get own ArticulationBody.
    _selfBody = transform.Find("base_link").GetComponent<ArticulationBody>();

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
    if (runSimulationFile) {
      SimulationParser parser = new(jointCount, simulationFilepath);
      _robotStates = parser.GetEnumerator();
    }
  }

  void FixedUpdate() {
    if (!runSimulationFile || !_robotStates.MoveNext())
      return;

    // Get next pose from sim parser.
    RobotState nextPose = _robotStates.Current;
    SetState(nextPose);
  }

  public void SetState(RobotState nextPose) {
    // Update pose
    UpdatePose(nextPose.JointPositions.Select(d => (float)d).AsReadOnlyList());

    // Update location
    UpdateLocation(nextPose.Position, nextPose.Orientation);
  }

  private void UpdateLocation((double, double, double)nextPosition,
                              (double, double, double)nextRotation) {
    Transform bodyTransform = _selfBody.transform;
    Vector3 curPosition = bodyTransform.position;
    Vector3 curRotation = bodyTransform.rotation.eulerAngles;

    (double x, double y, double z) = nextPosition;
    (double i, double j, double k) = nextRotation;

    Vector3 newPosition =
        new Vector3((float)x + curPosition.x, (float)z + curPosition.y,
                    (float)y + curPosition.z);
    // Bullet uses z-axis for up/down, Unity uses y for that instead
    Quaternion newRotation = Quaternion.Euler((float)i + curRotation.x,
                                              (float)k - 90f + curRotation.y,
                                              (float)j + curRotation.z);

    _selfBody.TeleportRoot(newPosition, newRotation);
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
