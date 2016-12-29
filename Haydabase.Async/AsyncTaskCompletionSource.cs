using System;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace Haydabase.Async
{
    /// <summary>
    /// Represents the producer side of a <see cref="System.Threading.Tasks.Task{TResult}"/> unbound to a
    /// delegate, providing access to the consumer side through the <see cref="Task"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is often the case that a <see cref="System.Threading.Tasks.Task{TResult}"/> is desired to
    /// represent another asynchronous operation.
    /// <see cref="AsyncTaskCompletionSource{TResult}">AsyncTaskCompletionSource</see> is provided for this purpose. It enables
    /// the creation of a task that can be handed out to consumers, and those consumers can use the members
    /// of the task as they would any other. However, unlike most tasks, the state of a task created by an
    /// AsyncTaskCompletionSource is controlled explicitly by the methods on AsyncTaskCompletionSource. This enables the
    /// completion of the external asynchronous operation to be propagated to the underlying Task. The
    /// separation also ensures that consumers are not able to transition the state without access to the
    /// corresponding AsyncTaskCompletionSource.
    /// </para>
    /// <para>
    /// <see cref="AsyncTaskCompletionSource{TResult}"/> behaves identically to <see cref="TaskCompletionSource{TResult}"/>
    /// but with async continuations (awaiting the <see cref="Task"/>) not executing synchronously on the thread that calls the
    /// Set* or TrySet* methods.
    /// </para>
    /// <para>
    /// All members of <see cref="AsyncTaskCompletionSource{TResult}"/> are thread-safe
    /// and may be used from multiple threads concurrently.
    /// </para>
    /// </remarks>
    /// <typeparam name="TResult">The type of the result value assocatied with this <see
    /// cref="AsyncTaskCompletionSource{TResult}"/>.</typeparam>
    [HostProtection(Synchronization = true, ExternalThreading = true)]
    public class AsyncTaskCompletionSource<TResult>
    {
        private readonly TaskCompletionSource<TResult> _source;

        /// <summary>
        /// Creates an <see cref="AsyncTaskCompletionSource{TResult}"/>.
        /// </summary>
        public AsyncTaskCompletionSource()
            : this(new TaskCompletionSource<TResult>())
        {
        }

        /// <summary>
        /// Creates an <see cref="AsyncTaskCompletionSource{TResult}"/>
        /// with the specified options.
        /// </summary>
        /// <remarks>
        /// The <see cref="System.Threading.Tasks.Task{TResult}"/> created
        /// by this instance and accessible through its <see cref="Task"/> property
        /// will be instantiated using the specified <paramref name="creationOptions"/>.
        /// </remarks>
        /// <param name="creationOptions">The options to use when creating the underlying
        /// <see cref="System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="creationOptions"/> represent options invalid for use
        /// with a <see cref="TaskCompletionSource{TResult}"/>.
        /// </exception>
        public AsyncTaskCompletionSource(TaskCreationOptions creationOptions)
            : this(new TaskCompletionSource<TResult>(creationOptions))
        {
        }

        /// <summary>
        /// Creates an <see cref="AsyncTaskCompletionSource{TResult}"/>
        /// with the specified state.
        /// </summary>
        /// <param name="state">The state to use as the underlying 
        /// <see cref="System.Threading.Tasks.Task{TResult}"/>'s AsyncState.</param>
        public AsyncTaskCompletionSource(object state)
            : this(new TaskCompletionSource<TResult>(state))
        {
        }

        /// <summary>
        /// Creates an <see cref="AsyncTaskCompletionSource{TResult}"/> with
        /// the specified state and options.
        /// </summary>
        /// <param name="creationOptions">The options to use when creating the underlying
        /// <see cref="System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="state">The state to use as the underlying 
        /// <see cref="System.Threading.Tasks.Task{TResult}"/>'s AsyncState.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="creationOptions"/> represent options invalid for use
        /// with a <see cref="TaskCompletionSource{TResult}"/>.
        /// </exception>
        public AsyncTaskCompletionSource(object state, TaskCreationOptions creationOptions)
            : this(new TaskCompletionSource<TResult>(state, creationOptions))
        {
        }

        private AsyncTaskCompletionSource(TaskCompletionSource<TResult> sink)
        {
            _source = new TaskCompletionSource<TResult>();
            _source.Task.ContinueWith(t => OnContinuation(t, sink));
            Task = sink.Task;
        }

        private static void OnContinuation(Task<TResult> task, TaskCompletionSource<TResult> sink)
        {
            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                    sink.SetResult(task.Result);
                    break;
                case TaskStatus.Faulted:
                    sink.SetException(task.Exception.InnerException);
                    break;
                case TaskStatus.Canceled:
                    sink.SetCanceled();
                    break;
            }
        }

        /// <summary>
        /// Gets the <see cref="System.Threading.Tasks.Task{TResult}"/> created
        /// by this <see cref="AsyncTaskCompletionSource{TResult}"/>.
        /// </summary>
        /// <remarks>
        /// This property enables a consumer access to the <see
        /// cref="System.Threading.Tasks.Task{TResult}"/> that is controlled by this instance.
        /// The <see cref="SetResult"/>, <see cref="SetException(Exception)"/>, and <see cref="SetCanceled"/>
        /// methods (and their "Try" variants) on this instance all result in the relevant state
        /// transitions on this underlying Task.
        /// </remarks>
        public Task<TResult> Task { get; }

        /// <summary>
        /// Transitions the underlying
        /// <see cref="System.Threading.Tasks.Task{TResult}"/> into the 
        /// <see cref="TaskStatus.RanToCompletion">RanToCompletion</see>
        /// state.
        /// </summary>
        /// <param name="result">The result value to bind to this <see 
        /// cref="System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <exception cref="InvalidOperationException">
        /// The underlying <see cref="System.Threading.Tasks.Task{TResult}"/> is already in one
        /// of the three final states:
        /// <see cref="TaskStatus.RanToCompletion">RanToCompletion</see>, 
        /// <see cref="TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="TaskStatus.Canceled">Canceled</see>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Task"/> was disposed.</exception>
        public void SetResult(TResult result)
        {
            _source.SetResult(result);
        }

        /// <summary>
        /// Transitions the underlying
        /// <see cref="System.Threading.Tasks.Task{TResult}"/> into the 
        /// <see cref="TaskStatus.Faulted">Faulted</see>
        /// state.
        /// </summary>
        /// <param name="exception">The exception to bind to this <see 
        /// cref="System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="exception"/> argument is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// The underlying <see cref="System.Threading.Tasks.Task{TResult}"/> is already in one
        /// of the three final states:
        /// <see cref="TaskStatus.RanToCompletion">RanToCompletion</see>, 
        /// <see cref="TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="TaskStatus.Canceled">Canceled</see>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Task"/> was disposed.</exception>
        public void SetException(Exception exception)
        {
            _source.SetException(exception);
        }

        /// <summary>
        /// Transitions the underlying
        /// <see cref="System.Threading.Tasks.Task{TResult}"/> into the 
        /// <see cref="TaskStatus.Canceled">Canceled</see>
        /// state.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The underlying <see cref="System.Threading.Tasks.Task{TResult}"/> is already in one
        /// of the three final states:
        /// <see cref="TaskStatus.RanToCompletion">RanToCompletion</see>, 
        /// <see cref="TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="TaskStatus.Canceled">Canceled</see>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Task"/> was disposed.</exception>
        public void SetCanceled()
        {
            _source.SetCanceled();
        }

        /// <summary>
        /// Attempts to transition the underlying
        /// <see cref="System.Threading.Tasks.Task{TResult}"/> into the 
        /// <see cref="TaskStatus.RanToCompletion">RanToCompletion</see>
        /// state.
        /// </summary>
        /// <param name="result">The result value to bind to this <see 
        /// cref="System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <remarks>This operation will return false if the 
        /// <see cref="System.Threading.Tasks.Task{TResult}"/> is already in one
        /// of the three final states:
        /// <see cref="TaskStatus.RanToCompletion">RanToCompletion</see>, 
        /// <see cref="TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="TaskStatus.Canceled">Canceled</see>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="Task"/> was disposed.</exception>
        public bool TrySetResult(TResult result)
        {
            return _source.TrySetResult(result);
        }

        /// <summary>
        /// Attempts to transition the underlying
        /// <see cref="System.Threading.Tasks.Task{TResult}"/> into the 
        /// <see cref="TaskStatus.Faulted">Faulted</see>
        /// state.
        /// </summary>
        /// <param name="exception">The exception to bind to this <see 
        /// cref="System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <remarks>This operation will return false if the 
        /// <see cref="System.Threading.Tasks.Task{TResult}"/> is already in one
        /// of the three final states:
        /// <see cref="TaskStatus.RanToCompletion">RanToCompletion</see>, 
        /// <see cref="TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="TaskStatus.Canceled">Canceled</see>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The <paramref name="exception"/> argument is null.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Task"/> was disposed.</exception>
        public bool TrySetException(Exception exception)
        {
            return _source.TrySetException(exception);
        }

        /// <summary>
        /// Attempts to transition the underlying
        /// <see cref="System.Threading.Tasks.Task{TResult}"/> into the 
        /// <see cref="TaskStatus.Canceled">Canceled</see>
        /// state.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <remarks>This operation will return false if the 
        /// <see cref="System.Threading.Tasks.Task{TResult}"/> is already in one
        /// of the three final states:
        /// <see cref="TaskStatus.RanToCompletion">RanToCompletion</see>, 
        /// <see cref="TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="TaskStatus.Canceled">Canceled</see>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="Task"/> was disposed.</exception>
        public bool TrySetCanceled()
        {
            return _source.TrySetCanceled();
        }
    }
}
