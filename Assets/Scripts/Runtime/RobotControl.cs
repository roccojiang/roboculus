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
  private const float DeadZone = 0.15f;
  private float _heldTime = 0.0f;
  private StickAxes _stickAxes = StickAxes.Vertical;
  private LineRenderer _laserPointer;

  public bool Grabbable { get; set; } = true;
  public bool Manipulatable { get; set; } = true;

  void Start() {
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

    // Grab a reference to an ObjectManipulator's laser.
    _laserPointer =
        GameObject.Find("ObjectManipulator")?.GetComponent<LineRenderer>();

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
    if (!Manipulatable)
      return;

    // Get the thumbstick axes for the secondary controller.
    Vector2 secondThumbS = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
    float vert = secondThumbS.y;

    // If the controls are within a thumbstick dead-zone, don't change the
    // opacity.
    if (Mathf.Abs(vert) < DeadZone)
      return;

    // Modify the opacities here.
    foreach (Renderer r in _renderers) {
      ChangeOpacity(r, 0.025f * vert);
    }
  }

  private void HandleThumbstickClick() {
    if (!Manipulatable) {
      _heldTime = 0.0f;
      return;
    }

    if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick) ||
        OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick)) {
      _heldTime += Time.fixedDeltaTime;
      return;
    }

    if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick) ||
        OVRInput.GetUp(OVRInput.Button.SecondaryThumbstick)) {
      if (_heldTime > 1f)
        SetToGround();
      else
        SwitchStickAxes();
      _heldTime = 0.0f;
    }
  }

  private void HandleThumbstickMovement() {
    if (!Manipulatable)
      return;

    // Get inputs.
    Vector2 secondThumbS = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
    if (secondThumbS.x < DeadZone)
      secondThumbS.x = 0;
    if (secondThumbS.y < DeadZone)
      secondThumbS.y = 0;

    Vector3 currentPos = _selfBody.transform.position;
    Quaternion currentRot = _selfBody.transform.rotation;

    // Move accordingly.
    switch (_stickAxes) {
    case StickAxes.Lateral:
      currentPos.x += secondThumbS.x * 0.5f;
      currentPos.z += secondThumbS.y * 0.5f;
      break;
    case StickAxes.Vertical:
      currentPos.y += secondThumbS.y * 0.5f;
      break;
    default:
      throw new ArgumentOutOfRangeException();
    }

    _selfBody.TeleportRoot(currentPos, currentRot);
  }

  void FixedUpdate() {
    HandleOpacityInputs();
    HandleThumbstickClick();
    HandleThumbstickMovement();

    if (!runSimulationFile || !_robotStates.MoveNext())
      return;

    // Get next pose from sim parser.
    RobotState nextPose = _robotStates.Current;
    SetState(nextPose);
  }

  public void SetStartPosition(Vector3 newPosition) {
    _startingPosition = newPosition;
  }

  public void SetStartRotation(Quaternion newRotation) {
    _startingRotation = newRotation;
  }

  private float GetRobotHeight() {
    return _selfBody.GetComponentsInChildren<MeshRenderer>()
        .Select(c => {
          return Math.Abs(_selfBody.transform.position.y - c.bounds.min.y);
        })
        .Max();
  }

  private void SetToGround() {
    Transform transform1 = _selfBody.transform;
    Vector3 robotPos = transform1.position;
    Quaternion robotRot = transform1.rotation;

    robotPos.y = GetRobotHeight();
    _selfBody.TeleportRoot(robotPos, robotRot);
  }

  private void SwitchStickAxes() {
    switch (_stickAxes) {
    case StickAxes.Lateral:
      _stickAxes = StickAxes.Vertical;
      if (_laserPointer != null) {
        _laserPointer.startColor = Color.green;
        _laserPointer.endColor = Color.green;
      }
      break;
    case StickAxes.Vertical:
      _stickAxes = StickAxes.Lateral;
      if (_laserPointer != null) {
        _laserPointer.startColor = Color.red;
        _laserPointer.endColor = Color.blue;
      }
      break;
    default:
      throw new ArgumentOutOfRangeException();
    }
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
