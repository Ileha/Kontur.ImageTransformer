using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.ForTask
{
    public abstract class TaskWrapper {
        protected AsyncHttpServer Data;
        protected Task Task;
        protected abstract Action execute { get; }

        public TaskWrapper(AsyncHttpServer listner) {
            Data = listner;
            Task = new Task(TaskExecute);
        }

        protected abstract bool IsNextStep();

        private void TaskExecute() {
            Data.stopper.WaitOne();
            if (!IsNextStep()) {
                Data.stopper.Set();
            }
            else {
                execute();
            }
        }
    }
}
