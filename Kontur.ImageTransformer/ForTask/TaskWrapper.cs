using System;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.ForTask
{
    public abstract class TaskWrapper {
        protected AsyncHttpServer Data;
        protected Task Task;
        protected abstract Action execute { get; }
        private int _id;

        public TaskWrapper(AsyncHttpServer listner, int id) {
            Data = listner;
            Task = new Task(TaskExecute);
            Task.Start();
            _id = id;
        }

        protected abstract bool IsNextStep();

        private void TaskExecute() {
            while (true)
            {
                Data.stopper.WaitOne();
                if (!IsNextStep())
                {
                    if (!Data.isRunning) {
                        Console.WriteLine("stoped proc id {0}", _id);
                        break;
                    }
                    Console.WriteLine("proc id {0} is stop", _id);
                    Data.stopper.Reset();
                }
                else {
                    Console.WriteLine("proc id {0} is execute", _id);
                    execute();
                }
            }
        }
    }
}
