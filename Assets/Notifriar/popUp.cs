using UnityEngine;
using System.Collections;

public class popUp : MonoBehaviour {

    public Animator anim;
    // Modify to suit UI (NGUI vs. UI vs. whatever)
    public UnityEngine.UI.Text textLabel;
    public UnityEngine.UI.Text countLabel;
    public UnityEngine.UI.Image sprite;

    [Space()]
    public AnimationClip inAnimation;
    public AnimationClip outAnimation;
    [Tooltip("Time display popup.")]
    public float timeWait = 2;

    public Notifriar.PopupData data;

    float startTime = 0;
    Notifriar master;

    public popUp Spawn(Notifriar nm, Notifriar.PopupData pData) {
        if (anim == null)
            anim = GetComponent<Animator>();
        master = nm;
        data = pData;
        data.p = this;
        gameObject.name = "popup_" + data.index;
        gameObject.SetActive(true);
        UpdateMessage();
        inAnimation.wrapMode = WrapMode.Once;
        outAnimation.wrapMode = WrapMode.Once;
        anim.Play(inAnimation.name);
        StartCoroutine(Play());
        return this;
    }

    // Modify to suit UI (NGUI vs. UI vs. whatever)
    public void Parent(Transform parent, Vector3 offset) {
        //gameObject.transform.parent = parent;
        transform.SetParent(parent, true);
        gameObject.transform.position = parent.position + offset;
        transform.localScale = Vector3.one;
    }

    // Modify to suit UI (NGUI vs. UI vs. whatever)
    public void UpdateMessage() {
        textLabel.text = data.message;
        if (data.hasVal)
        {
            if (countLabel != null)
                countLabel.text = data.val.ToString();
            else
                textLabel.text = data.message + " " + data.val.ToString();
        }
        else if (countLabel != null)
            countLabel.text = "";
        if (sprite != null)
        {
            if (data.spriteName != null)
            {
                // Enable Sprite and change texture/source image
                sprite.enabled = true;
                sprite.sprite = Resources.Load<Sprite>(data.spriteName);
            }
            else
                sprite.enabled = false;
        }
        timeWait = 1;
    }

    // Modify to suit UI (NGUI vs. UI vs. whatever)
    public void MoveTo(Vector3 position, float seconds, int newId) {
        data.index = newId;
        gameObject.name = "popup_" + data.index;
        startTime = Time.time;
        //transform.localPosition = position;
        StartCoroutine(Move(position, seconds));
    }

    IEnumerator Move(Vector3 position, float seconds) {
        Vector3 start = transform.localPosition;
        while (startTime + seconds > Time.time)
        {
            transform.localPosition = Vector3.Lerp(start, position, (Time.time - startTime) / seconds);
            yield return null;
        }
        transform.localPosition = position;
    }


    IEnumerator Play() {
        yield return new WaitForSeconds(inAnimation.length);
        while (timeWait > 0)
        {
            float wait = timeWait;
            timeWait = 0;
            yield return new WaitForSeconds(wait);
        }
        anim.Play(outAnimation.name);
        yield return new WaitForSeconds(outAnimation.length);
        master.removeActiveMessage(this);
    }

}
