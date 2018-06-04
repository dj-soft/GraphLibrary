using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Asol.Tools.WorkScheduler.Application
{
    /// <summary>
    /// Worker class : run queue requests in thread on background.
    /// Each request has 4 steps: 
    /// 1. Accept request and store it in queue (in thread of caller); 
    /// 2. Remove request from queue (in work thread on background);
    /// 3. Call target method specified in reques, and send to this method data from request (in work thread on background);
    /// 4. Get result from target method and call callback method into source of request, with result from target method.
    /// One Worker instance have one Thread.
    /// </summary>
    public class Worker
    {
        #region Worker thread and Main Loop
        public Worker()
        {
            this._WorkThreadInit();
        }
        private int _Id;
        private object _Locker;
        private List<IWorkItem> _WorkRequestList;
        private System.Threading.AutoResetEvent _Semaphore;
        private System.Threading.Thread _WorkThread;
        private bool _Running;
        private bool _Stopped;
        /// <summary>
        /// Initiate all structures for thread on background
        /// </summary>
        protected void _WorkThreadInit()
        {
            this._Id = App.GetNextId(typeof(Worker));
            this._Locker = new object();
            this._WorkRequestList = new List<IWorkItem>();
            this._Semaphore = new System.Threading.AutoResetEvent(false);
            this._Stopped = false;

            this._WorkThread = new System.Threading.Thread(_WorkThreadLoop);
            this._WorkThread.IsBackground = true;
            this._WorkThread.Name = "Worker " + this._Id.ToString();
            this._WorkThread.Priority = System.Threading.ThreadPriority.BelowNormal;
            this._WorkThread.Start();
        }
        /// <summary>
        /// Main loop of Thread-On-Background.
        /// </summary>
        protected void _WorkThreadLoop()
        {
            this._Running = true;
            while (!this._Stopped)
            {
                IWorkItem workItem = this._GetWorkItem();
                if (workItem != null && workItem.IsValid)
                {
                    this._RunningItem = workItem;
                    workItem.Run();
                    this._RunningItem.Dispose();
                    this._RunningItem = null;
                }
            }
            this._Running = false;
        }
        /// <summary>
        /// Get first workItem from this queue (from index [0] in this._WorkRequestList).
        /// When this list is empty, then this thread is "blocked" (=go to sleep) and wait to "wake-up" from Semaphore (signaled from _AddWorkItem() method).
        /// This method is allways called in Background (=Worker) Thread.
        /// </summary>
        /// <returns></returns>
        private IWorkItem _GetWorkItem()
        {
            IWorkItem workItem = null;
            while (!this._Stopped)
            {
                lock (this._Locker)
                {
                    List<IWorkItem> itemList = this._WorkRequestList;
                    if (itemList != null && itemList.Count > 0)
                        workItem = this._GetWorkItemFrom(itemList);
                }
                if (workItem != null || this._Stopped)
                    break;

                this._Semaphore.WaitOne(TimeSpan.FromSeconds(1d));
            }
            return workItem;
        }
        /// <summary>
        /// Return a "first" request (workItem) to process, from this._WorkRequestList.
        /// Accept IWorkItem.PriorityId and App.CurrentPriorityId.
        /// </summary>
        /// <param name="itemList">List of items. Has at least 1 item.</param>
        /// <returns></returns>
        private IWorkItem _GetWorkItemFrom(List<IWorkItem> itemList)
        {
            int? priorityId = App.CurrentPriorityId;
            int index = -1;
            // At first attempt: we search for IWorkItem with PriorityId equal to App.CurrentPriorityId (when App.CurrentPriorityId has value):
            if (priorityId.HasValue)
                index = itemList.FindIndex(w => w.PriorityId == priorityId.Value);
            // When App.CurrentPriorityId has not value, or itemList does not contain item with this value, we grab first item from list:
            if (index < 0)
                index = 0;

            IWorkItem item = itemList[index];
            itemList.RemoveAt(index);

            return item;
        }
        /// <summary>
        /// Add new workItem to end of this queue (Add(workItem) into this._WorkRequestList).
        /// This method is called typically in User thread (GUI).
        /// Use lock on _Locker.
        /// After release lock set signal to Semaphore, thus waiting thread on background can "wake-up" and can began work on newly inserted request.
        /// </summary>
        /// <param name="workItem"></param>
        private void _AddWorkItem(IWorkItem workItem)
        {
            if (workItem == null) return;
            lock (this._Locker)
            {
                this._WorkRequestList.Add(workItem);
            }
            this._Semaphore.Set();
        }
        private IWorkItem _RunningItem;
        #endregion
        #region AddRequests
        #region TRequest
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread.</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends.</param>
        public void AddRequest<TRequest>(Action<TRequest> workMethod, TRequest workData, Action<TRequest> callbackMethod)
        {
            this._AddWorkItem(new WorkItemRequest<TRequest>(int.MaxValue, "", workMethod, workData, callbackMethod, null));
        }
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread.</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends.</param>
        /// <param name="exceptionMethod">Exception method, will be called on Exception in Working method. Can be null.</param>
        public void AddRequest<TRequest>(Action<TRequest> workMethod, TRequest workData, Action<TRequest> callbackMethod, Action<TRequest, Exception> exceptionMethod)
        {
            this._AddWorkItem(new WorkItemRequest<TRequest>(int.MaxValue, "", workMethod, workData, callbackMethod, exceptionMethod));
        }
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <param name="priorityId">ID for priority. See also App.CurrentPriorityId property.</param>
        /// <param name="name">name of request. Can be null. Is defined only for debug and messages purposes.</param>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread.</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends.</param>
        /// <param name="exceptionMethod">Exception method, will be called on Exception in Working method. Can be null.</param>
        public void AddRequest<TRequest>(int priorityId, Action<TRequest> workMethod, TRequest workData, Action<TRequest> callbackMethod)
        {
            this._AddWorkItem(new WorkItemRequest<TRequest>(priorityId, "", workMethod, workData, callbackMethod, null));
        }
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <param name="priorityId">ID for priority. See also App.CurrentPriorityId property.</param>
        /// <param name="name">name of request. Can be null. Is defined only for debug and messages purposes.</param>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread.</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends.</param>
        /// <param name="exceptionMethod">Exception method, will be called on Exception in Working method. Can be null.</param>
        public void AddRequest<TRequest>(int priorityId, string name, Action<TRequest> workMethod, TRequest workData, Action<TRequest> callbackMethod, Action<TRequest, Exception> exceptionMethod)
        {
            this._AddWorkItem(new WorkItemRequest<TRequest>(priorityId, name, workMethod, workData, callbackMethod, exceptionMethod));
        }
        #endregion
        #region TRequest, TResponse
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <typeparam name="TResponse">Type of response</typeparam>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread. Must return response of type (TResponse)</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends. Must accept one parameter of type (TResponse)</param>
        public void AddRequest<TRequest, TResponse>(Func<TRequest, TResponse> workMethod, TRequest workData, Action<TRequest, TResponse> callbackMethod)
        {
            this._AddWorkItem(new WorkItemRequestResponse<TRequest, TResponse>(int.MaxValue, "", workMethod, workData, callbackMethod, null));
        }
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <typeparam name="TResponse">Type of response</typeparam>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread. Must return response of type (TResponse)</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends. Must accept one parameter of type (TResponse)</param>
        /// <param name="exceptionMethod">Exception method, will be called on Exception in Working method. Can be null.</param>
        public void AddRequest<TRequest, TResponse>(Func<TRequest, TResponse> workMethod, TRequest workData, Action<TRequest, TResponse> callbackMethod, Action<TRequest, Exception> exceptionMethod)
        {
            this._AddWorkItem(new WorkItemRequestResponse<TRequest, TResponse>(int.MaxValue, "", workMethod, workData, callbackMethod, exceptionMethod));
        }
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <typeparam name="TResponse">Type of response</typeparam>
        /// <param name="priorityId">ID for priority. See also App.CurrentPriorityId property.</param>
        /// <param name="name">name of request. Can be null. Is defined only for debug and messages purposes.</param>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread. Must return response of type (TResponse)</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends. Must accept one parameter of type (TResponse)</param>
        /// <param name="exceptionMethod">Exception method, will be called on Exception in Working method. Can be null.</param>
        public void AddRequest<TRequest, TResponse>(int priorityId, Func<TRequest, TResponse> workMethod, TRequest workData, Action<TRequest, TResponse> callbackMethod)
        {
            this._AddWorkItem(new WorkItemRequestResponse<TRequest, TResponse>(priorityId, "", workMethod, workData, callbackMethod, null));
        }
        /// <summary>
        /// Add a new request to process in Working queue.
        /// </summary>
        /// <typeparam name="TRequest">Type of request</typeparam>
        /// <typeparam name="TResponse">Type of response</typeparam>
        /// <param name="priorityId">ID for priority. See also App.CurrentPriorityId property.</param>
        /// <param name="name">name of request. Can be null. Is defined only for debug and messages purposes.</param>
        /// <param name="workMethod">Working method. Must accept one parameter of type (TRequest). This method will be called in background thread. Must return response of type (TResponse)</param>
        /// <param name="workData">Data for Working method.</param>
        /// <param name="callbackMethod">Callback method, will be called after Working method will ends. Must accept one parameter of type (TResponse)</param>
        /// <param name="exceptionMethod">Exception method, will be called on Exception in Working method. Can be null.</param>
        public void AddRequest<TRequest, TResponse>(int priorityId, string name, Func<TRequest, TResponse> workMethod, TRequest workData, Action<TRequest, TResponse> callbackMethod, Action<TRequest, Exception> exceptionMethod)
        {
            this._AddWorkItem(new WorkItemRequestResponse<TRequest, TResponse>(priorityId, name, workMethod, workData, callbackMethod, exceptionMethod));
        }
        #endregion
        #endregion
        #region Stop
        /// <summary>
        /// Stop this Worker.
        /// After Stop will be done current WorkItem method (this working Thread is not physically Aborted as Thread), 
        /// but will be stopped queue of waiting requests (next WorkItems are not processed).
        /// </summary>
        public void Stop()
        {
            this._Stopped = true;
            IWorkItem workItem = this._RunningItem;
            if (workItem != null)
                workItem.Stop();
            this._Semaphore.Set();
        }
        #endregion
    }
    #region WorkItemRequest<TRequest>
    /// <summary>
    /// WorkItem with Type for request, with no response
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    internal class WorkItemRequest<TRequest> : WorkItemBase, IWorkItem
    {
        #region Data
        public WorkItemRequest(int priorityId, string name, Action<TRequest> workMethod, TRequest workData, Action<TRequest> callbackMethod, Action<TRequest, Exception> exceptionMethod)
            : base(priorityId, name)
        {
            this._WorkMethod = workMethod;
            this._WorkData = workData;
            this._CallbackMethod = callbackMethod;
            this._ExceptionMethod = exceptionMethod;
        }
        protected Action<TRequest> _WorkMethod;
        protected TRequest _WorkData;
        protected Action<TRequest> _CallbackMethod;
        protected Action<TRequest, Exception> _ExceptionMethod;
        #endregion
        #region Interface IWorkItem members
        bool IWorkItem.IsValid { get { return (this._WorkMethod != null); } }
        int IWorkItem.PriorityId { get { return this._PriorityId; } }
        void IWorkItem.Run()
        {
            if (this._WorkMethod == null) return;

            try
            {
                this._State = WorkState.Processing;
                using (var scopeW = App.Trace.Scope(TracePriority.Priority1_ElementaryTimeDebug, "Worker", "Run", "WorkMethod"))
                {
                    this._WorkMethod(this._WorkData);
                    if (!this._IsStopped)
                    {
                        if (this._CallbackMethod != null)
                        {
                            using (var scopeC = App.Trace.Scope(TracePriority.Priority1_ElementaryTimeDebug, "Worker", "Run", "CallbackMethod"))
                            {
                                this._CallbackMethod(this._WorkData);
                            }
                        }
                    }
                    this._State = WorkState.Processed;
                }
            }
            catch (Exception exc)
            {
                this._State = WorkState.Error;
                App.Trace.Exception(exc, "Exception in Worker thread");
                if (!this._IsStopped)
                {
                    if (this._ExceptionMethod != null)
                    {
                        using (var scopeE = App.Trace.Scope(TracePriority.Priority1_ElementaryTimeDebug, "Worker", "Run", "ExceptionMethod"))
                        {
                            this._ExceptionMethod(this._WorkData, exc);
                        }
                    }
                }
            }
        }
        void IDisposable.Dispose()
        {
            this._WorkMethod = null;
            this._WorkData = default(TRequest);
            this._CallbackMethod = null;
            this._ExceptionMethod = null;
        }
        #endregion
    }
    #endregion
    #region WorkItemRequestResponse<TRequest, TResponse>
    /// <summary>
    /// WorkItem with Type for request and Type for response
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    internal class WorkItemRequestResponse<TRequest, TResponse> : WorkItemBase, IWorkItem
    {
        #region Data
        public WorkItemRequestResponse(int priorityId, string name, Func<TRequest, TResponse> workMethod, TRequest workData, Action<TRequest, TResponse> callbackMethod, Action<TRequest, Exception> exceptionMethod)
            : base(priorityId, name)
        {
            this._WorkMethod = workMethod;
            this._WorkData = workData;
            this._CallbackMethod = callbackMethod;
            this._ExceptionMethod = exceptionMethod;
        }
        protected Func<TRequest, TResponse> _WorkMethod;
        protected TRequest _WorkData;
        protected Action<TRequest, TResponse> _CallbackMethod;
        protected Action<TRequest, Exception> _ExceptionMethod;
        #endregion
        #region Interface IWorkItem members
        bool IWorkItem.IsValid { get { return (this._WorkMethod != null); } }
        int IWorkItem.PriorityId { get { return this._PriorityId; } }
        void IWorkItem.Run()
        {
            if (this._WorkMethod == null) return;

            try
            {
                this._State = WorkState.Processing;
                using (var scopeW = App.Trace.Scope(TracePriority.Priority1_ElementaryTimeDebug, "Worker", "Run", "WorkMethod"))
                {
                    TResponse response = this._WorkMethod(this._WorkData);
                    if (!this._IsStopped)
                    {
                        if (this._CallbackMethod != null)
                        {
                            using (var scopeC = App.Trace.Scope(TracePriority.Priority1_ElementaryTimeDebug, "Worker", "Run", "CallbackMethod"))
                            {
                                this._CallbackMethod(this._WorkData, response);
                            }
                        }
                    }
                    this._State = WorkState.Processed;
                }
            }
            catch (Exception exc)
            {
                this._State = WorkState.Error;
                App.Trace.Exception(exc, "Exception in Worker thread");
                if (!this._IsStopped)
                {
                    if (this._ExceptionMethod != null)
                    {
                        using (var scopeE = App.Trace.Scope(TracePriority.Priority1_ElementaryTimeDebug, "Worker", "Run", "ExceptionMethod"))
                        {
                            this._ExceptionMethod(this._WorkData, exc);
                        }
                    }
                }
            }
        }
        void IDisposable.Dispose()
        {
            this._WorkMethod = null;
            this._WorkData = default(TRequest);
            this._CallbackMethod = null;
            this._ExceptionMethod = null;
        }
        #endregion
    }
    #endregion
    #region WorkItemBase
    internal abstract class WorkItemBase
    {
        public WorkItemBase(int priorityId, string name)
        {
            this._RequestId = App.GetNextId(typeof(IWorkItem));
            this._PriorityId = priorityId;
            this._Name = name;
            this._State = WorkState.Waiting;
        }
        public override string ToString()
        {
            return "RequestId: " + this._RequestId.ToString() + "; Name: " + (this._Name == null ? "" : this._Name) + "; State: " + this._State.ToString();
        }
        protected int _RequestId;
        protected int _PriorityId;
        protected string _Name;
        protected WorkState _State;
        protected bool _IsStopped;
        /// <summary>
        /// Stop this request when running. Does not call any callback nor exception handler.
        /// </summary>
        public void Stop()
        {
            this._IsStopped = true;
        }
    }
    #endregion
    #region Interface IWorkItem; enum WorkState
    /// <summary>
    /// Interface for one WorkItem, working in background thread
    /// </summary>
    internal interface IWorkItem : IDisposable
    {
        /// <summary>
        /// true when this item is valid (can call Run() method)
        /// </summary>
        bool IsValid { get; }
        /// <summary>
        /// ID of priority, by this priority can be sorted Queue of WorkItems
        /// </summary>
        int PriorityId { get; }
        /// <summary>
        /// Run this request
        /// </summary>
        void Run();
        /// <summary>
        /// Stop this request when running. Does not call any callback nor exception handler.
        /// </summary>
        void Stop();
    }
    public enum WorkState
    {
        None,
        Waiting,
        Processing,
        Processed,
        Error
    }
    #endregion
}
