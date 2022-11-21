using UnityEngine;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Runtime {
public class MenuController : MonoBehaviour {
  public GameObject urdfButton;
  public GameObject menuBackground;
  public RuntimeUrdfImporter urdfImporter;

  private HashSet<string> urdfs = new();

  private const int BUTTON_SEPARATION = 80;
  private const char PATH_SEPARATOR = '/';

  private int yOffset = -BUTTON_SEPARATION / 2;

  // Start is called before the first frame update
  void Start() {}

  // Update is called once per frame
  void Update() {}

  public void UpdateMenu() {
    Debug.Log("[+] Starting menu controller!");

    string[] robots = Directory.GetDirectories(Application.persistentDataPath +
                                               PATH_SEPARATOR);
    print(robots.ToString());
    print(urdfs.ToString());
    foreach (string robot in robots) {
      foreach (string path in Directory.GetFiles(robot, "*.urdf")) {
        print(path);

        if (urdfs.Add(path)) {
          AddUrdfButton(path);
        }
      }
    }
  }

  private void AddUrdfButton(string path) {
    print("path: " + path + " at offset " + yOffset);
    var newButton =
        Instantiate(urdfButton, new Vector3(0, 0, 0), Quaternion.identity);
    var buttonRect = newButton.GetComponent<RectTransform>();
    buttonRect.SetParent(menuBackground.transform);

    RectTransform scrollView = buttonRect.parent.parent.parent as RectTransform;
    buttonRect.SetLocalPositionAndRotation(
        new Vector3(scrollView.rect.width / 2, yOffset, 0),
        Quaternion.identity);
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
    });

    yOffset -= BUTTON_SEPARATION;
  }
}
}