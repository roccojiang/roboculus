using System.Collections.Concurrent;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Runtime {
public class MenuController : MonoBehaviour {
  public GameObject urdfButton;
  public GameObject urdfMenuBackground;
  public GameObject urdfMenu;
  public RuntimeUrdfImporter urdfImporter;

  public GameObject popupWindow;
  public TextMeshProUGUI popupTextField;
  private bool _popupIsShown = false;

  public GameObject eventCamera;

  private HashSet<string> _urdfs = new();
  private ConcurrentQueue<string> _urdfButtonPaths = new();

  private const int DISTANCE_FROM_CAMERA = 3;
  private const char PATH_SEPARATOR = '/';

  void Start() {
    UrdfServer.TriggerPopupWindow += ShowPopupWindow;
    UrdfServer.OnUrdfUpload += RefreshMenu;
    Server.TriggerPopupWindow += ShowPopupWindow;

    RefreshMenu(Application.persistentDataPath);
  }

  void Update() {
    string urdfPath;
    while (_urdfButtonPaths.TryDequeue(out urdfPath))
      AddUrdfButton(urdfPath);

    // Disable opening URDF menu if there is a popup
    if (_popupIsShown)
      return;

    // Open menu with either (right hand) 'B' button or (left) menu button
    if (OVRInput.GetUp(OVRInput.Button.Two) ||
        OVRInput.GetUp(OVRInput.Button.Start)) {
      transform.position =
          eventCamera.transform.position +
          (eventCamera.transform.forward * DISTANCE_FROM_CAMERA);
      transform.rotation = eventCamera.transform.rotation;
      // Keep menu upright
      transform.rotation =
          Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0);

      urdfMenu.SetActive(!urdfMenu.activeSelf);
    }
  }

  private void ShowPopupWindow(string msg) {
    _popupIsShown = true;
    urdfMenu.SetActive(false);
    popupTextField.text = msg;
    popupWindow.SetActive(true);
  }

  public void ClosePopupWindow() {
    popupWindow.SetActive(false);
    _popupIsShown = false;
  }

  private void RefreshMenu(string applicationDataStore) {
    Debug.Log("[+] Starting menu controller!");

    string[] robots =
        Directory.GetDirectories(applicationDataStore + PATH_SEPARATOR);
    print(robots.ToString());
    print(_urdfs.ToString());
    foreach (string robot in robots) {
      foreach (string path in Directory.GetFiles(robot, "*.urdf")) {
        print(path);

        if (_urdfs.Add(path)) {
          _urdfButtonPaths.Enqueue(path);
        }
      }
    }
  }

  private void AddUrdfButton(string path) {
    var newButton =
        Instantiate(urdfButton, new Vector3(0, 0, 0), Quaternion.identity);
    var buttonRect = newButton.GetComponent<RectTransform>();
    buttonRect.SetParent(urdfMenuBackground.transform, false);
    buttonRect.localScale = new Vector3(1, 1, 1);

    // urdf is top level in directory where the robot name is directory name
    // TODO: Depending on how paths are returned on oculus this may need to be
    // changed
    var textObject = buttonRect.GetComponentInChildren<TextMeshProUGUI>();
    textObject.text = path.Split(PATH_SEPARATOR)[^2];

    newButton.GetComponent<Button>().onClick.AddListener(() => {
      Debug.Log("IMPORTING ROBOT");
      Debug.Log("urdf object:" + urdfImporter);
      urdfImporter.LoadUrdf(path);
      urdfMenu.SetActive(false);
    });
  }
}
}