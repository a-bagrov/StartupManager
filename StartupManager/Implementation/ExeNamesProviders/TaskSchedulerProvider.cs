using System;
using StartupManager.Interfaces;
using StartupManager.Models;
using System.Collections.Generic;
using TaskScheduler;
using System.Linq;
using StartupManager.Properties;

namespace StartupManager.Implementation.ExeNamesProviders
{
    /// <summary>
    /// Представляет доступ к элементам из планировщика задач.
    /// </summary>
    internal class TaskSchedulerProvider : IStartupExeValuesProvider
    {
        public StartupType StartupType => StartupType.Scheduler;

        public IEnumerable<string> GetValues()
        {
            var taskService = new TaskScheduler.TaskScheduler();
            taskService.Connect();
            
            return GetExeFileNames(taskService.GetFolder("\\"));
        }

        private IEnumerable<string> GetExeFileNames(ITaskFolder taskFolder)
        {
            //получить все таски включая скрытые
            var tasks = taskFolder.GetTasks((int)_TASK_ENUM_FLAGS.TASK_ENUM_HIDDEN);
            foreach (IRegisteredTask task in tasks)
            {
                if (task is null)
                    continue;

                ITaskDefinition def;

                try
                {
                    def = task.Definition;
                }
                catch (Exception e)
                {
                    Console.WriteLine($@"{Resources.ErrorGettingTaskDefinition} {task.Path} {e}");
                    continue;
                }

                if (!task.Enabled || def == null || def.Triggers.Count == 0 ||
                    def.Actions.Count == 0)
                    continue;

                //есть хотя бы один триггер при загрузке системы
                if (!def.Triggers.Cast<ITrigger>().Any(c => c.Enabled && c.Type == _TASK_TRIGGER_TYPE2.TASK_TRIGGER_BOOT))
                    continue;

                foreach (IAction action in def.Actions)
                {
                    if (action.Type == _TASK_ACTION_TYPE.TASK_ACTION_EXEC && action is IExecAction execAction)
                        yield return string.Join(" ", execAction.Path, execAction.Arguments);
                }
            }

            var folders = taskFolder.GetFolders(0);

            if (folders.Count == 0)
                yield break;

            foreach (ITaskFolder folder in folders)
            {
                foreach (var fileName in GetExeFileNames(folder))
                    yield return fileName;
            }
        }
    }
}
