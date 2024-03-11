using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class is used to execute actions on the main thread from other threads.
// This is useful when you want to update the UI from a background thread.
public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> executeOnMainThread = new Queue<Action>();
    private static MainThreadDispatcher instance;

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MainThreadDispatcher>();
                if (instance == null)
                {
                    GameObject dispatcherGameObject = new GameObject("MainThreadDispatcher");
                    instance = dispatcherGameObject.AddComponent<MainThreadDispatcher>();
                }
            }
            return instance;
        }
    }

    public static void ExecuteOnMainThread(Action action)
    {
        if (action == null)
        {
            return;
        }

        lock (executeOnMainThread)
        {
            executeOnMainThread.Enqueue(action);
        }
    }

    private void Update()
    {
        while (executeOnMainThread.Count > 0)
        {
            Action action = null;

            lock (executeOnMainThread)
            {
                if (executeOnMainThread.Count > 0)
                {
                    action = executeOnMainThread.Dequeue();
                }
            }

            action?.Invoke();
        }
    }

    public void write(string message)
    {
        Debug.Log(message);
    }
}
