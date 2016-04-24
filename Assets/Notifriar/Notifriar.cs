using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Notifriar : MonoBehaviour {

    public static Notifriar _instance { get; private set; }

    [Tooltip("Prefab used as 'popup'. Must include 'popUp.cs' on root.")]
    public GameObject popupPrefab;
    [Header("Queue sizes")]
    [Tooltip("Max number of notifications on screen at once.")]
    public int maxDisplaySize = 4;
    [Tooltip("Max number of notifications to queue before ignoring/replacing.")]
    public int maxQueueSize = 40;
    [Tooltip("Speed to move popups to 'higher' location. 0 to disable.")]
    [Range(0, 10)]
    public float collapseSpeed = 1;
    [Tooltip("When queue is full, drop mesages with this priority to make room for higher priority.")]
    public int replaceAtPriority = 10;

    [Header("Display location")]
    [Tooltip("Transform to use as popup parent. Default is self.")]
    public Transform spawnAtParent;
    [Tooltip("Offset from parent.")]
    public Vector2 locationOffset;
    [Tooltip("Offset for each new notification.")]
    public Vector2 offset = new Vector2(0, 20);
    [Tooltip("In case unit scale (NGUI) is not 1.")]
    public float unitScale = 1;
    
    // Data for the popup. A class such that data may be updated.
    public class PopupData {
        // A given reference name for the data/popup
        public string hashCode;
        public int index;

        public int priority;
        public string message;

        // If given a numeric value, show it
        public bool hasVal;
        public int val;
        // Name of icon to display with popup
        public string spriteName;
        public popUp p;

        public PopupData(string message, int priority=5, bool hasVal=false, int val=0, string spriteName = null) {
            this.priority = priority;
            this.message = message;
            this.hasVal = hasVal;
            this.val = val;
            this.hashCode = null;
            this.spriteName = spriteName;
            p = null;
            index = -1;
        }

        public void UpdateValue(int v, bool setTo=false) {
            val += v;
            if (setTo)
                val = v;
            if (p != null)
                p.UpdateMessage();
        }
    }

    public Dictionary<string, PopupData> dataDict = new Dictionary<string, PopupData>();
    // Displayed message array
    popUp[] activeMessages;
    int usedSlots = 0;
    // Message Queue
    PriorityQueue<PopupData> messageQueue = new PriorityQueue<PopupData>();

    /// <summary>
    /// Push MESSAGE of PRIORITY X to the notification queue. Static call to AddMessage.
    /// </summary>
    /// <param name="message">String to display.</param>
    /// <param name="value">Value to show/increment notification by.</param>
    /// <param name="priority">Importance of notification.</param>
    /// <param name="hashCode">Use to later reference the notification (increment pickups? change message? your call)</param>
    /// <param name="spriteName">Name of sprite to load and display in notifcation.</param>
    /// <returns></returns>
    static public bool EnqueueMessage(string message, int value = 0, int priority = 5, string hashCode = null, string spriteName = null, bool setValue = false) {
        return _instance.AddMessage(message, value, priority, hashCode, spriteName);
    }

    /// <summary>
    /// Push MESSAGE of PRIORITY X to the notification queue.
    /// </summary>
    /// <param name="message">String to display.</param>
    /// <param name="value">Value to show/increment notification by.</param>
    /// <param name="priority">Importance of notification.</param>
    /// <param name="hashCode">Use to later reference the notification (increment pickups? change message? your call)</param>
    /// <param name="spriteName">Name of sprite to load and display in notifcation.</param>
    /// <returns></returns>
    public bool AddMessage(string message, int value=0, int priority = 5, string hashCode=null, string spriteName=null, bool setValue=false) {
        if (hashCode != null && dataDict.ContainsKey(hashCode))
        {
            dataDict[hashCode].UpdateValue(value, setValue);
            return true;
        }
        if (messageQueue.Count >= maxQueueSize) {
            if (priority >= replaceAtPriority)
                return false;
            // Attempt to remove lowest priority
            if (messageQueue.HighestPriority < replaceAtPriority || messageQueue.PopLast() == null)
                return false;
        }
        bool hasVal = value != 0;
        PopupData d = new PopupData(message, priority, hasVal, value, spriteName);
        if (hashCode != null)
        {
            d.hashCode = hashCode;
            dataDict.Add(hashCode, d);
        }
        messageQueue.Enqueue(priority, d);
        pushMessage();
        return true;
    }

    /// <summary>
    /// Remove popup p from active queue. Called by PopUp
    /// </summary>
    /// <param name="p">Popup within activeMessages to remove.</param>
    public void removeActiveMessage(popUp p) {
        if (usedSlots == 0)
            return;
        usedSlots -= 1;
        int id = p.data.index;
        activeMessages[id] = null;
        string hashCode = p.data.hashCode;
        if (hashCode != null)
            dataDict.Remove(hashCode);
        Destroy(p.gameObject);
        if (collapseSpeed > 0)
            CollapsePopUps(id);
        pushMessage();
    }
    
    /// <summary>
    /// Move active messages to lower priority slots if open.
    /// </summary>
    /// <param name="id"></param>
    void CollapsePopUps(int id) {
        Vector3 moveOffset = (Vector3)locationOffset / unitScale;
        for (int i = id; i < activeMessages.Length; i++)
        {
            if (activeMessages[i] == null)
                continue;
            for (int j = 0; j < i; j++)
            {
                if (activeMessages[j] == null)
                {
                    activeMessages[j] = activeMessages[i];
                    activeMessages[i] = null;
                    Vector3 target = ((Vector3)offset / unitScale) * j + moveOffset;
                    activeMessages[j].MoveTo(target, collapseSpeed/(i-j), j);
                }
            }
        }
    }

    /// <summary>
    /// Display next message in queue.
    /// </summary>
    public void pushMessage() {
        if (messageQueue.Count == 0 || usedSlots == maxDisplaySize)
            return;
        int id = 0;
        for (int i = 0; i < maxDisplaySize; i++)
            if (activeMessages[i] == null)
            {
                id = i;
                break;
            }
        PopupData data = messageQueue.Pop();
        data.index = id;
        GameObject g = Instantiate(popupPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        popUp p = g.GetComponent<popUp>().Spawn(this, data);
        Vector3 at = (Vector3)locationOffset / unitScale;
        at += ((Vector3)offset / unitScale) * id;
        p.Parent(spawnAtParent, at);
        activeMessages[id] = p;
        usedSlots += 1;
    }

    void setup() {
        if (spawnAtParent == null)
            spawnAtParent = transform;
        activeMessages = new popUp[maxDisplaySize];
        for (int i = 0; i < maxDisplaySize; i++)
            activeMessages[i] = null;
    }

    void Awake() {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Debug.LogWarning(this + " already exists, deleting.");
            Destroy(this.gameObject);
        }
        setup();
    }

    void OnDestroy() {
        if (_instance == this) _instance = null;
    }

    void OnDrawGizmosSelected() {
        Vector3 position = spawnAtParent.position + (Vector3)locationOffset / unitScale;
        Vector3 offsetPosition = position + (Vector3)offset / unitScale;

        Gizmos.color = Color.red;
        BKB_Drawer.DrawCross(offsetPosition, 10f / unitScale);
        Gizmos.DrawLine(position, offsetPosition);

        Gizmos.color = Color.blue;
        BKB_Drawer.DrawCross(position, 10f / unitScale);
    }

    // TODO -
    // Flag to override notification time
    // ITickable
    //  Notifriar master 'TICKS' popup data. 
    //      Popup - all it does:
    //          spawn
    //          move
    //          despawn
    // Drawer class
}
