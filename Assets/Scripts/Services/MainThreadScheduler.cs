using System;
using System.Collections;
using UnityEngine;
using Utils;

namespace Services
{
    public class MainThreadScheduler : IService
    {
        private UnityThread thread;
        
        public MainThreadScheduler()
        {
            GameObject obj = new GameObject("MainThreadScheduler");
            //obj.hideFlags = HideFlags.HideAndDontSave;
            thread = obj.AddComponent<UnityThread>();
        }

        public void ExecuteCoroutine(IEnumerator coroutine)
        {
            thread.ExecuteCoroutine(coroutine);
        }

        public void ExecuteInUpdate(Action action)
        {
            thread.ExecuteInUpdate(action);
        }
        
        public void ExecuteInFixedUpdate(Action action)
        {
            thread.ExecuteInFixedUpdate(action);
        }
        
        public void ExecuteInLateUpdate(Action action)
        {
            thread.ExecuteInLateUpdate(action);
        }
    }
}