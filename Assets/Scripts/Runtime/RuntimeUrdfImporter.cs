using System.Collections;
using System.Collections.Generic;
using Runtime;
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
/// - The runtime import feature is currently only functional in builds created using
///   Mono scripting backend and will not fully work in standalone builds created with
///   IL2CPP due to the dependency to Assimpnet plugin for loading collada meshes.
///   However URDF files that only use STL format for both visual and collision meshes
///   can still be imported in runtime in standalone IL2CPP builds.  
/// </summary>
public class RuntimeUrdfImporter : MonoBehaviour
{
    public string defaultUrdfFilepath = "";
    public bool setImmovableLink = true;
    public bool useVHACD = false; // this is not yet fully functional in runtime.
    public bool showProgress = false; // this is not stable in runtime.
    public bool clearOnLoad = false;

    public string immovableLinkName = "base_link";
    public GameObject currentRobot = null;

    public void LoadUrdf(string urdfFilepath)
    {
        Debug.Log("[+] LOAD URDF: " + urdfFilepath);
        if (string.IsNullOrEmpty(urdfFilepath))
        {
            return;
        }

        // clear the existing robot to avoid collision
        if (clearOnLoad && currentRobot != null)
        {
            currentRobot.SetActive(false);
            Destroy(currentRobot);
        }
        // yield return null;

        ImportSettings settings = new ImportSettings
        {
            chosenAxis = ImportSettings.axisType.yAxis,
            convexMethod = useVHACD ? ImportSettings.convexDecomposer.vHACD : ImportSettings.convexDecomposer.unity,
        };

        GameObject robotObject = null;
        robotObject = UrdfRobotExtensions.CreateRuntime(urdfFilepath, settings);

        if (robotObject != null && robotObject.transform != null) 
        {
            robotObject.transform.SetParent(transform);
            SetControllerParameters(robotObject);
            Debug.Log("Successfully Loaded URDF" + robotObject.name);
        }
        currentRobot = robotObject;
    }

    void SetControllerParameters(GameObject robot)
    {
        Transform baseNode = robot.transform.Find(immovableLinkName);
        if (baseNode && baseNode.TryGetComponent<ArticulationBody>(out ArticulationBody baseNodeAB))
        {
            baseNodeAB.immovable = true;
            baseNodeAB.useGravity = false;
        }

        BoxCollider grabbableCollider = baseNode.gameObject.AddComponent<BoxCollider>();
        grabbableCollider.center = Vector3.zero;
        grabbableCollider.size = new Vector3(0.1f, 0.1f, 0.1f);

        if (robot.TryGetComponent<Controller>(out Controller controller))
        {
            Destroy(controller);
        }

        RobotControl control = robot.AddComponent<RobotControl>();
    }

    void Start()
    {
    }
}