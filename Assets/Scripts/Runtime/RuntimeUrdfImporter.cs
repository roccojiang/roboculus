using System;
using Runtime;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Robotics.UrdfImporter;
using Unity.Robotics.UrdfImporter.Control;
using UnityEngine;

/// <summary>
/// Example component for using the runtime urdf import funcionality.
/// To use, attach to a gameobject and use the GUI to load a URDF.
///
/// Notes:
///
/// - This component is for demonstration and testing only
///   and is not intended to be used as is in a final product.
///
/// - The runtime import feature is currently only functional in builds created
/// using
///   Mono scripting backend and will not fully work in standalone builds
///   created with IL2CPP due to the dependency to Assimpnet plugin for loading
///   collada meshes. However URDF files that only use STL format for both
///   visual and collision meshes can still be imported in runtime in standalone
///   IL2CPP builds.
/// </summary>
public class RuntimeUrdfImporter : MonoBehaviour {
  public string defaultUrdfFilepath = "";
  public bool setImmovableLink = true;
  public bool useVHACD = false; // this is not yet fully functional in runtime.
  public bool showProgress = false; // this is not stable in runtime.
  public bool clearOnLoad = false;

  public string immovableLinkName = "base_link";
  public GameObject currentRobot = null;

  public static event Action<GameObject, string> RuntimeRobotImported;

  public int counter = 0;

  public void LoadUrdf(string urdfFilepath) {
    counter++;
    Debug.Log("[+] LOAD URDF: " + urdfFilepath);
    if (string.IsNullOrEmpty(urdfFilepath)) {
      return;
    }

    // yield return null;

    ImportSettings settings = new ImportSettings {
      chosenAxis = ImportSettings.axisType.yAxis,
      convexMethod = useVHACD ? ImportSettings.convexDecomposer.vHACD
                              : ImportSettings.convexDecomposer.unity,
    };

    GameObject robotObject = null;
    robotObject = UrdfRobotExtensions.CreateRuntime(urdfFilepath, settings);

    if (robotObject != null && robotObject.transform != null) {
      int jointCount = CountJoints(
          urdfFilepath); // This will be parsed from the URDF file instead
      robotObject.transform.SetParent(transform);

      // SetControllerParameters(robotObject, jointCount, urdfFilepath);
      SetControllerParameters(robotObject, jointCount, urdfFilepath);

      // robotObject.name = robotObject.name + counter.ToString();

      Debug.Log("Successfully Loaded URDF " + robotObject.name);
    }

    // clear the existing robot to avoid collision (works - comment this to load multiple thing, then may have server issue)
    if (clearOnLoad && currentRobot.name == "hexapod") {
      // currentRobot.SetActive(false);
      // Destroy(currentRobot);
      Server server = currentRobot.GetComponent<Server>();
      if (server != null) {Destroy(server);}

    }

    currentRobot = robotObject;
    RuntimeRobotImported?.Invoke(currentRobot, immovableLinkName);

    // Can try to comment out if server started in params
    Server newServer = currentRobot.GetComponent<Server>();
    newServer.StopServer();
    newServer.Start();
  }

  private int CountJoints(string filePath) {
    string contents = File.ReadAllText(filePath, Encoding.UTF8);
    return Regex.Matches(contents, @"<joint.*>").Count;
  }

  // string urdfFilepath as third param
  void SetControllerParameters(GameObject robot, int jointCount, string urdfFilepath) {
    ArticulationBody baseNode = robot.GetComponentInChildren<ArticulationBody>();
    if (baseNode) {
      baseNode.immovable = true;
      baseNode.useGravity = false;

      BoxCollider grabbableCollider =
      baseNode.gameObject.AddComponent<BoxCollider>();
      grabbableCollider.center = Vector3.zero;
      grabbableCollider.size = new Vector3(0.1f, 0.1f, 0.1f);

      immovableLinkName = baseNode.gameObject.name;
      // attach locateme tag to the object
      baseNode.gameObject.tag = "locateme";
    }

    // Destroy any pre-loaded controller if it exists
    if (robot.TryGetComponent<Controller>(out Controller controller)) {
      Destroy(controller);
    }

    RobotControl control = robot.AddComponent<RobotControl>();
    control.jointCount = jointCount;

    Server oldServer = currentRobot.GetComponent<Server>();

    if (urdfFilepath.Contains("bot")){
      Server newServer = robot.AddComponent<Server>();

      System.Type type = oldServer.GetType();
      // Copied fields can be restricted with BindingFlags
      System.Reflection.FieldInfo[] fields = type.GetFields();
      foreach (System.Reflection.FieldInfo field in fields) {
        field.SetValue(newServer, field.GetValue(oldServer));
      }
      newServer.StopServer();
      newServer.Start();
      
      }

    foreach (ArticulationBody childBody in robot.GetComponentsInChildren<ArticulationBody>())
    {
      childBody.useGravity = false;
    }

  //   if (urdfFilepath.Contains("bot")){
  //     newServer.StopServer();
  //     newServer.Start();
  // }

  }
}
