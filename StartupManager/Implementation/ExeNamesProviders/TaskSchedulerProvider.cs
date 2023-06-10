using System;
using StartupManager.Interfaces;
using StartupManager.Models;
using System.Collections.Generic;
using TaskScheduler;
using System.Linq;
using System.Windows.Controls.Primitives;
using StartupManager.Properties;

namespace StartupManager.Implementation.ExeNamesProviders
{
    /// <summary>
    /// Представляет доступ к элементам из планировщика задач.
    /// </summary>
    internal class TaskSchedulerProvider : IStartupExeValuesProvider
    {
        public StartupType StartupType => StartupType.Scheduler;

        public IEnumerable<StartupResult> GetValues()
        {
            var taskService = new TaskScheduler.TaskScheduler();
            taskService.Connect();
            
            return GetExeFileNames(taskService.GetFolder("\\"));
        }

        private IEnumerable<StartupResult> GetExeFileNames(ITaskFolder taskFolder)
        {
            //получить все таски включая скрытые
            var tasks = taskFolder.GetTasks((int)_TASK_ENUM_FLAGS.TASK_ENUM_HIDDEN);
            foreach (IRegisteredTask task in tasks)
            {
                if (task is null)
                    continue;

                ITaskDefinition def;

                StartupResult result = default;
                try
                {
                    def = task.Definition;
                }
                catch (Exception e)
                {
                    result = new($@"{Resources.ErrorGettingTaskDefinition} {task.Path} {e}", false);
                    Console.WriteLine(result);
                    continue;
                }

                if (result != default)
                {
                    yield return result;
                    continue;
                }

                if (!task.Enabled || def == null || def.Triggers.Count == 0 || def.Actions.Count == 0)
                    continue;

                //есть хотя бы один триггер при загрузке системы
                // only show what is enabled and has a trigger of type boot
                // todo: show what is disabled as well?
                if (!def.Triggers.Cast<ITrigger>().Any(c => c.Enabled && c.Type == _TASK_TRIGGER_TYPE2.TASK_TRIGGER_BOOT))
                    continue;
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
