using UnityEngine;
using System.Collections;

public class Demo : MonoBehaviour {

    // Use this for initialization
    void Start() {
        StartCoroutine(Play());
    }

    IEnumerator Play() {

        Notifriar.EnqueueMessage("Hello");
        yield return new WaitForSeconds(1.5f);
        Notifriar.EnqueueMessage("Times called: ", 1, hashCode:"counter");
        yield return new WaitForSeconds(1.1f);
        Notifriar.EnqueueMessage("Achievement: Use Noti-Friar", spriteName:"mark");
        Notifriar.EnqueueMessage("Times called: ", 1, hashCode: "counter");
        yield return new WaitForSeconds(1);
        Notifriar.EnqueueMessage("test me 4");
        Notifriar.EnqueueMessage("Times called: ", 1, hashCode: "counter");
        yield return new WaitForSeconds(1.25f);
        Notifriar.EnqueueMessage("test me 5");
        Notifriar.EnqueueMessage("test me 5a");
        yield return new WaitForSeconds(1);
        Notifriar.EnqueueMessage("test me 6");
        yield return new WaitForSeconds(1);
        Notifriar.EnqueueMessage("test me 7");
        yield return new WaitForSeconds(1);
        Notifriar.EnqueueMessage("test me 8");
        yield return null;
    }

    void OnGUI() {
        if (GUI.Button(new Rect(10, 10, 100, 50), "Count"))
            Notifriar.EnqueueMessage("Hello World", 1, hashCode: "world");

        if (GUI.Button(new Rect(10, 70, 100, 50), "Sprite"))
            Notifriar.EnqueueMessage("sprite me", spriteName: "mark");

    }
}
