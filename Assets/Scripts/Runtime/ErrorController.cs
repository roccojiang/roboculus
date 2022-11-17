using UnityEngine;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Runtime {
public class ErrorController : MonoBehaviour {

  public GameObject textObject;
  public TextMeshProUGUI textField;

  // Start is called before the first frame update
  void Start() {
    Debug.Log("[+] Starting error controller!");
    this.gameObject.SetActive(false);
  }

  // Update is called once per frame
  void Update() {}
}
}