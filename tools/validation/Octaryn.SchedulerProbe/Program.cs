using System.Collections.Concurrent;
using Octaryn.Client.ClientHost;
using Octaryn.Server.Tick;
using Octaryn.Shared.ApiExposure;
using Octaryn.Shared.Host;

return SchedulerProbe.Run();

internal static class SchedulerProbe
{
    public static int Run()
    {
        ValidateInvalidWorkerCounts("client", count => new ClientHostScheduler(count));
        ValidateInvalidWorkerCounts("server", count => new ServerHostScheduler(count));
        ValidateDefaultCapacity("client", () => new ClientHostScheduler());
        ValidateDefaultCapacity("server", () => new ServerHostScheduler());

        using var client = new ClientHostScheduler(2, ProbeDeclarations("client", 2));
        using var server = new ServerHostScheduler(2, ProbeDeclarations("server", 2));

        ValidateScheduler("client", client, () => client.Diagnostics);
        ValidateScheduler("server", server, () => server.Diagnostics);
        ValidateTopology("client", client.Diagnostics, client.WorkerThreadCapacity);
        ValidateTopology("server", server.Diagnostics, server.WorkerThreadCapacity);
        ValidateShutdownUnresolvedWorkDiagnostics(
            "client",
            () => new ClientHostScheduler(2, UnresolvedProbeDeclarations("client")),
            scheduler => scheduler.Diagnostics);
        ValidateShutdownUnresolvedWorkDiagnostics(
            "server",
            () => new ServerHostScheduler(2, UnresolvedProbeDeclarations("server")),
            scheduler => scheduler.Diagnostics);
        ValidateDisposedScheduler("client", new ClientHostScheduler(2));
        ValidateDisposedScheduler("server", new ServerHostScheduler(2));
        return 0;
    }

    private static void ValidateDefaultCapacity(string owner, Func<IHostScheduler> createScheduler)
    {
        using var disposable = (IDisposable)createScheduler();
        var scheduler = (IHostScheduler)disposable;
        var expected = Math.Max(
            HostSchedulingContract.MinimumWorkerThreads,
            Environment.ProcessorCount);
        if (scheduler.WorkerThreadCapacity != expected)
        {
            throw new InvalidOperationException(
                $"{owner}: default worker capacity {scheduler.WorkerThreadCapacity} did not match {expected}.");
        }
    }

    private static void ValidateTopology(
        string owner,
        HostSchedulerDiagnostics diagnostics,
        int workerThreadCapacity)
    {
        var expectedCoordinator = $"octaryn.{owner}.coordinator";
        if (diagnostics.CoordinatorThreadName != expectedCoordinator || !diagnostics.IsCoordinatorThreadAlive)
        {
            throw new InvalidOperationException($"{owner}: coordinator diagnostics are invalid.");
        }

        if (diagnostics.LiveWorkerThreadCount != workerThreadCapacity ||
            diagnostics.WorkerThreadNames.Count != workerThreadCapacity)
        {
            throw new InvalidOperationException($"{owner}: worker diagnostics do not match capacity.");
        }

        for (var index = 0; index < workerThreadCapacity; index++)
        {
            var expectedWorker = $"octaryn.{owner}.worker.{index}";
            if (!diagnostics.WorkerThreadNames.Contains(expectedWorker, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"{owner}: missing worker thread {expectedWorker}.");
            }
        }
    }

