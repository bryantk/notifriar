using UnityEngine;
using System.Collections;

public class PopUp : MonoBehaviour {

    public Animator anim;
    // Modify to suit UI (NGUI vs. UI vs. whatever)
    public UnityEngine.UI.Text textLabel;
    public UnityEngine.UI.Text countLabel;
    public UnityEngine.UI.Image sprite;

    [Space()]
    public AnimationClip inAnimation;
    public AnimationClip outAnimation;

    public Notifriar.PopupData data;

    public PopUp Spawn(Notifriar.PopupData pData) {
        if (anim == null)
            anim = GetComponent<Animator>();
        data = pData;
        data.p = this;
        gameObject.name = "popup_" + data.index;
        gameObject.SetActive(true);
        UpdateMessage();
        inAnimation.wrapMode = WrapMode.Once;
        outAnimation.wrapMode = WrapMode.Once;
        // Accoutn for 'in' animation length
        data.TTL += inAnimation.length;
        anim.Play(inAnimation.name);
        return this;
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
    }

    // Modify to suit UI (NGUI vs. UI vs. whatever)
    public void MoveTo(Vector3 position, float seconds, int newId) {
        data.index = newId;
        gameObject.name = "popup_" + data.index;
        StartCoroutine(Move(position, seconds));
    }

    IEnumerator Move(Vector3 position, float seconds) {
        Vector3 start = transform.localPosition;
        float startTime = Time.time;
        while (startTime + seconds > Time.time)
        {
            transform.localPosition = Vector3.Lerp(start, position, (Time.time - startTime) / seconds);
            yield return null;
        }
        transform.localPosition = position;
    }

}
