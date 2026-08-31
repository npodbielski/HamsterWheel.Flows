using FluentAssertions;

namespace HamsterWheel.Flows.Tests.Utils;

public static class WaitExtensions
{
    public static async Task WaitSeconds(this Task action, int seconds = 5, Func<string>? additionalMessage = null)
    {
        //in case action already completed just return it
        if (action.IsCompleted || action.IsCanceled || action.IsFaulted)
        {
            return;
        }

        Task? completedTask = null;
        Task? delay = null;
        while (seconds > 0)
        {
            delay = Task.Delay(1 * 1000);
            completedTask = await Task.WhenAny(action, delay);
            //in case action already completed just return it
            if (action.IsCompleted || action.IsCanceled || action.IsFaulted)
            {
                return;
            }

            --seconds;
        }

        //assert
        completedTask.Should().NotBeNull();
        if (action.IsCompleted || action.IsCanceled || action.IsFaulted)
        {
            return;
        }

        completedTask.Should().NotBe(delay, " status of awaited action is {0} {1}", action.Status, additionalMessage?.Invoke());
    }

    public static async Task<T> WaitSeconds<T>(this Task<T> action, int seconds = 5)
    {
        //in case action already completed just return it
        if (action.IsCompleted || action.IsCanceled || action.IsFaulted)
        {
            return await action;
        }

        Task? completedTask = null;
        Task? delay = null;
        while (seconds > 0)
        {
            delay = Task.Delay(1 * 1000);
            completedTask = await Task.WhenAny(action, delay);
            //in case action already completed just return it
            if (action.IsCompleted || action.IsCanceled || action.IsFaulted)
            {
                return action.Result;
            }

            --seconds;
        }

        //assert
        completedTask.Should().NotBeNull();
        completedTask.Should().NotBe(delay, "{0}", action.Status);

        return action.Result;
    }
}