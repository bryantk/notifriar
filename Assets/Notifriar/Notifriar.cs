using UnityEngine;
using System.Collections.Generic;

public class Notifriar : MonoBehaviour, ITick {

    public static Notifriar _instance { get; private set; }

    [Tooltip("Prefab used as 'popup'. Must include 'popUp.cs' on root.")]
    public GameObject popupPrefab;
    public float defaultTTL = 2;
    [Header("Queue sizes")]
    [Tooltip("Max number of notifications on screen at once.")]
    public int maxDisplaySize = 4;
    [Tooltip("Max number of notifications to queue before ignoring/replacing.")]
    public int maxQueueSize = 40;
    [Tooltip("Speed to move popups to 'higher' location. 0=Don't move, higher=slower.")]
    [Range(0, 2)]
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
        // Required to run NotiFriar
        public string hashCode;
        public int index;
        public PopUp p;
        public float TTL;
        public bool alive;

        // Add what you want here
        public int priority;
        public string message;
        // If given a numeric value, show it
        public bool hasVal;
        public int val;
        // Name of icon to display with popup
        public string spriteName;

        public PopupData(string message, int priority=5, bool hasVal=false, int val=0, string spriteName = null, float ttl=-1, string hashCode=null) {
            this.priority = priority;
            this.message = message;
            this.hasVal = hasVal;
            this.val = val;
            this.hashCode = hashCode;
            this.spriteName = spriteName;
            p = null;
            index = -1;
            ttl = ttl > 0 ? ttl : 1;
            TTL = ttl;
            alive = true;
        }

        public void UpdateValue(int v, bool setTo=false, float ttl=-1, bool doNow=true) {
            val += v;
            if (setTo)
                val = v;
            if (ttl > 0)
                TTL = ttl;
            if (doNow && p != null)
                p.UpdateMessage();
        }
    }

    public Dictionary<string, PopupData> dataDict = new Dictionary<string, PopupData>();
    // Displayed message array
    PopUp[] activeMessages;
    int usedSlots = 0;
    // Track if a notification was removed this Tick
    bool modified = false;
    // Message Queue
    PriorityQueue<PopupData> messageQueue = new PriorityQueue<PopupData>();

    bool paused = false;
    bool hideOnPause = false;

    /// <summary>
    /// Push MESSAGE of PRIORITY X to the notification queue. Static call to AddMessage.
    /// </summary>
    /// <param name="message">String to display.</param>
    /// <param name="value">Value to show/increment notification by.</param>
    /// <param name="priority">Importance of notification.</param>
    /// <param name="hashCode">Use to later reference the notification (increment pickups? change message? your call)</param>
    /// <param name="spriteName">Name of sprite to load and display in notifcation.</param>
    /// <returns></returns>
    static public bool EnqueueMessage(string message, int value = 0, int priority = 5, string hashCode = null, string spriteName = null, bool setValue = false, float ttl = -1) {
        return _instance.AddMessage(message, value, priority, hashCode, spriteName, setValue, ttl);
    }

    static public void Pause(bool hide=false) {
        _instance.hideOnPause = hide;
        _instance.IPause();
    }

    static public void Resume() {
        _instance.IResume();
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
    public bool AddMessage(string message, int value=0, int priority = 5, string hashCode=null, string spriteName=null, bool setValue=false, float ttl=-1) {
        if (ttl <= 0)
            ttl = defaultTTL;
        if (hashCode != null && dataDict.ContainsKey(hashCode))
        {
            // If paused: store this for later
            dataDict[hashCode].UpdateValue(value, setValue, ttl, !paused);
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
        PopupData d = new PopupData(message, priority, hasVal, value, spriteName, ttl);
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
    public void removeActiveMessage(PopUp p) {
        if (usedSlots == 0)
            return;
        usedSlots -= 1;
        int id = p.data.index;
        activeMessages[id] = null;
        Destroy(p.gameObject);
    }
    
    /// <summary>
    /// Move active messages to lower priority slots if open.
    /// </summary>
    /// <param name="id"></param>
    void CollapsePopUps() {
        Vector3 moveOffset = (Vector3)locationOffset / unitScale;
        int lowestPossible = 0;
        for (int i = 1; i < activeMessages.Length; i++)
        {
            if (activeMessages[i] == null)
                continue;
            
            for (int j = lowestPossible; j < i; j++)
            {
                if (activeMessages[j] == null)
                {
                    lowestPossible = j+1;
                    Vector3 target = ((Vector3)offset / unitScale) * j + moveOffset;
                    activeMessages[i].MoveTo(target, collapseSpeed, j);
                    activeMessages[j] = activeMessages[i];
                    activeMessages[i] = null;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Display next message in queue.
    /// </summary>
    public void pushMessage() {
        if (messageQueue.Count == 0 || usedSlots == maxDisplaySize || paused)
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
        PopUp p = g.GetComponent<PopUp>().Spawn(data);
        Vector3 at = (Vector3)locationOffset / unitScale;
        at += ((Vector3)offset / unitScale) * id;
        //Parent and set notification location
        g.transform.SetParent(spawnAtParent);
        g.transform.position = spawnAtParent.position + at;
        g.transform.localScale = Vector3.one;

        activeMessages[id] = p;
        usedSlots += 1;
    }

    void TickPopup(PopUp p, float dt) {
        p.data.TTL -= dt;
        if (p.data.TTL <= 0)
        {
            if (p.data.alive)
            {
                if (p.data.hashCode != null)
                    dataDict.Remove(p.data.hashCode);
                p.data.TTL = p.outAnimation.length;
                p.data.alive = false;
                p.anim.Play(p.outAnimation.name);
            }
            else
            {
                removeActiveMessage(p);
                modified = true;
            }
        }
    }

    public void ITick() {
        if (paused)
            return;
        modified = false;
        float dt = Time.deltaTime;
        for (int i = 0; i < activeMessages.Length; i++)
        {
            if (activeMessages[i] == null)
                continue;
            TickPopup(activeMessages[i], dt);
        }
        if (modified && collapseSpeed > 0)
            CollapsePopUps();
        while (messageQueue.Count > 0 && usedSlots < maxDisplaySize)
        {
            pushMessage();
        }
    }

    public void IPause() {
        paused = true;
        for (int i = 0; i < activeMessages.Length; i++)
        {
            if (activeMessages[i] == null)
                continue;
            activeMessages[i].anim.speed = 0;
            if (hideOnPause)
                activeMessages[i].gameObject.transform.GetChild(0).gameObject.SetActive(false);
        }
    }

    public void IResume() {
        if (!paused)
            return;
        // pop queued 'hashes'
        paused = false;
        for (int i = 0; i < activeMessages.Length; i++)
        {
            if (activeMessages[i] == null)
                continue;
            activeMessages[i].anim.speed = 1;
            activeMessages[i].gameObject.transform.GetChild(0).gameObject.SetActive(true);
            activeMessages[i].UpdateMessage();
        }
    }

    void setup() {
        if (spawnAtParent == null)
            spawnAtParent = transform;
        activeMessages = new PopUp[maxDisplaySize];
        for (int i = 0; i < maxDisplaySize; i++)
            activeMessages[i] = null;
    }

    void Update() {
        // Can be run less often if needed.
        ITick();
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
    // documentation
}
