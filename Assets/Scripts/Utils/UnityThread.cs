#define ENABLE_UPDATE_FUNCTION_CALLBACK
#define ENABLE_LATEUPDATE_FUNCTION_CALLBACK
#define ENABLE_FIXEDUPDATE_FUNCTION_CALLBACK

using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace Utils
{
    public class UnityThread : MonoBehaviour
    {

        private List<Action> actionQueuesUpdateFunc = new ();
        List<Action> actionCopiedQueueUpdateFunc = new ();
        private volatile bool noActionQueueToExecuteUpdateFunc = true;

        private List<Action> actionQueuesLateUpdateFunc = new ();
        List<Action> actionCopiedQueueLateUpdateFunc = new ();
        private volatile bool noActionQueueToExecuteLateUpdateFunc = true;
        
        private List<Action> actionQueuesFixedUpdateFunc = new ();
        List<Action> actionCopiedQueueFixedUpdateFunc = new ();
        private volatile bool noActionQueueToExecuteFixedUpdateFunc = true;

        public void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        public void ExecuteCoroutine(IEnumerator action)
        {
            ExecuteInUpdate(() => StartCoroutine(action));
        }

        public void ExecuteInUpdate(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException();
            }

            lock (actionQueuesUpdateFunc)
            {
                actionQueuesUpdateFunc.Add(action);
                noActionQueueToExecuteUpdateFunc = false;
            }
        }

        public void Update()
        {
            if (noActionQueueToExecuteUpdateFunc)
            {
                return;
            }

            //Clear the old actions from the actionCopiedQueueUpdateFunc queue
            actionCopiedQueueUpdateFunc.Clear();
            lock (actionQueuesUpdateFunc)
            {
                //Copy actionQueuesUpdateFunc to the actionCopiedQueueUpdateFunc variable
                actionCopiedQueueUpdateFunc.AddRange(actionQueuesUpdateFunc);
                //Now clear the actionQueuesUpdateFunc since we've done copying it
                actionQueuesUpdateFunc.Clear();
                noActionQueueToExecuteUpdateFunc = true;
            }

            // Loop and execute the functions from the actionCopiedQueueUpdateFunc
            for (int i = 0; i < actionCopiedQueueUpdateFunc.Count; i++)
            {
                actionCopiedQueueUpdateFunc[i].Invoke();
            }
        }

        public void ExecuteInLateUpdate(System.Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (actionQueuesLateUpdateFunc)
            {
                actionQueuesLateUpdateFunc.Add(action);
                noActionQueueToExecuteLateUpdateFunc = false;
            }
        }

        public void LateUpdate()
        {
            if (noActionQueueToExecuteLateUpdateFunc)
            {
                return;
            }

            //Clear the old actions from the actionCopiedQueueLateUpdateFunc queue
            actionCopiedQueueLateUpdateFunc.Clear();
            lock (actionQueuesLateUpdateFunc)
            {
                //Copy actionQueuesLateUpdateFunc to the actionCopiedQueueLateUpdateFunc variable
                actionCopiedQueueLateUpdateFunc.AddRange(actionQueuesLateUpdateFunc);
                //Now clear the actionQueuesLateUpdateFunc since we've done copying it
                actionQueuesLateUpdateFunc.Clear();
                noActionQueueToExecuteLateUpdateFunc = true;
            }

            // Loop and execute the functions from the actionCopiedQueueLateUpdateFunc
            for (int i = 0; i < actionCopiedQueueLateUpdateFunc.Count; i++)
            {
                actionCopiedQueueLateUpdateFunc[i].Invoke();
            }
        }
        
        public void ExecuteInFixedUpdate(System.Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            lock (actionQueuesFixedUpdateFunc)
            {
                actionQueuesFixedUpdateFunc.Add(action);
                noActionQueueToExecuteFixedUpdateFunc = false;
            }
        }

        public void FixedUpdate()
        {
            if (noActionQueueToExecuteFixedUpdateFunc)
            {
                return;
            }

            //Clear the old actions from the actionCopiedQueueFixedUpdateFunc queue
            actionCopiedQueueFixedUpdateFunc.Clear();
            lock (actionQueuesFixedUpdateFunc)
            {
                //Copy actionQueuesFixedUpdateFunc to the actionCopiedQueueFixedUpdateFunc variable
                actionCopiedQueueFixedUpdateFunc.AddRange(actionQueuesFixedUpdateFunc);
                //Now clear the actionQueuesFixedUpdateFunc since we've done copying it
                actionQueuesFixedUpdateFunc.Clear();
                noActionQueueToExecuteFixedUpdateFunc = true;
            }

            // Loop and execute the functions from the actionCopiedQueueFixedUpdateFunc
            for (int i = 0; i < actionCopiedQueueFixedUpdateFunc.Count; i++)
            {
                actionCopiedQueueFixedUpdateFunc[i].Invoke();
            }
        }
    }
}