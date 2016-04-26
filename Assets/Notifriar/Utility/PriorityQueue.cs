using System.Collections.Generic;

public class PriorityQueue<T> {

    SortedDictionary<int, Queue<T>> messageQueue;
    int queueSize;

    public int Count {
        get { return queueSize; }
    }

    public PriorityQueue() {
        messageQueue = new SortedDictionary<int, Queue<T>>();
        queueSize = 0;
    }

    public void Enqueue(int priority, T item) {
        priority = priority >= 0 ? priority : 0;
        if (!messageQueue.ContainsKey(priority))
            messageQueue.Add(priority, new Queue<T>());
        messageQueue[priority].Enqueue(item);
        queueSize += 1;
    }

    /// <summary>
    /// Pop highest priority item from message Queue
    /// </summary>
    /// <returns></returns>
    public T Pop() {
        if (queueSize == 0)
            throw new System.InvalidOperationException("Queue is empty.");
        foreach (Queue<T> q in messageQueue.Values)
        {
            if (q.Count > 0)
            {
                queueSize -= 1;
                return q.Dequeue();
            }
        }
        throw new System.InvalidOperationException("No items found.");
    }

    public IEnumerable<int> Keys {
        get {
            foreach (int p in messageQueue.Keys)
                yield return p;
        }
    }

    public int HighestPriority {
        get {
            int highestPriority = 0;
            foreach (int p in messageQueue.Keys)
                highestPriority = p > highestPriority ? p : highestPriority;
            return highestPriority;
        }
    }

    /// <summary>
    /// Remove lowest priority item from message Queue
    /// </summary>
    /// <returns></returns>
    public T PopLast() {
        Queue<T> q = messageQueue[HighestPriority];
        T first = q.Peek();
        T current = default(T);
        while (true)
        {
            current = q.Dequeue();
            if (EqualityComparer<T>.Default.Equals(q.Peek(), first))
                break;
            q.Enqueue(current);
        }
        queueSize -= 1;
        return current;
    }

}