    private static void ValidateInvalidWorkerCounts(
        string owner,
        Func<int, IDisposable> createScheduler)
    {
        foreach (var workerCount in new[] { 0, 1 })
        {
            try
            {
                using var scheduler = createScheduler(workerCount);
                throw new InvalidOperationException($"{owner}: accepted invalid worker count {workerCount}.");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }
    }

    private static void ValidateShutdownUnresolvedWorkDiagnostics<TScheduler>(
        string owner,
        Func<TScheduler> createScheduler,
        Func<TScheduler, HostSchedulerDiagnostics> getDiagnostics)
        where TScheduler : IHostScheduler, IDisposable
    {
        var scheduler = createScheduler();
        var unresolved = new HostScheduledWork(
            $"{owner}.probe.shutdown_unresolved",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.CanRunInParallel,
            _ => throw new InvalidOperationException($"{owner}: unresolved shutdown work should not execute."));

        if (!scheduler.TrySchedule(unresolved))
        {
            throw new InvalidOperationException($"{owner}: unresolved shutdown work was rejected.");
        }

        scheduler.Dispose();
        var diagnostics = getDiagnostics(scheduler);
        if (diagnostics.FireAndForgetFailureCount != 1 ||
            diagnostics.LastFireAndForgetFailureWorkId != unresolved.WorkId ||
            diagnostics.LastFireAndForgetFailureType != typeof(InvalidOperationException).FullName)
        {
            throw new InvalidOperationException($"{owner}: unresolved shutdown failure diagnostics were not recorded.");
        }
    }

    private static void ValidateScheduler(
        string owner,
        IHostScheduler scheduler,
        Func<HostSchedulerDiagnostics> getDiagnostics)
    {
        if (scheduler.MinimumWorkerThreads != HostSchedulingContract.MinimumWorkerThreads)
        {
            throw new InvalidOperationException($"{owner}: wrong minimum worker count.");
        }

        if (scheduler.WorkerThreadCapacity < HostSchedulingContract.MinimumWorkerThreads)
        {
            throw new InvalidOperationException($"{owner}: worker capacity below contract minimum.");
        }

        if (!scheduler.IsWorkerPoolAvailable)
        {
            throw new InvalidOperationException($"{owner}: worker pool unavailable before disposal.");
        }

        var frame = CreateFrame(42);
        var ranBlockingWork = false;
        var blockingWork = new HostScheduledWork(
            $"{owner}.probe.blocking",
            HostWorkPhase.Validation,
            HostWorkAccess.None,
            HostWorkScheduleFlags.RequiresTickBarrier,
            context =>
            {
                if (context.ThreadRole != HostThreadRole.WorkerPool)
                {
                    throw new InvalidOperationException($"{owner}: blocking work did not run on worker pool.");
                }

                if (context.WorkerIndex < 0)
                {
                    throw new InvalidOperationException($"{owner}: worker index was not assigned.");
                }

                if (context.Frame.FrameIndex != frame.FrameIndex)
                {
                    throw new InvalidOperationException($"{owner}: frame context was not propagated.");
                }

                ranBlockingWork = true;
            });

        if (!scheduler.TryRun(blockingWork, frame) || !ranBlockingWork)
        {
            throw new InvalidOperationException($"{owner}: blocking scheduled work did not complete.");
        }

        using var fireAndForgetCompleted = new ManualResetEventSlim();
        var fireAndForgetWork = new HostScheduledWork(
            $"{owner}.probe.fire_and_forget",
            HostWorkPhase.Validation,
            HostWorkAccess.None,
            HostWorkScheduleFlags.None,
            context =>
            {
                if (context.ThreadRole != HostThreadRole.WorkerPool || context.WorkerIndex < 0)
                {
                    throw new InvalidOperationException($"{owner}: fire-and-forget work did not run on worker pool.");
                }

                fireAndForgetCompleted.Set();
            });

        if (!scheduler.TrySchedule(fireAndForgetWork))
        {
            throw new InvalidOperationException($"{owner}: fire-and-forget schedule was rejected.");
        }

        if (!fireAndForgetCompleted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: fire-and-forget work did not execute.");
        }

        var expectedFailure = new InvalidOperationException($"{owner}: expected blocking failure.");
        var failingBlockingWork = new HostScheduledWork(
            $"{owner}.probe.failing_blocking",
            HostWorkPhase.Validation,
            HostWorkAccess.None,
            HostWorkScheduleFlags.RequiresTickBarrier,
            _ => throw expectedFailure);

        try
        {
            scheduler.TryRun(failingBlockingWork, frame);
            throw new InvalidOperationException($"{owner}: blocking scheduler failure did not propagate.");
        }
        catch (InvalidOperationException exception) when (ReferenceEquals(exception, expectedFailure))
        {
        }

        using var workerSurvived = new ManualResetEventSlim();
        var failingFireAndForgetWork = new HostScheduledWork(
            $"{owner}.probe.failing_fire_and_forget",
            HostWorkPhase.Validation,
            HostWorkAccess.None,
            HostWorkScheduleFlags.None,
            _ => throw new InvalidOperationException($"{owner}: expected fire-and-forget failure."));
        var postFailureWork = new HostScheduledWork(
            $"{owner}.probe.after_fire_and_forget_failure",
            HostWorkPhase.Validation,
            HostWorkAccess.None,
            HostWorkScheduleFlags.None,
            context =>
            {
                if (context.ThreadRole == HostThreadRole.Main)
                {
                    throw new InvalidOperationException($"{owner}: work executed on main thread.");
                }

                workerSurvived.Set();
            });

        if (!scheduler.TrySchedule(failingFireAndForgetWork) || !scheduler.TrySchedule(postFailureWork))
        {
            throw new InvalidOperationException($"{owner}: scheduler rejected fire-and-forget failure probe.");
        }

        if (!workerSurvived.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: worker pool stopped after fire-and-forget failure.");
        }

        var diagnosticsAfterFailure = getDiagnostics();
        if (diagnosticsAfterFailure.FireAndForgetFailureCount != 1 ||
            diagnosticsAfterFailure.LastFireAndForgetFailureWorkId != failingFireAndForgetWork.WorkId ||
            diagnosticsAfterFailure.LastFireAndForgetFailureType != typeof(InvalidOperationException).FullName)
        {
            throw new InvalidOperationException($"{owner}: fire-and-forget failure diagnostics were not recorded.");
        }

        ValidateSerialScheduling(owner, scheduler);
        ValidateRunsAfterScheduling(owner, scheduler);
        ValidateRunsAfterResetsBetweenScheduleEpochs(owner, scheduler);
        ValidateRunsAfterFailedPrerequisiteSkipsDependent(owner, scheduler);
        ValidateRunsBeforeScheduling(owner, scheduler);
        ValidateCommitBarrierDrainsEarlierParallelWork(owner, scheduler);
        ValidateExactResourceWriteConflict(owner, scheduler);
        ValidateIndependentResourceWritesUseWorkerPool(owner, scheduler);
        ValidateNestedBlockingRunRejected(owner, scheduler);
        ValidateUndeclaredWorkRejected(owner, scheduler);
        ValidateBlockingTryRunOrderedDependencyRejected(owner, scheduler);
        ValidateParallelCapacity(owner, scheduler);
    }

    private static void ValidateBlockingTryRunOrderedDependencyRejected(string owner, IHostScheduler scheduler)
    {
        var prerequisite = new HostScheduledWork(
            $"{owner}.probe.order.after.prerequisite",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.CanRunInParallel,
            _ => throw new InvalidOperationException($"{owner}: ordered blocking prerequisite should not execute."));
        var dependent = new HostScheduledWork(
            $"{owner}.probe.order.after.dependent",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.CanRunInParallel,
            _ => throw new InvalidOperationException($"{owner}: ordered blocking dependent should not execute."));
        if (scheduler.TryRun(prerequisite, CreateFrame(91)) || scheduler.TryRun(dependent, CreateFrame(91)))
        {
            throw new InvalidOperationException($"{owner}: ordered blocking TryRun was accepted.");
        }
    }


    private static void ValidateRunsAfterFailedPrerequisiteSkipsDependent(string owner, IHostScheduler scheduler)
    {
        using var dependentStarted = new ManualResetEventSlim();
        using var prerequisiteStarted = new ManualResetEventSlim();

        var dependent = new HostScheduledWork(
            $"{owner}.probe.order.failed.dependent",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.CanRunInParallel,
            _ => dependentStarted.Set());
        var prerequisite = new HostScheduledWork(
            $"{owner}.probe.order.failed.prerequisite",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.CanRunInParallel,
            _ =>
            {
                prerequisiteStarted.Set();
                throw new InvalidOperationException($"{owner}: expected ordered prerequisite failure.");
            });

        if (!scheduler.TrySchedule(dependent) || !scheduler.TrySchedule(prerequisite))
        {
            throw new InvalidOperationException($"{owner}: failed prerequisite order probe was rejected.");
        }

        if (!prerequisiteStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: failed prerequisite order probe did not start.");
        }

        if (dependentStarted.Wait(TimeSpan.FromMilliseconds(250)))
        {
            throw new InvalidOperationException($"{owner}: dependent ran after ordered prerequisite failure.");
        }
    }


    private static void ValidateRunsAfterResetsBetweenScheduleEpochs(string owner, IHostScheduler scheduler)
    {
        RunOrderedPairEpoch(owner, scheduler, "first");
        RunOrderedPairEpoch(owner, scheduler, "second");
    }

    private static void RunOrderedPairEpoch(string owner, IHostScheduler scheduler, string epoch)
    {
        using var dependentStarted = new ManualResetEventSlim();
        using var prerequisiteStarted = new ManualResetEventSlim();
        using var releasePrerequisite = new ManualResetEventSlim();

        var dependent = new HostScheduledWork(
            $"{owner}.probe.order.epoch.dependent",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.CanRunInParallel,
            _ => dependentStarted.Set());
        var prerequisite = new HostScheduledWork(
            $"{owner}.probe.order.epoch.prerequisite",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.CanRunInParallel,
            _ =>
            {
                prerequisiteStarted.Set();
                if (!releasePrerequisite.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new InvalidOperationException($"{owner}: RunsAfter epoch {epoch} prerequisite release timed out.");
                }
            });

        if (!scheduler.TrySchedule(dependent))
        {
            throw new InvalidOperationException($"{owner}: RunsAfter epoch {epoch} dependent work was rejected.");
        }

        if (dependentStarted.Wait(TimeSpan.FromMilliseconds(150)))
        {
            throw new InvalidOperationException($"{owner}: RunsAfter epoch {epoch} reused stale prerequisite completion.");
        }

        if (!scheduler.TrySchedule(prerequisite))
        {
            throw new InvalidOperationException($"{owner}: RunsAfter epoch {epoch} prerequisite work was rejected.");
        }

        if (!prerequisiteStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: RunsAfter epoch {epoch} prerequisite did not start.");
        }

        if (dependentStarted.Wait(TimeSpan.FromMilliseconds(150)))
        {
            throw new InvalidOperationException($"{owner}: RunsAfter epoch {epoch} dependent started before prerequisite completed.");
        }

        releasePrerequisite.Set();
        if (!dependentStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: RunsAfter epoch {epoch} dependent did not start.");
        }
    }


    private static void ValidateCommitBarrierDrainsEarlierParallelWork(string owner, IHostScheduler scheduler)
    {
        using var parallelStarted = new ManualResetEventSlim();
        using var releaseParallel = new ManualResetEventSlim();
        using var barrierStarted = new ManualResetEventSlim();

        var parallel = new HostScheduledWork(
            $"{owner}.probe.barrier.parallel",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.CanRunInParallel,
            _ =>
            {
                parallelStarted.Set();
                if (!releaseParallel.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new InvalidOperationException($"{owner}: barrier parallel release timed out.");
                }
            });
        var barrier = new HostScheduledWork(
            $"{owner}.probe.barrier.commit",
            HostWorkPhase.Gameplay,
            HostWorkAccess.None,
            HostWorkScheduleFlags.RequiresTickBarrier,
            _ => barrierStarted.Set());

        if (!scheduler.TrySchedule(parallel))
        {
            throw new InvalidOperationException($"{owner}: barrier parallel work was rejected.");
        }

        if (!parallelStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: barrier parallel work did not start.");
        }

        if (!scheduler.TrySchedule(barrier))
        {
            throw new InvalidOperationException($"{owner}: barrier work was rejected.");
        }

        if (barrierStarted.Wait(TimeSpan.FromMilliseconds(150)))
        {
            throw new InvalidOperationException($"{owner}: barrier work started before earlier parallel work completed.");
        }

        releaseParallel.Set();
        if (!barrierStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: barrier work did not start after earlier parallel work completed.");
        }
    }


    private static void ValidateRunsAfterScheduling(string owner, IHostScheduler scheduler)
    {
        using var dependentStarted = new ManualResetEventSlim();
        using var prerequisiteStarted = new ManualResetEventSlim();
        using var releasePrerequisite = new ManualResetEventSlim();
        var order = new ConcurrentQueue<string>();

        var dependent = new HostScheduledWork(
            $"{owner}.probe.order.after.dependent",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.CanRunInParallel,
            _ =>
            {
                order.Enqueue("dependent");
                dependentStarted.Set();
            });
        var prerequisite = new HostScheduledWork(
            $"{owner}.probe.order.after.prerequisite",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.CanRunInParallel,
            _ =>
            {
                order.Enqueue("prerequisite");
                prerequisiteStarted.Set();
                if (!releasePrerequisite.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new InvalidOperationException($"{owner}: RunsAfter prerequisite release timed out.");
                }
            });

        if (!scheduler.TrySchedule(dependent))
        {
            throw new InvalidOperationException($"{owner}: RunsAfter dependent work was rejected.");
        }

        if (dependentStarted.Wait(TimeSpan.FromMilliseconds(150)))
        {
            throw new InvalidOperationException($"{owner}: RunsAfter dependent started before prerequisite was scheduled.");
        }

        if (!scheduler.TrySchedule(prerequisite))
        {
            throw new InvalidOperationException($"{owner}: RunsAfter prerequisite work was rejected.");
        }

        if (!prerequisiteStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: RunsAfter prerequisite did not start.");
        }

        if (dependentStarted.Wait(TimeSpan.FromMilliseconds(150)))
        {
            throw new InvalidOperationException($"{owner}: RunsAfter dependent started before prerequisite completed.");
        }

        releasePrerequisite.Set();
        if (!dependentStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: RunsAfter dependent did not start after prerequisite.");
        }

        ValidateOrder(owner, "RunsAfter", order, "prerequisite", "dependent");
    }

    private static void ValidateRunsBeforeScheduling(string owner, IHostScheduler scheduler)
    {
        using var afterStarted = new ManualResetEventSlim();
        using var beforeStarted = new ManualResetEventSlim();
        using var releaseBefore = new ManualResetEventSlim();
        var order = new ConcurrentQueue<string>();

        var after = new HostScheduledWork(
            $"{owner}.probe.order.before.after",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.CanRunInParallel,
            _ =>
            {
                order.Enqueue("after");
                afterStarted.Set();
            });
        var before = new HostScheduledWork(
            $"{owner}.probe.order.before.before",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.CanRunInParallel,
            _ =>
            {
                order.Enqueue("before");
                beforeStarted.Set();
                if (!releaseBefore.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new InvalidOperationException($"{owner}: RunsBefore release timed out.");
                }
            });

        if (!scheduler.TrySchedule(after))
        {
            throw new InvalidOperationException($"{owner}: RunsBefore dependent work was rejected.");
        }

        if (afterStarted.Wait(TimeSpan.FromMilliseconds(150)))
        {
            throw new InvalidOperationException($"{owner}: RunsBefore dependent started before prerequisite was scheduled.");
        }

        if (!scheduler.TrySchedule(before))
        {
            throw new InvalidOperationException($"{owner}: RunsBefore prerequisite work was rejected.");
        }

        if (!beforeStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: RunsBefore prerequisite did not start.");
        }

        if (afterStarted.Wait(TimeSpan.FromMilliseconds(150)))
        {
            throw new InvalidOperationException($"{owner}: RunsBefore dependent started before prerequisite completed.");
        }

        releaseBefore.Set();
        if (!afterStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: RunsBefore dependent did not start after prerequisite.");
        }

        ValidateOrder(owner, "RunsBefore", order, "before", "after");
    }

    private static void ValidateOrder(
        string owner,
        string label,
        ConcurrentQueue<string> order,
        string expectedFirst,
        string expectedSecond)
    {
        if (!order.TryDequeue(out var first) ||
            !order.TryDequeue(out var second) ||
            first != expectedFirst ||
            second != expectedSecond)
        {
            throw new InvalidOperationException($"{owner}: {label} order was not enforced.");
        }
    }

    private static void ValidateExactResourceWriteConflict(string owner, IHostScheduler scheduler)
    {
        using var firstStarted = new ManualResetEventSlim();
        using var releaseFirst = new ManualResetEventSlim();
        using var secondStarted = new ManualResetEventSlim();

        var first = new HostScheduledWork(
            $"{owner}.probe.resource_conflict.first",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.CanRunInParallel,
            _ =>
            {
                firstStarted.Set();
                if (!releaseFirst.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new InvalidOperationException($"{owner}: exact conflict first release timed out.");
                }
            });
        var second = new HostScheduledWork(
            $"{owner}.probe.resource_conflict.second",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.CanRunInParallel,
            _ => secondStarted.Set());

        if (!scheduler.TrySchedule(first))
        {
            throw new InvalidOperationException($"{owner}: exact conflict first work was rejected.");
        }

        if (!firstStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: exact conflict first work did not start.");
        }

        if (!scheduler.TrySchedule(second))
        {
            throw new InvalidOperationException($"{owner}: exact conflict second work was rejected.");
        }

        if (secondStarted.Wait(TimeSpan.FromMilliseconds(150)))
        {
            throw new InvalidOperationException($"{owner}: exact conflicting resource work ran concurrently.");
        }

        releaseFirst.Set();
        if (!secondStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: exact conflict second work did not complete.");
        }
    }

    private static void ValidateIndependentResourceWritesUseWorkerPool(string owner, IHostScheduler scheduler)
    {
        using var bothStarted = new CountdownEvent(2);
        using var releaseWorkers = new ManualResetEventSlim();

        for (var index = 0; index < 2; index++)
        {
            var resourceIndex = index;
            var work = new HostScheduledWork(
                $"{owner}.probe.resource_independent.{resourceIndex}",
                HostWorkPhase.Gameplay,
                HostWorkAccess.GameplayStateWrite,
                HostWorkScheduleFlags.CanRunInParallel,
                context =>
                {
                    if (context.ThreadRole != HostThreadRole.WorkerPool || context.WorkerIndex < 0)
                    {
                        throw new InvalidOperationException($"{owner}: independent resource work did not run on worker pool.");
                    }

                    bothStarted.Signal();
                    if (!releaseWorkers.Wait(TimeSpan.FromSeconds(5)))
                    {
                        throw new InvalidOperationException($"{owner}: independent resource release timed out.");
                    }
                });

            if (!scheduler.TrySchedule(work))
            {
                throw new InvalidOperationException($"{owner}: independent resource work was rejected.");
            }
        }

        if (!bothStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: independent resource writes did not run in parallel.");
        }

        releaseWorkers.Set();
    }

    private static void ValidateSerialScheduling(string owner, IHostScheduler scheduler)
    {
        using var firstStarted = new ManualResetEventSlim();
        using var releaseFirst = new ManualResetEventSlim();
        using var secondStarted = new ManualResetEventSlim();
        var order = new ConcurrentQueue<string>();

        var first = new HostScheduledWork(
            $"{owner}.probe.serial.first",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.DeterministicOrder | HostWorkScheduleFlags.RequiresTickBarrier,
            _ =>
            {
                order.Enqueue("first");
                firstStarted.Set();
                if (!releaseFirst.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new InvalidOperationException($"{owner}: serial first release timed out.");
                }
            });
        var second = new HostScheduledWork(
            $"{owner}.probe.serial.second",
            HostWorkPhase.Gameplay,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.DeterministicOrder | HostWorkScheduleFlags.RequiresTickBarrier,
            _ =>
            {
                order.Enqueue("second");
                secondStarted.Set();
            });

        if (!scheduler.TrySchedule(first) || !scheduler.TrySchedule(second))
        {
            throw new InvalidOperationException($"{owner}: serial work was rejected.");
        }

        if (!firstStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: serial first work did not start.");
        }

        if (secondStarted.Wait(TimeSpan.FromMilliseconds(150)))
        {
            throw new InvalidOperationException($"{owner}: serial second work started before first completed.");
        }

        releaseFirst.Set();
        if (!secondStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: serial second work did not start.");
        }

        if (!order.TryDequeue(out var firstOrder) ||
            !order.TryDequeue(out var secondOrder) ||
            firstOrder != "first" ||
            secondOrder != "second")
        {
            throw new InvalidOperationException($"{owner}: serial work order was not deterministic.");
        }
    }

    private static void ValidateNestedBlockingRunRejected(string owner, IHostScheduler scheduler)
    {
        using var completed = new ManualResetEventSlim();
        var frame = CreateFrame(77);
        var nested = new HostScheduledWork(
            $"{owner}.probe.nested_blocking_inner",
            HostWorkPhase.Validation,
            HostWorkAccess.None,
            HostWorkScheduleFlags.RequiresTickBarrier,
            _ => throw new InvalidOperationException($"{owner}: nested work should not execute."));
        var outer = new HostScheduledWork(
            $"{owner}.probe.nested_blocking_outer",
            HostWorkPhase.Validation,
            HostWorkAccess.None,
            HostWorkScheduleFlags.CanRunInParallel,
            _ =>
            {
                if (scheduler.TryRun(nested, frame))
                {
                    throw new InvalidOperationException($"{owner}: nested blocking TryRun was accepted.");
                }

                completed.Set();
            });

        if (!scheduler.TrySchedule(outer))
        {
            throw new InvalidOperationException($"{owner}: nested blocking probe was rejected.");
        }

        if (!completed.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: nested blocking probe did not complete.");
        }
    }

    private static void ValidateParallelCapacity(string owner, IHostScheduler scheduler)
    {
        using var allStarted = new CountdownEvent(scheduler.WorkerThreadCapacity);
        using var releaseWorkers = new ManualResetEventSlim();
        var workers = new ConcurrentDictionary<int, byte>();

        for (var index = 0; index < scheduler.WorkerThreadCapacity; index++)
        {
            var work = new HostScheduledWork(
                $"{owner}.probe.parallel_capacity.{index}",
                HostWorkPhase.Validation,
                HostWorkAccess.None,
                HostWorkScheduleFlags.CanRunInParallel,
                context =>
                {
                    if (context.ThreadRole != HostThreadRole.WorkerPool || context.WorkerIndex < 0)
                    {
                        throw new InvalidOperationException($"{owner}: parallel work did not run on worker pool.");
                    }

                    workers.TryAdd(context.WorkerIndex, 0);
                    allStarted.Signal();
                    if (!releaseWorkers.Wait(TimeSpan.FromSeconds(5)))
                    {
                        throw new InvalidOperationException($"{owner}: parallel capacity release timed out.");
                    }
                });

            if (!scheduler.TrySchedule(work))
            {
                throw new InvalidOperationException($"{owner}: parallel capacity work was rejected.");
            }
        }

        if (!allStarted.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException($"{owner}: worker pool did not start capacity probe.");
        }

        releaseWorkers.Set();
        if (workers.Count != scheduler.WorkerThreadCapacity)
        {
            throw new InvalidOperationException($"{owner}: worker pool did not use full capacity.");
        }
    }

    private static void ValidateUndeclaredWorkRejected(string owner, IHostScheduler scheduler)
    {
        var undeclared = new HostScheduledWork(
            $"{owner}.probe.undeclared",
            HostWorkPhase.Validation,
            HostWorkAccess.None,
            HostWorkScheduleFlags.None,
            _ => throw new InvalidOperationException($"{owner}: undeclared work executed."));
        if (scheduler.TrySchedule(undeclared) || scheduler.TryRun(undeclared, CreateFrame(88)))
        {
            throw new InvalidOperationException($"{owner}: undeclared work was accepted.");
        }

        var mismatchedPhase = new HostScheduledWork(
            $"{owner}.probe.blocking",
            HostWorkPhase.Gameplay,
            HostWorkAccess.None,
            HostWorkScheduleFlags.RequiresTickBarrier,
            _ => throw new InvalidOperationException($"{owner}: mismatched work executed."));
        if (scheduler.TrySchedule(mismatchedPhase) || scheduler.TryRun(mismatchedPhase, CreateFrame(89)))
        {
            throw new InvalidOperationException($"{owner}: mismatched declared work was accepted.");
        }

        var mismatchedAccess = new HostScheduledWork(
            $"{owner}.probe.blocking",
            HostWorkPhase.Validation,
            HostWorkAccess.GameplayStateWrite,
            HostWorkScheduleFlags.RequiresTickBarrier,
            _ => throw new InvalidOperationException($"{owner}: access-mismatched work executed."));
        if (scheduler.TrySchedule(mismatchedAccess) || scheduler.TryRun(mismatchedAccess, CreateFrame(90)))
        {
            throw new InvalidOperationException($"{owner}: access-mismatched declared work was accepted.");
        }
    }

    private static void ValidateDisposedScheduler(string owner, IDisposable disposableScheduler)
    {
        var scheduler = (IHostScheduler)disposableScheduler;
        disposableScheduler.Dispose();
        if (scheduler.IsWorkerPoolAvailable)
        {
            throw new InvalidOperationException($"{owner}: disposed scheduler still reports worker availability.");
        }

        var work = new HostScheduledWork(
            $"{owner}.probe.rejected_after_dispose",
            HostWorkPhase.Validation,
            HostWorkAccess.None,
            HostWorkScheduleFlags.None,
            _ => throw new InvalidOperationException($"{owner}: disposed scheduler executed work."));

        if (scheduler.TrySchedule(work) || scheduler.TryRun(work, CreateFrame(1)))
        {
            throw new InvalidOperationException($"{owner}: disposed scheduler accepted work.");
        }
    }

    private static HostFrameContext CreateFrame(ulong frameIndex)
    {
        return new HostFrameContext(
            1.0 / 60.0,
            frameIndex,
            default);
    }

    private static IReadOnlyList<ScheduledSystemDeclaration> ProbeDeclarations(string owner, int workerThreadCapacity)
    {
        var declarations = new List<ScheduledSystemDeclaration>
        {
            Declaration($"{owner}.probe.blocking", HostWorkPhase.Validation, HostWorkAccess.None, HostWorkScheduleFlags.RequiresTickBarrier),
            Declaration($"{owner}.probe.fire_and_forget", HostWorkPhase.Validation, HostWorkAccess.None, HostWorkScheduleFlags.None),
            Declaration($"{owner}.probe.failing_blocking", HostWorkPhase.Validation, HostWorkAccess.None, HostWorkScheduleFlags.RequiresTickBarrier),
            Declaration($"{owner}.probe.failing_fire_and_forget", HostWorkPhase.Validation, HostWorkAccess.None, HostWorkScheduleFlags.None),
            Declaration($"{owner}.probe.after_fire_and_forget_failure", HostWorkPhase.Validation, HostWorkAccess.None, HostWorkScheduleFlags.None),
            Declaration(
                $"{owner}.probe.serial.first",
                HostWorkPhase.Gameplay,
                HostWorkAccess.GameplayStateWrite,
                HostWorkScheduleFlags.DeterministicOrder | HostWorkScheduleFlags.RequiresTickBarrier),
            Declaration(
                $"{owner}.probe.serial.second",
                HostWorkPhase.Gameplay,
                HostWorkAccess.GameplayStateWrite,
                HostWorkScheduleFlags.DeterministicOrder | HostWorkScheduleFlags.RequiresTickBarrier),
            DeclarationWithOrdering(
                $"{owner}.probe.order.after.prerequisite",
                HostWorkPhase.Gameplay,
                HostWorkScheduleFlags.CanRunInParallel,
                runsAfter: [],
                runsBefore: []),
            DeclarationWithOrdering(
                $"{owner}.probe.order.after.dependent",
                HostWorkPhase.Gameplay,
                HostWorkScheduleFlags.CanRunInParallel,
                runsAfter: [$"{owner}.probe.order.after.prerequisite"],
                runsBefore: []),
            DeclarationWithOrdering(
                $"{owner}.probe.order.epoch.prerequisite",
                HostWorkPhase.Gameplay,
                HostWorkScheduleFlags.CanRunInParallel,
                runsAfter: [],
                runsBefore: []),
            DeclarationWithOrdering(
                $"{owner}.probe.order.epoch.dependent",
                HostWorkPhase.Gameplay,
                HostWorkScheduleFlags.CanRunInParallel,
                runsAfter: [$"{owner}.probe.order.epoch.prerequisite"],
                runsBefore: []),
            DeclarationWithOrdering(
                $"{owner}.probe.order.failed.prerequisite",
                HostWorkPhase.Gameplay,
                HostWorkScheduleFlags.CanRunInParallel,
                runsAfter: [],
                runsBefore: []),
            DeclarationWithOrdering(
                $"{owner}.probe.order.failed.dependent",
                HostWorkPhase.Gameplay,
                HostWorkScheduleFlags.CanRunInParallel,
                runsAfter: [$"{owner}.probe.order.failed.prerequisite"],
                runsBefore: []),
            DeclarationWithOrdering(
                $"{owner}.probe.order.before.before",
                HostWorkPhase.Gameplay,
                HostWorkScheduleFlags.CanRunInParallel,
                runsAfter: [],
                runsBefore: [$"{owner}.probe.order.before.after"]),
            DeclarationWithOrdering(
                $"{owner}.probe.order.before.after",
                HostWorkPhase.Gameplay,
                HostWorkScheduleFlags.CanRunInParallel,
                runsAfter: [],
                runsBefore: []),
            DeclarationWithWrites(
                $"{owner}.probe.barrier.parallel",
                HostWorkPhase.Gameplay,
                HostWorkScheduleFlags.CanRunInParallel,
                "octaryn.probe.barrier.parallel"),
            Declaration(
                $"{owner}.probe.barrier.commit",
                HostWorkPhase.Gameplay,
                HostWorkAccess.None,
                HostWorkScheduleFlags.RequiresTickBarrier),
            DeclarationWithWrites(
                $"{owner}.probe.resource_conflict.first",
                HostWorkPhase.Gameplay,
                HostWorkScheduleFlags.CanRunInParallel,
                "octaryn.probe.exact.shared"),
            DeclarationWithWrites(
                $"{owner}.probe.resource_conflict.second",
                HostWorkPhase.Gameplay,
                HostWorkScheduleFlags.CanRunInParallel,
                "octaryn.probe.exact.shared"),
            DeclarationWithWrites(
                $"{owner}.probe.resource_independent.0",
                HostWorkPhase.Gameplay,
                HostWorkScheduleFlags.CanRunInParallel,
                "octaryn.probe.exact.left"),
            DeclarationWithWrites(
                $"{owner}.probe.resource_independent.1",
                HostWorkPhase.Gameplay,
                HostWorkScheduleFlags.CanRunInParallel,
                "octaryn.probe.exact.right"),
            Declaration($"{owner}.probe.nested_blocking_inner", HostWorkPhase.Validation, HostWorkAccess.None, HostWorkScheduleFlags.RequiresTickBarrier),
            Declaration($"{owner}.probe.nested_blocking_outer", HostWorkPhase.Validation, HostWorkAccess.None, HostWorkScheduleFlags.CanRunInParallel)
        };

        for (var index = 0; index < workerThreadCapacity; index++)
        {
            declarations.Add(Declaration(
                $"{owner}.probe.parallel_capacity.{index}",
                HostWorkPhase.Validation,
                HostWorkAccess.None,
                HostWorkScheduleFlags.CanRunInParallel));
        }

        return declarations;
    }

    private static IReadOnlyList<ScheduledSystemDeclaration> UnresolvedProbeDeclarations(string owner)
    {
        return
        [
            DeclarationWithOrdering(
                $"{owner}.probe.shutdown_unresolved",
                HostWorkPhase.Gameplay,
                HostWorkScheduleFlags.CanRunInParallel,
                runsAfter: [$"{owner}.probe.shutdown_missing"],
                runsBefore: [])
        ];
    }

    private static ScheduledSystemDeclaration Declaration(
        string systemId,
        HostWorkPhase phase,
        HostWorkAccess access,
        HostWorkScheduleFlags flags)
    {
        return new ScheduledSystemDeclaration(
            systemId,
            phase,
            HostScheduleIds.FrameOrTickOwner,
            ReadsForAccess(access),
            WritesForAccess(access),
            [],
            [],
            flags,
            HostScheduleIds.FrameOrTickEndBarrier);
    }

    private static ScheduledSystemDeclaration DeclarationWithWrites(
        string systemId,
        HostWorkPhase phase,
        HostWorkScheduleFlags flags,
        params string[] resourceIds)
    {
        return new ScheduledSystemDeclaration(
            systemId,
            phase,
            HostScheduleIds.FrameOrTickOwner,
            [],
            resourceIds
                .Select(resourceId => new ScheduledResourceAccess(resourceId, ScheduledAccessMode.Write))
                .ToArray(),
            [],
            [],
            flags,
            HostScheduleIds.FrameOrTickEndBarrier);
    }

    private static ScheduledSystemDeclaration DeclarationWithOrdering(
        string systemId,
        HostWorkPhase phase,
        HostWorkScheduleFlags flags,
        IReadOnlyList<string> runsAfter,
        IReadOnlyList<string> runsBefore)
    {
        return new ScheduledSystemDeclaration(
            systemId,
            phase,
            HostScheduleIds.FrameOrTickOwner,
            [],
            [new ScheduledResourceAccess($"{systemId}.state", ScheduledAccessMode.Write)],
            runsAfter,
            runsBefore,
            flags,
            HostScheduleIds.FrameOrTickEndBarrier);
    }

    private static IReadOnlyList<ScheduledResourceAccess> ReadsForAccess(HostWorkAccess access)
    {
        var reads = new List<ScheduledResourceAccess>();
        if ((access & (HostWorkAccess.InputSnapshot | HostWorkAccess.FrameTimingSnapshot)) != 0)
        {
            reads.Add(new ScheduledResourceAccess(HostApiIds.Frame, ScheduledAccessMode.Read));
        }

        if ((access & HostWorkAccess.GameplayStateRead) != 0)
        {
            reads.Add(new ScheduledResourceAccess("octaryn.probe.state", ScheduledAccessMode.Read));
        }

        if ((access & HostWorkAccess.ContentRegistryRead) != 0)
        {
            reads.Add(new ScheduledResourceAccess("octaryn.probe.content.registry", ScheduledAccessMode.Read));
        }

        return reads;
    }

    private static IReadOnlyList<ScheduledResourceAccess> WritesForAccess(HostWorkAccess access)
    {
        var writes = new List<ScheduledResourceAccess>();
        if ((access & HostWorkAccess.GameplayStateWrite) != 0)
        {
            writes.Add(new ScheduledResourceAccess("octaryn.probe.state", ScheduledAccessMode.Write));
        }

        if ((access & HostWorkAccess.CommandSinkWrite) != 0)
        {
            writes.Add(new ScheduledResourceAccess(HostApiIds.Commands, ScheduledAccessMode.Write));
        }

        return writes;
    }
}
