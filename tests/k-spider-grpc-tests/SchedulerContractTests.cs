using Google.Protobuf;
using KSpider.Grpc.Scheduler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Grpc.Tests;

[TestClass]
public class SchedulerContractTests
{
    [TestMethod]
    public void TaskDispatchRoundTrips()
    {
        var message = new TaskDispatch
        {
            TaskId = "42",
            TaskType = "channel_round",
            TaskDetailJson = "{\"channel_id\":7}",
            DispatchToken = "token-1",
        };

        var parsed = TaskDispatch.Parser.ParseFrom(message.ToByteArray());

        Assert.AreEqual(message.TaskId, parsed.TaskId);
        Assert.AreEqual(message.TaskType, parsed.TaskType);
        Assert.AreEqual(message.TaskDetailJson, parsed.TaskDetailJson);
        Assert.AreEqual(message.DispatchToken, parsed.DispatchToken);
    }

    [TestMethod]
    public void NodeMessageKeepsRegisterPayload()
    {
        var message = new NodeMessage
        {
            Register = new Register
            {
                NodeId = "node-1",
                NodeType = "crawler",
                DeviceJson = "{\"cpu\":8}",
            },
        };

        var parsed = NodeMessage.Parser.ParseFrom(message.ToByteArray());

        Assert.AreEqual(NodeMessage.PayloadOneofCase.Register, parsed.PayloadCase);
        Assert.AreEqual("node-1", parsed.Register.NodeId);
        Assert.AreEqual("crawler", parsed.Register.NodeType);
    }

    [TestMethod]
    public void AdminMessageKeepsCancelPayload()
    {
        var message = new AdminMessage
        {
            TaskCancel = new TaskCancel
            {
                TaskId = "42",
                DispatchToken = "token-1",
            },
        };

        var parsed = AdminMessage.Parser.ParseFrom(message.ToByteArray());

        Assert.AreEqual(AdminMessage.PayloadOneofCase.TaskCancel, parsed.PayloadCase);
        Assert.AreEqual("42", parsed.TaskCancel.TaskId);
        Assert.AreEqual("token-1", parsed.TaskCancel.DispatchToken);
    }
}
