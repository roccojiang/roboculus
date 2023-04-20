// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using TMPro;

// public class PositionGetterScript : MonoBehaviour
// {
//     // public Transform target;
//     // public TMP_Text text;
//     // public Vector3 MyGameObjectPosition;
//     // // Start is called before the first frame update
//     // void Start()
//     // {
        
//     // }

//     // // Update is called once per frame
//     // void Update()
//     // {
//     //     // MyGameObjectPosition = GameObject.Find("Your_Name_Here").transform.position;
//     //     MyGameObjectPosition = GameObject.Find("hexapod").transform.position;
//     //     // text.text = target.position.ToString();
//     //     text.text = MyGameObjectPosition.ToString();
//     // }
//     private GameObject hero;
//     private TMP_Text positionText;

//     private void Start()
//     {
//         // Find the Hero GameObject by name
//         // hero = GameObject.Find("base_link");

//         // Get the TextMeshPro component of the text object
//         positionText = GetComponent<TMP_Text>();
//     }

//     private void Update()
//     {
//         hero = GameObject.Find("base_link");
//         // Get the current position of the hero
//         Vector3 heroPosition = hero.transform.position;

//         // Update the text to display the hero's position
//         // positionText.text = "Hero Position: (" + heroPosition.x + ", " + heroPosition.y + ", " + heroPosition.z + ")";
//         positionText.text = heroPosition.ToString();
    
//     }
// }


using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
// using RobotControl;

public class PositionGetterScript : MonoBehaviour
{
    private TMP_Text positionText;
    private List<GameObject> heroes;

    private void Start()
    {
        // Get the TextMeshPro component of the text object
        positionText = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        // Find all Hero GameObjects in the scene and add them to the list
        GameObject[] heroObjects = GameObject.FindGameObjectsWithTag("locateme");
        heroes = new List<GameObject>(heroObjects);

        // Clear the text
        positionText.text = "";

        // Loop through all Hero GameObjects in the list
        foreach (GameObject hero in heroes)
        {
            // Get the current position of the hero
            Vector3 heroPosition = hero.transform.position;
            Quaternion heroRotation = hero.transform.rotation;

            // // Update the starting position and orientation of the hero
            // RobotControl control = hero.GetComponent<RobotControl>();
            // if (control != null) {
            //     control._startingPosition = heroPosition;
            //     control._startingRotation = heroRotation;
            //     };


            // Add the hero's position to the text
            positionText.text += hero.transform.parent.name + ":" + heroPosition.ToString() + ":" + heroRotation.ToString() +"\n";
        }
    }
}
