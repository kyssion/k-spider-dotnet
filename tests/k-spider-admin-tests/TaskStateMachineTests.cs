using KSpider.Admin.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Admin.Tests;

[TestClass]
public sealed class TaskStateMachineTests
{
    [TestMethod]
    public void AdminCanMarkPushedAndPushFailed()
    {
        Assert.IsTrue(TaskStateMachine.CanTransition(
            AdminTaskStatus.Pushing, AdminTaskStatus.Pushed, TransitionActor.Admin));
        Assert.IsTrue(TaskStateMachine.CanTransition(
            AdminTaskStatus.Pushing, AdminTaskStatus.PushFailed, TransitionActor.Admin));
    }

    [TestMethod]
    public void AdminCanRedispatchFromFailedOrPushed()
    {
        Assert.IsTrue(TaskStateMachine.CanTransition(
            AdminTaskStatus.PushFailed, AdminTaskStatus.Pushing, TransitionActor.Admin));
        Assert.IsTrue(TaskStateMachine.CanTransition(
            AdminTaskStatus.RunFailed, AdminTaskStatus.Pushing, TransitionActor.Admin));
        Assert.IsTrue(TaskStateMachine.CanTransition(
            AdminTaskStatus.Pushed, AdminTaskStatus.Pushing, TransitionActor.Admin));
    }

    [TestMethod]
    public void AdminCanRedispatchRunningTaskAfterProbeFailure()
    {
        Assert.IsTrue(TaskStateMachine.CanTransition(
            AdminTaskStatus.Running, AdminTaskStatus.Pushing, TransitionActor.Admin));
    }

    [TestMethod]
    public void AdminCanCancelEveryNonTerminalStatus()
    {
        foreach (var status in new[]
                 {
                     AdminTaskStatus.Pushing,
                     AdminTaskStatus.PushFailed,
                     AdminTaskStatus.Pushed,
                     AdminTaskStatus.Running,
                     AdminTaskStatus.RunFailed,
                 })
        {
            Assert.IsTrue(TaskStateMachine.CanTransition(
                status, AdminTaskStatus.Cancelled, TransitionActor.Admin), status.ToString());
        }
    }

    [TestMethod]
    public void TerminalStatusCannotBeCancelledAgain()
    {
        Assert.IsFalse(TaskStateMachine.CanTransition(
            AdminTaskStatus.Success, AdminTaskStatus.Cancelled, TransitionActor.Admin));
        Assert.IsFalse(TaskStateMachine.CanTransition(
            AdminTaskStatus.Abandoned, AdminTaskStatus.Cancelled, TransitionActor.Admin));
    }

    [TestMethod]
    public void NodeCanStartFromPushingOrPushed()
    {
        Assert.IsTrue(TaskStateMachine.CanTransition(
            AdminTaskStatus.Pushing, AdminTaskStatus.Running, TransitionActor.Node));
        Assert.IsTrue(TaskStateMachine.CanTransition(
            AdminTaskStatus.Pushed, AdminTaskStatus.Running, TransitionActor.Node));
    }

    [TestMethod]
    public void NodeCanOnlyFinishFromRunning()
    {
        Assert.IsTrue(TaskStateMachine.CanTransition(
            AdminTaskStatus.Running, AdminTaskStatus.Success, TransitionActor.Node));
        Assert.IsTrue(TaskStateMachine.CanTransition(
            AdminTaskStatus.Running, AdminTaskStatus.RunFailed, TransitionActor.Node));
        Assert.IsFalse(TaskStateMachine.CanTransition(
            AdminTaskStatus.Pushed, AdminTaskStatus.Success, TransitionActor.Node));
    }
}
