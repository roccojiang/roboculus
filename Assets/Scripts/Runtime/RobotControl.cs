using SimParser;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using System;

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

  private Vector3 _startingPosition = Vector3.zero;
  private float _yCorrection = 0.0f;
  private Quaternion _startingRotation = Quaternion.identity;
  private Renderer[] _renderers;

  public float deadZone = 0.15f;
  public float heightMultiplier = 0.0025f;

  public bool Grabbable { get; set; } = true;
  private bool _objectGrabbed = false;

  private OVRInput.Controller _dominantHand;

  void Start() {
    ObjectManipulator.UpdateObjectGrabState += grabbed => _objectGrabbed =
        grabbed;
    ObjectManipulator.OnRobotGrab += ManipulateRobot;
    ObjectManipulator.DominantHandChanged += controller => _dominantHand =
        controller;

    // Get own ArticulationBody.
    _selfBody = transform.Find("base_link").GetComponent<ArticulationBody>();

    // Find all the robot's renderers.
    _renderers = GetComponentsInChildren<Renderer>();
    foreach (Renderer r in _renderers) {
      // """Gently coerce""" the materials to be transparent.
      foreach (Material m in r.materials) {
        m.SetFloat("_Mode", 3f); // Pong my-[EXPLETIVE REDACTED]
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetFloat("_SrcBlend", (float)BlendMode.One);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0.0f);
        m.DisableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHABLEND_ON");
        m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
      }
    }

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

  private static void ChangeOpacity(Renderer renderer, float increment) {
    foreach (Material material in renderer.materials) {
      Color oldColor = material.color;
      float oldAlpha = oldColor.a;

      material.color = new Color(oldColor.r, oldColor.g, oldColor.b,
                                 Mathf.Clamp(oldAlpha + increment, 0.0f, 1.0f));
    }
  }

  private void HandleOpacityInputs() {
    // Get the thumbstick axes for the secondary controller.
    OVRInput.Controller nonDominantHand =
        _dominantHand == OVRInput.Controller.RTouch
            ? OVRInput.Controller.LTouch
            : OVRInput.Controller.RTouch;

    Vector2 secondThumbS =
        OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, nonDominantHand);
    float vert = secondThumbS.y;

    // If the controls are within a thumbstick dead-zone, don't change the
    // opacity.
    if (Mathf.Abs(vert) < deadZone)
      return;

    // Modify the opacities here.
    foreach (Renderer r in _renderers) {
      ChangeOpacity(r, 0.025f * vert);
    }
  }

  private void HandleRobotHeight() {
    Vector2 thumbstick =
        OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, _dominantHand);

    // Height-adjust mode, no triggers held
    // Dominant stick precisely adjusts Y-axis
    // if (!(OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controller) ||
    //       OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, controller))) {
    if (!_objectGrabbed) {
      Vector3 oldPosition = _selfBody.transform.position;
      oldPosition.y += heightMultiplier * thumbstick.y;
      _selfBody.TeleportRoot(oldPosition, _selfBody.transform.rotation);
    }

    // Pressing thumbstick resets robot height
    if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, _dominantHand)) {
      SetToGround();
    }
  }

  private Vector3 ManipulateRobot(Vector3 oldPosition, Vector3 newPosition) {
    // Both triggers: free X/Y/Z axes movement
    if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, _dominantHand) &&
        OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, _dominantHand)) {
      print("[***] both triggers");
    }
    // Side trigger: X/Z axes movement
    else if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, _dominantHand)) {
      newPosition.y = oldPosition.y;
      print("[***] side trigger");
    }
    // Front trigger: Y axis movement
    else if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, _dominantHand)) {
      print("[***] front trigger");
      newPosition.x = oldPosition.x;
      newPosition.z = oldPosition.z;
    }

    return newPosition;
  }

  void FixedUpdate() {
    if (runSimulationFile && _robotStates.MoveNext()) {
      // Get next pose from sim parser.
      RobotState nextPose = _robotStates.Current;
      SetState(nextPose);
    } else {
      HandleOpacityInputs();
      HandleRobotHeight();
    }
  }

  public void SetStartPosition() {
    _startingPosition = _selfBody.transform.position;
  }

  public void SetStartRotation() {
    _startingRotation = _selfBody.transform.rotation;
  }

  public float GetRobotHeight() {
    return _selfBody.GetComponentsInChildren<MeshRenderer>()
        .Select(c => {
          return Math.Abs(_selfBody.transform.position.y - c.bounds.min.y);
        })
        .Max();
  }

  public void SetToGround() {
    Vector3 robotPos = _selfBody.transform.position;
    robotPos.y = GetRobotHeight();
    _selfBody.TeleportRoot(robotPos, _selfBody.transform.rotation);
  }

  public void SetState(RobotState nextPose) {
    // If first state, set yCorrection.
    if (nextPose.IsFirst)
      _yCorrection = (float)nextPose.Position.z;

    // Update pose
    UpdatePose(nextPose.JointPositions.Select(d => (float)d).AsReadOnlyList());

    // Update location
    UpdateLocation(nextPose.Position, nextPose.Orientation);
  }

  private void UpdateLocation((double, double, double)nextPosition,
                              (double, double, double)nextRotation) {
    // Bullet uses Z for up/down. Unity uses Y for that.
    (double x, double z, double y) = nextPosition;
    (double i, double k, double j) = nextRotation;

    Quaternion newOrientation = Quaternion.Euler((float)i, (float)j, (float)k);

    Quaternion newRotation =
        Quaternion.Euler((float)i, (float)j + 90f, (float)k);

    newRotation *= _startingRotation;
    newOrientation *= _startingRotation;

    Vector3 nextVectorPosition =
        new((float)x, (float)y - _yCorrection, (float)z);

    Vector3 newPosition =
        (newRotation * nextVectorPosition) + _startingPosition;

    _selfBody.TeleportRoot(newPosition, newOrientation);
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
