using MicroM.Web.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryTest
{


    [TestClass]
    public class B1_BackgroundTaskQueueTests
    {
        private BackgroundTaskQueue _queue;
        private TestOutputLogger<BackgroundTaskQueue> _testOutputLogger;
        public TestContext TestContext { get; set; }
        private CancellationTokenSource _cts;


        [TestInitialize]
        public void Initialize()
        {
            _testOutputLogger = new TestOutputLogger<BackgroundTaskQueue>
            {
                TestContext = this.TestContext
            };
            _cts = new CancellationTokenSource();
            _queue = new BackgroundTaskQueue(15, 100, _testOutputLogger, _cts.Token);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _cts.Cancel();
            _queue.Dispose();
            _cts.Dispose();
        }

        [TestMethod]
        public async Task Enqueue_SingleInstance_AcceptsOneTask()
        {
            var taskFunc = new Func<CancellationToken, Task<string>>(async ct => { await Task.Delay(100, ct); return "Done"; });

            var taskName = "singleInstance";

            Guid id1 = _queue.Enqueue(taskName, taskFunc, true);
            Guid id2 = _queue.Enqueue(taskName, taskFunc, true);

            Assert.AreNotEqual(Guid.Empty, id1);
            Assert.AreEqual(Guid.Empty, id2);

            // Wait for the queue to stop processing
            await Task.Delay(300);
        }


        [TestMethod]
        public async Task Enqueue_MultipleInstances_AcceptsMultipleTasks()
        {
            var taskFunc = new Func<CancellationToken, Task<string>>(async ct => { await Task.Delay(100, ct); return "Done"; });

            var taskName = "multiInstance";

            Guid id1 = _queue.Enqueue(taskName, taskFunc, false);
            Guid id2 = _queue.Enqueue(taskName, taskFunc, false);

            Assert.AreNotEqual(Guid.Empty, id1);
            Assert.AreNotEqual(Guid.Empty, id2);

            // Wait for the queue to stop processing
            await Task.Delay(300);
        }


        [TestMethod]
        public async Task GetQueueStatus_ReturnsCorrectStatus()
        {
            var taskFunc = new Func<CancellationToken, Task<string>>(async ct =>  {
                await Task.Delay(200, ct);
                return "Done"; 
            });

            _queue.Enqueue(nameof(GetQueueStatus_ReturnsCorrectStatus), taskFunc, false);

            var status = _queue.GetQueueStatus();

            // The test will pass if either QueuedCount or RunningCount is 1.
            Assert.IsTrue(status.QueuedCount == 1 || status.RunningCount == 1,
                $"Expected either QueuedCount or RunningCount to be 1, but found QueuedCount = {status.QueuedCount}, RunningCount = {status.RunningCount}");

            // Wait for the queue to stop processing
            await Task.Delay(300);
        }


        [TestMethod]
        public void GetTaskStatus_TaskNotFound_ReturnsNotFoundStatus()
        {
            var status = _queue.GetTaskStatus(Guid.NewGuid());

            Assert.AreEqual(QueueTaskStatus.NotFound, status.Status);
        }



        [TestMethod]
        public async Task GetTasksStatus_ReturnsAllTaskStatus()
        {
            static async Task<string> workItem(CancellationToken ct)
            {
                await Task.Delay(100, ct);
                return "Test";
            }

            var id1 = _queue.Enqueue("taskOne", workItem, false);
            var id2 = _queue.Enqueue("taskTwo", workItem, false);

            var statuses = _queue.GetTasksStatus();

            Assert.IsTrue(statuses.ContainsKey(id1));
            Assert.IsTrue(statuses.ContainsKey(id2));

            // Wait for the queue to stop processing
            await Task.Delay(300);

        }


        [TestMethod]
        public async Task GetTaskStatus_ReturnsCorrectStatus()
        {
            static async Task<string> workItem(CancellationToken ct)
            {
                await Task.Delay(100, ct);
                return "Test";
            }

            var id = _queue.Enqueue(nameof(GetTaskStatus_ReturnsCorrectStatus), workItem, false);
            var status = _queue.GetTaskStatus(id);

            Assert.AreEqual(QueueTaskStatus.Queued, status.Status);

            // Wait for the queue to stop processing
            await Task.Delay(300);
        }


        [TestMethod]
        public async Task CancelTask_SetsTaskStatusToCancelled()
        {
            static async Task<string> workItem(CancellationToken ct)
            {
                await Task.Delay(10000, ct);
                return "Test";
            }

            var id = _queue.Enqueue("cancelTask", workItem, false);

            // Introduce a small delay to ensure that the task has started running
            await Task.Delay(100);

            _queue.CancelTask(id);

            // Allow some time for the cancellation to propagate and the task status to update
            await Task.Delay(500);

            var status = _queue.GetTaskStatus(id);

            // Note: Now the cancellation should have had time to propagate, so we can safely check the status
            Assert.AreEqual(QueueTaskStatus.Cancelled, status.Status);
        }
    }

}

