using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Haydabase.Async.Tests
{
    [TestFixture]
    [Category("Unit")]
    public class AsyncTaskCompletionSourceTests
    {
        private const string ExpectedResult = "This is the expected result.";

        [Test]
        public async Task SetResult_Task_Await_Returns_Result()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();

            // Act
            source.SetResult(ExpectedResult);

            // Assert
            Assert.That(await source.Task, Is.EqualTo(ExpectedResult));
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void SetResult_Task_Result_Returns_Result()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();

            // Act
            source.SetResult(ExpectedResult);

            // Assert
            Assert.That(source.Task.Result, Is.EqualTo(ExpectedResult));
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void SetResult_ResultAlreadySet_Throws_InvalidOperationException()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            source.SetResult(ExpectedResult);

            // Act
            TestDelegate action = () => source.SetResult("Some other result");

            // Assert
            Assert.That(action, Throws.InvalidOperationException);
            Assert.That(source.Task.Result, Is.EqualTo(ExpectedResult));
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void SetResult_ExceptionAlreadySet_Throws_InvalidOperationException()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var expectedException = new Exception(ExpectedResult);
            source.SetException(expectedException);

            // Act
            TestDelegate action = () => source.SetResult("Some other result");

            // Assert
            Assert.That(action, Throws.InvalidOperationException);
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.Exception, Is.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.True);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void SetResult_CancellationAlreadySet_Throws_InvalidOperationException()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            source.SetCanceled();

            // Act
            TestDelegate action = () => source.SetResult("Some other result");

            // Assert
            Assert.That(action, Throws.InvalidOperationException);
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.True);
        }

        [Test]
        public async Task SetResult_Task_Await_ContinuationOccursOnDifferentThread()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var settingThread = new Thread(() => source.SetResult(ExpectedResult)) { IsBackground = false };
            var resultAwaitingTask = CreateStartedTask(() => source.Task);

            // Act
            settingThread.Start();
            var result = await resultAwaitingTask;
            WaitForThread(settingThread);

            // Assert
            Assert.That(result.Result, Is.EqualTo(ExpectedResult));
            Assert.That(result.ManagedThreadId, Is.Not.EqualTo(settingThread.ManagedThreadId), "Continuation thread should differ from setting thread");
        }

        [Test]
        public async Task SetResult_Task_Result_ContinuationOccursOnDifferentThread()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var settingThread = new Thread(() => source.SetResult(ExpectedResult)) { IsBackground = false };
            var resultAwaitingTask = CreateStartedTask(() => source.Task.Result);

            // Act
            settingThread.Start();
            var result = await resultAwaitingTask;
            WaitForThread(settingThread);

            // Assert
            Assert.That(result.Result, Is.EqualTo(ExpectedResult));
            Assert.That(result.ManagedThreadId, Is.Not.EqualTo(settingThread.ManagedThreadId), "Continuation thread should differ from setting thread");
        }

        [Test]
        public void SetException_Task_Await_Throws_Exception()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var expectedException = new Exception(ExpectedResult);

            // Act
            source.SetException(expectedException);

            // Assert
            Assert.That(async() => await source.Task, Throws.Exception.EqualTo(expectedException));
            Assert.That(source.Task.Exception, Is.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.True);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void SetException_Task_Result_Throws_Exception()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var expectedException = new Exception(ExpectedResult);

            // Act
            source.SetException(expectedException);

            // Assert
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.Exception, Is.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.True);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void SetException_ResultAlreadySet_Throws_InvalidOperationException()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            source.SetResult(ExpectedResult);

            // Act
            TestDelegate action = () => source.SetException(new Exception("Some other result"));

            // Assert
            Assert.That(action, Throws.InvalidOperationException);
            Assert.That(source.Task.Result, Is.EqualTo(ExpectedResult));
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void SetException_ExceptionAlreadySet_Throws_InvalidOperationException()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var expectedException = new Exception(ExpectedResult);
            source.SetException(expectedException);

            // Act
            TestDelegate action = () => source.SetException(new Exception("Some other result"));

            // Assert
            Assert.That(action, Throws.InvalidOperationException);
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.Exception, Is.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.True);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void SetException_CancellationAlreadySet_Throws_InvalidOperationException()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            source.SetCanceled();

            // Act
            TestDelegate action = () => source.SetException(new Exception("Some other result"));

            // Assert
            Assert.That(action, Throws.InvalidOperationException);
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.True);
        }

        [Test]
        public async Task SetException_Task_Await_ContinuationOccursOnDifferentThread()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var expectedException = new Exception(ExpectedResult);
            var settingThread = new Thread(() => source.SetException(expectedException)) { IsBackground = false };
            var resultAwaitingTask = CreateStartedTask(async () =>
            {
                try
                {
                    await source.Task;
                }
                catch (Exception exception)
                {
                    return exception;
                }
                throw new AssertionException("Unreachable code");
            });

            // Act
            settingThread.Start();
            var result = await resultAwaitingTask;
            WaitForThread(settingThread);

            // Assert
            Assert.That(result.Result, Is.EqualTo(expectedException));
            Assert.That(result.ManagedThreadId, Is.Not.EqualTo(settingThread.ManagedThreadId), "Continuation thread should differ from setting thread");
        }

        [Test]
        public async Task SetException_Task_Wait_ContinuationOccursOnDifferentThread()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var expectedException = new Exception(ExpectedResult);
            var settingThread = new Thread(() => source.SetException(expectedException)) { IsBackground = false };
            var resultAwaitingTask = CreateStartedTask(() =>
            {
                try
                {
                    source.Task.Wait();
                }
                catch (AggregateException exception)
                {
                    return exception.InnerException;
                }
                throw new AssertionException("Unreachable code");
            });

            // Act
            settingThread.Start();
            var result = await resultAwaitingTask;
            WaitForThread(settingThread);

            // Assert
            Assert.That(result.Result, Is.EqualTo(expectedException));
            Assert.That(result.ManagedThreadId, Is.Not.EqualTo(settingThread.ManagedThreadId), "Continuation thread should differ from setting thread");
        }

        [Test]
        public void SetCanceled_Task_Await_Throws_TaskCanceledException()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();

            // Act
            source.SetCanceled();

            // Assert
            Assert.That(async() => await source.Task, Throws.TypeOf<TaskCanceledException>());
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.True);
        }

        [Test]
        public void SetCanceled_Task_Result_Throws_TaskCanceledException()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();

            // Act
            source.SetCanceled();

            // Assert
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.True);
        }

        [Test]
        public void SetCanceled_ResultAlreadySet_Throws_InvalidOperationException()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            source.SetResult(ExpectedResult);

            // Act
            TestDelegate action = () => source.SetCanceled();

            // Assert
            Assert.That(action, Throws.InvalidOperationException);
            Assert.That(source.Task.Result, Is.EqualTo(ExpectedResult));
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void SetCanceled_ExceptionAlreadySet_Throws_InvalidOperationException()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var expectedException = new Exception(ExpectedResult);
            source.SetException(expectedException);

            // Act
            TestDelegate action = () => source.SetCanceled();

            // Assert
            Assert.That(action, Throws.InvalidOperationException);
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.Exception, Is.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.True);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void SetCanceled_CancellationAlreadySet_Throws_InvalidOperationException()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            source.SetCanceled();

            // Act
            TestDelegate action = () => source.SetCanceled();

            // Assert
            Assert.That(action, Throws.InvalidOperationException);
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.True);
        }

        [Test]
        public async Task SetCanceled_Task_Await_ContinuationOccursOnDifferentThread()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var settingThread = new Thread(() => source.SetCanceled()) { IsBackground = false };
            var resultAwaitingTask = CreateStartedTask(async () =>
            {
                try
                {
                    await source.Task;
                }
                catch (Exception exception)
                {
                    return exception;
                }
                throw new AssertionException("Unreachable code");
            });

            // Act
            settingThread.Start();
            var result = await resultAwaitingTask;
            WaitForThread(settingThread);

            // Assert
            Assert.That(result.Result, Is.TypeOf<TaskCanceledException>());
            Assert.That(result.ManagedThreadId, Is.Not.EqualTo(settingThread.ManagedThreadId), "Continuation thread should differ from setting thread");
        }

        [Test]
        public async Task SetCanceled_Task_Result_ContinuationOccursOnDifferentThread()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var settingThread = new Thread(() => source.SetCanceled()) { IsBackground = false };
            var resultAwaitingTask = CreateStartedTask(() =>
            {
                try
                {
                    source.Task.Wait();
                }
                catch (AggregateException exception)
                {
                    return exception.InnerException;
                }
                throw new AssertionException("Unreachable code");
            });

            // Act
            settingThread.Start();
            var result = await resultAwaitingTask;
            WaitForThread(settingThread);

            // Assert
            Assert.That(result.Result, Is.TypeOf<TaskCanceledException>());
            Assert.That(result.ManagedThreadId, Is.Not.EqualTo(settingThread.ManagedThreadId), "Continuation thread should differ from setting thread");
        }

        [Test]
        public async Task TrySetResult_Task_Await_Returns_Result()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();

            // Act
            var result = source.TrySetResult(ExpectedResult);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(await source.Task, Is.EqualTo(ExpectedResult));
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void TrySetResult_Task_Result_Returns_Result()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();

            // Act
            var result = source.TrySetResult(ExpectedResult);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(source.Task.Result, Is.EqualTo(ExpectedResult));
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void TrySetResult_ResultAlreadySet_Returns_False()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            source.SetResult(ExpectedResult);

            // Act
            var result = source.TrySetResult("Some other result");

            // Assert
            Assert.That(result, Is.False);
            Assert.That(source.Task.Result, Is.EqualTo(ExpectedResult));
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void TrySetResult_ExceptionAlreadySet_Returns_False()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var expectedException = new Exception(ExpectedResult);
            source.SetException(expectedException);

            // Act
            var result = source.TrySetResult("Some other result");

            // Assert
            Assert.That(result, Is.False);
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.Exception, Is.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.True);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void TrySetResult_CancellationAlreadySet_Returns_False()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            source.SetCanceled();

            // Act
            var result = source.TrySetResult("Some other result");

            // Assert
            Assert.That(result, Is.False);
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.True);
        }

        [Test]
        public async Task TrySetResult_Task_Await_ContinuationOccursOnDifferentThread()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var settingThread = new Thread(() => source.TrySetResult(ExpectedResult)) { IsBackground = false };
            var resultAwaitingTask = CreateStartedTask(() => source.Task);

            // Act
            settingThread.Start();
            var result = await resultAwaitingTask;
            WaitForThread(settingThread);

            // Assert
            Assert.That(result.Result, Is.EqualTo(ExpectedResult));
            Assert.That(result.ManagedThreadId, Is.Not.EqualTo(settingThread.ManagedThreadId), "Continuation thread should differ from setting thread");
        }

        [Test]
        public async Task TrySetResult_Task_Result_ContinuationOccursOnDifferentThread()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var settingThread = new Thread(() => source.TrySetResult(ExpectedResult)) { IsBackground = false };
            var resultAwaitingTask = CreateStartedTask(() => source.Task.Result);

            // Act
            settingThread.Start();
            var result = await resultAwaitingTask;
            WaitForThread(settingThread);

            // Assert
            Assert.That(result.Result, Is.EqualTo(ExpectedResult));
            Assert.That(result.ManagedThreadId, Is.Not.EqualTo(settingThread.ManagedThreadId), "Continuation thread should differ from setting thread");
        }

        [Test]
        public void TrySetException_Task_Await_Throws_Exception()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var expectedException = new Exception(ExpectedResult);

            // Act
            var result = source.TrySetException(expectedException);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(async () => await source.Task, Throws.Exception.EqualTo(expectedException));
            Assert.That(source.Task.Exception, Is.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.True);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void TrySetException_Task_Result_Throws_Exception()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var expectedException = new Exception(ExpectedResult);

            // Act
            var result = source.TrySetException(expectedException);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.Exception, Is.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.True);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void TrySetException_ResultAlreadySet_Returns_False()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            source.SetResult(ExpectedResult);

            // Act
            var result = source.TrySetException(new Exception("Some other result"));

            // Assert
            Assert.That(result, Is.False);
            Assert.That(source.Task.Result, Is.EqualTo(ExpectedResult));
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void TrySetException_ExceptionAlreadySet_Returns_False()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var expectedException = new Exception(ExpectedResult);
            source.SetException(expectedException);

            // Act
            var result = source.TrySetException(new Exception("Some other result"));

            // Assert
            Assert.That(result, Is.False);
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.Exception, Is.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.True);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void TrySetException_CancellationAlreadySet_Returns_False()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            source.SetCanceled();

            // Act
            var result = source.TrySetException(new Exception("Some other result"));

            // Assert
            Assert.That(result, Is.False);
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.True);
        }

        [Test]
        public async Task TrySetException_Task_Await_ContinuationOccursOnDifferentThread()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var expectedException = new Exception(ExpectedResult);
            var settingThread = new Thread(() => source.TrySetException(expectedException)) { IsBackground = false };
            var resultAwaitingTask = CreateStartedTask(async () =>
            {
                try
                {
                    await source.Task;
                }
                catch (Exception exception)
                {
                    return exception;
                }
                throw new AssertionException("Unreachable code");
            });

            // Act
            settingThread.Start();
            var result = await resultAwaitingTask;
            WaitForThread(settingThread);

            // Assert
            Assert.That(result.Result, Is.EqualTo(expectedException));
            Assert.That(result.ManagedThreadId, Is.Not.EqualTo(settingThread.ManagedThreadId), "Continuation thread should differ from setting thread");
        }

        [Test]
        public async Task TrySetException_Task_Wait_ContinuationOccursOnDifferentThread()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var expectedException = new Exception(ExpectedResult);
            var settingThread = new Thread(() => source.TrySetException(expectedException)) { IsBackground = false };
            var resultAwaitingTask = CreateStartedTask(() =>
            {
                try
                {
                    source.Task.Wait();
                }
                catch (AggregateException exception)
                {
                    return exception.InnerException;
                }
                throw new AssertionException("Unreachable code");
            });

            // Act
            settingThread.Start();
            var result = await resultAwaitingTask;
            WaitForThread(settingThread);

            // Assert
            Assert.That(result.Result, Is.EqualTo(expectedException));
            Assert.That(result.ManagedThreadId, Is.Not.EqualTo(settingThread.ManagedThreadId), "Continuation thread should differ from setting thread");
        }

        [Test]
        public void TrySetCanceled_Task_Await_Throws_TaskCanceledException()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();

            // Act
            var result = source.TrySetCanceled();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(async () => await source.Task, Throws.TypeOf<TaskCanceledException>());
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.True);
        }

        [Test]
        public void TrySetCanceled_Task_Result_Throws_TaskCanceledException()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();

            // Act
            var result = source.TrySetCanceled();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.True);
        }

        [Test]
        public void TrySetCanceled_ResultAlreadySet_Returns_False()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            source.SetResult(ExpectedResult);

            // Act
            var result = source.TrySetCanceled();

            // Assert
            Assert.That(result, Is.False);
            Assert.That(source.Task.Result, Is.EqualTo(ExpectedResult));
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void TrySetCanceled_ExceptionAlreadySet_Returns_False()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var expectedException = new Exception(ExpectedResult);
            source.SetException(expectedException);

            // Act
            var result = source.TrySetCanceled();

            // Assert
            Assert.That(result, Is.False);
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.Exception, Is.TypeOf<AggregateException>().With.InnerException.EqualTo(expectedException));
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.True);
            Assert.That(source.Task.IsCanceled, Is.False);
        }

        [Test]
        public void TrySetCanceled_CancellationAlreadySet_Returns_False()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            source.SetCanceled();

            // Act
            var result = source.TrySetCanceled();

            // Assert
            Assert.That(result, Is.False);
            Assert.That(() => source.Task.Result, Throws.TypeOf<AggregateException>().With.InnerException.TypeOf<TaskCanceledException>());
            Assert.That(source.Task.Exception, Is.Null);
            Assert.That(source.Task.IsCompleted, Is.True);
            Assert.That(source.Task.IsFaulted, Is.False);
            Assert.That(source.Task.IsCanceled, Is.True);
        }

        [Test]
        public async Task TrySetCanceled_Task_Await_ContinuationOccursOnDifferentThread()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var settingThread = new Thread(() => source.TrySetCanceled()) { IsBackground = false };
            var resultAwaitingTask = CreateStartedTask(async () =>
            {
                try
                {
                    await source.Task;
                }
                catch (Exception exception)
                {
                    return exception;
                }
                throw new AssertionException("Unreachable code");
            });

            // Act
            settingThread.Start();
            var result = await resultAwaitingTask;
            WaitForThread(settingThread);

            // Assert
            Assert.That(result.Result, Is.TypeOf<TaskCanceledException>());
            Assert.That(result.ManagedThreadId, Is.Not.EqualTo(settingThread.ManagedThreadId), "Continuation thread should differ from setting thread");
        }

        [Test]
        public async Task TrySetCanceled_Task_Result_ContinuationOccursOnDifferentThread()
        {
            // Arrange
            var source = new AsyncTaskCompletionSource<string>();
            var settingThread = new Thread(() => source.TrySetCanceled()) { IsBackground = false };
            var resultAwaitingTask = CreateStartedTask(() =>
            {
                try
                {
                    source.Task.Wait();
                }
                catch (AggregateException exception)
                {
                    return exception.InnerException;
                }
                throw new AssertionException("Unreachable code");
            });

            // Act
            settingThread.Start();
            var result = await resultAwaitingTask;
            WaitForThread(settingThread);

            // Assert
            Assert.That(result.Result, Is.TypeOf<TaskCanceledException>());
            Assert.That(result.ManagedThreadId, Is.Not.EqualTo(settingThread.ManagedThreadId), "Continuation thread should differ from setting thread");
        }

        private static async Task<TaskResult<TResult>> CreateStartedTask<TResult>(Func<Task<TResult>> getResultAsync)
        {
            var value = await getResultAsync();
            return new TaskResult<TResult>(Thread.CurrentThread.ManagedThreadId, value);
        }

        private static Task<TaskResult<TResult>> CreateStartedTask<TResult>(Func<TResult> getResult)
        {
            var waitHandle = new ManualResetEventSlim();
            var task = Task.Run(() =>
            {
                waitHandle.Set();
                var value = getResult();
                return new TaskResult<TResult>(Thread.CurrentThread.ManagedThreadId, value);
            });
            waitHandle.Wait();
            return task;
        }

        private static void WaitForThread(Thread thread)
        {
            if (thread != Thread.CurrentThread)
            {
                thread.Join();
            }
        }

        private sealed class TaskResult<TResult>
        {
            public TaskResult(int managedThreadId, TResult result)
            {
                ManagedThreadId = managedThreadId;
                Result = result;
            }

            public int ManagedThreadId { get; }
            public TResult Result { get; }
        }
    }
}
