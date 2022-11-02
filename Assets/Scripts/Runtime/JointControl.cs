using UnityEngine;

namespace Runtime {
public class JointControl : MonoBehaviour {
  private RobotControl _controller;

  public float position;
  public ArticulationBody joint;

  void Start() {
    position = 0; // position varies [0-1]
    _controller = (RobotControl)GetComponentInParent(typeof(RobotControl));
    joint = GetComponent<ArticulationBody>();
    _controller.UpdateControlParams(this);
  }

  void FixedUpdate() {
    ArticulationDrive drive = joint.xDrive;

    float targetPosition =
        Mathf.Lerp(drive.lowerLimit, drive.upperLimit, (position + 1f) / 2f);
    drive.target = targetPosition;

    joint.xDrive = drive;
  }
}
}
