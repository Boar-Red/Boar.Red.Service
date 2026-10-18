using BaoXia.Utils;

namespace Boar.Red.Service.BrService.ViewModels
{
	public class ServiceNeedFoundInfo
	{
		////////////////////////////////////////////////
		// @自身属性
		////////////////////////////////////////////////

		#region 自身属性

		/// <summary>
		/// 服务分组名称。
		/// </summary>
		public string? GroupName { get; set; }

		/// <summary>
		/// 服务名称。
		/// </summary>
		public string ServiceName { get; set; }


		/// <summary>
		/// 要接收服务信息变动的任务信号源。
		/// </summary>
		private List<TaskCompletionSource<ServiceNeedFoundInfo>>? _taskCompletionSourcesToDidServiceInfoUpdated;

		protected List<TaskCompletionSource<ServiceNeedFoundInfo>> TaskCompletionSourcesToDidServiceInfoUpdated
		{
			get
			{
				if (_taskCompletionSourcesToDidServiceInfoUpdated != null)
				{
					return _taskCompletionSourcesToDidServiceInfoUpdated;
				}
				lock (this)
				{
					_taskCompletionSourcesToDidServiceInfoUpdated ??= new();
				}
				return _taskCompletionSourcesToDidServiceInfoUpdated;
			}
		}

		/// <summary>
		/// 要接收服务信息变动的回调。
		/// </summary>
		private List<Action<ServiceNeedFoundInfo>>? _actionsToDidServiceInfoUpdated;

		protected List<Action<ServiceNeedFoundInfo>> ActionsToDidServiceInfoUpdated
		{
			get
			{
				if (_actionsToDidServiceInfoUpdated != null)
				{
					return _actionsToDidServiceInfoUpdated;
				}
				lock (this)
				{
					_actionsToDidServiceInfoUpdated ??= new();
				}
				return _actionsToDidServiceInfoUpdated;
			}
		}

		#endregion


		////////////////////////////////////////////////
		// @自身实现
		////////////////////////////////////////////////

		#region 自身实现

		public ServiceNeedFoundInfo(
		    string? groupName,
		    string serviceName)
		{
			GroupName = groupName;
			ServiceName = serviceName;

		}

		public void AddActionToDidServiceInfoUpdated(
		    Action<ServiceNeedFoundInfo>? actionToDidServiceInfoUpdated)
		{
			if (actionToDidServiceInfoUpdated == null)
			{
				return;
			}

			lock (ActionsToDidServiceInfoUpdated)
			{
				ActionsToDidServiceInfoUpdated.Add(actionToDidServiceInfoUpdated);
			}
		}

		public TaskCompletionSource<ServiceNeedFoundInfo>? AddTaskCompletionSourcesToDidServiceInfoUpdated(
		    TaskCompletionSource<ServiceNeedFoundInfo>? taskCompletionSource)
		{
			if (taskCompletionSource == null)
			{
				return null;
			}

			var taskCompletionSourcesToDidServiceInfoUpdated
			    = TaskCompletionSourcesToDidServiceInfoUpdated;
			lock (this)
			{
				taskCompletionSourcesToDidServiceInfoUpdated.Add(taskCompletionSource);
			}

			return taskCompletionSource;
		}

		public void SetServiceInfoUpdated()
		{
			TaskCompletionSource<ServiceNeedFoundInfo>[]? taskCompletionSourcesToDidServiceInfoUpdated
			    = null;
			var allTaskCompletionSourcesToDidServiceInfoUpdated
			    = TaskCompletionSourcesToDidServiceInfoUpdated;
			lock (allTaskCompletionSourcesToDidServiceInfoUpdated)
			{
				if (allTaskCompletionSourcesToDidServiceInfoUpdated.Count > 0)
				{
					taskCompletionSourcesToDidServiceInfoUpdated
					    = allTaskCompletionSourcesToDidServiceInfoUpdated.ToArray();
					allTaskCompletionSourcesToDidServiceInfoUpdated.Clear();
				}
			}
			if (ArrayUtil.IsNotEmpty(taskCompletionSourcesToDidServiceInfoUpdated))
			{
				foreach (var taskCompletionSourceToDidServiceInfoUpdated
				    in
				    taskCompletionSourcesToDidServiceInfoUpdated)
				{
					taskCompletionSourceToDidServiceInfoUpdated.SetResult(this);
				}
			}

			Action<ServiceNeedFoundInfo>[]? actionsToDidServiceInfoUpdated = null;
			var allActionsToDidServiceInfoUpdated = ActionsToDidServiceInfoUpdated;
			lock (allActionsToDidServiceInfoUpdated)
			{
				if (ListUtil.IsNotEmpty(allActionsToDidServiceInfoUpdated))
				{
					//
					actionsToDidServiceInfoUpdated = allActionsToDidServiceInfoUpdated.ToArray();
					//
				}
			}
			if (ArrayUtil.IsNotEmpty(actionsToDidServiceInfoUpdated))
			{
				foreach (var actionToDidServiceInfoUpdated
				    in
				    actionsToDidServiceInfoUpdated)
				{
					actionToDidServiceInfoUpdated.Invoke(this);
				}
			}
		}

		#endregion


	}
}
