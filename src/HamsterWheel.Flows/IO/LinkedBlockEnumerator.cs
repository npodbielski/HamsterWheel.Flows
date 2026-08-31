using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using HamsterWheel.Flows.Blocks;

namespace HamsterWheel.Flows.IO;

public class LinkedBlockEnumerator<T>
{
    private readonly IPipelineBlock<T> _owner;
    private readonly TaskCompletionSource _initializedTaskSource = new();
    private readonly ConcurrentQueue<TaskCompletionSource<T>> _stack = new();
    private readonly List<TaskCompletionSource<T>> _notCompletedTasks = new();

    public LinkedBlockEnumerator(IPipelineBlock<T> owner)
    {
        _owner = owner;
        var firstTcs = new TaskCompletionSource<T>();
        _stack.Enqueue(firstTcs);
        _notCompletedTasks.Add(firstTcs);
    }

    public void Push(T item)
    {
        _initializedTaskSource.TrySetResult();
        var nextTaskSource = new TaskCompletionSource<T>();
        _notCompletedTasks.Add(nextTaskSource);
        _stack.Enqueue(nextTaskSource);
        var tcs = _notCompletedTasks[0];
        _notCompletedTasks.RemoveAt(0);
        tcs.TrySetResult(item);
    }

    public void Finish() => _initializedTaskSource.TrySetResult();

    public async IAsyncEnumerable<T> Get([EnumeratorCancellation] CancellationToken token = default)
    {
        //this should be completed once first result reaches enumerator
        await _initializedTaskSource.Task;
        if (!token.IsCancellationRequested)
        {
            while (_stack.TryDequeue(out var taskCompletionSource))
            {
                var valueTask = taskCompletionSource.Task;
                var nextValueOrCompletedTask = await Task.WhenAny(valueTask, _owner.Completion);
                if (nextValueOrCompletedTask == valueTask)
                {
                    yield return await valueTask;
                }
                else
                {
                    //valueTask cannot be awaited because it is placeholder task that was added to allow target block to wait for Completion
                    // it is done because we have no means to predict when source block for the link will finish its computation
                    while (_stack.TryDequeue(out var availableTaskSource))
                    {
                        var task = availableTaskSource.Task;
                        if (task.IsCompleted)
                        {
                            yield return await task;
                        }
                    }

                    yield break;
                }
            }
        }
    }
}