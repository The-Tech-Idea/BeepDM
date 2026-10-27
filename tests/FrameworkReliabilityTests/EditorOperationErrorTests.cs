using Moq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Tools;
using Xunit;

namespace TheTechIdea.Beep.Framework.Tests;

public sealed class EditorOperationErrorTests
{
    [Fact]
    public void Independent_datasource_scopes_can_finish_out_of_begin_order_without_restoring_a_completed_scope()
    {
        var (editor, _, shared) = Harness(); using var lifetime = editor;
        var first = new ErrorsInfo(); var second = new ErrorsInfo();
        var a = editor.BeginOperationErrors(first); var b = editor.BeginOperationErrors(second);
        try { a.Dispose(); Assert.Same(second, editor.ErrorObject); }
        finally { b.Dispose(); a.Dispose(); }
        Assert.Same(shared, editor.ErrorObject); b.Dispose();
    }
    private static (DMEEditor Editor, Mock<IDMLogger> Logger, ErrorsInfo Shared) Harness()
    {
        var logger = new Mock<IDMLogger>(); var shared = new ErrorsInfo { Flag = Errors.Ok, Message = "Shared state" };
        return (new DMEEditor(logger.Object, Mock.Of<IUtil>(), shared, Mock.Of<IConfigEditor>(), Mock.Of<IAssemblyHandler>()), logger, shared);
    }
    [Fact]
    public void Nested_diagnostic_scopes_restore_their_parent_without_mutating_shared_state()
    {
        var (editor, _, shared) = Harness(); using var lifetime = editor;
        var first = new ErrorsInfo(); var second = new ErrorsInfo();
        using (editor.BeginOperationErrors(first))
        {
            editor.AddLogMessage("Read", "First failure", DateTime.Now, 0, "fictional", Errors.Failed);
            using (editor.BeginOperationErrors(second))
            {
                editor.AddLogMessage("Read", "Second result", DateTime.Now, 0, "fictional", Errors.Ok);
                Assert.Same(second, editor.ErrorObject);
            }
            Assert.Same(first, editor.ErrorObject); Assert.Equal(Errors.Failed, first.Flag);
        }
        Assert.Same(shared, editor.ErrorObject); Assert.Equal(Errors.Ok, shared.Flag); Assert.Equal("Shared state", shared.Message);
    }
    [Fact]
    public async Task Restoring_an_inherited_scope_in_a_child_flow_does_not_dispose_the_parent_flow()
    {
        var (editor, _, shared) = Harness(); using var lifetime = editor; var own = new ErrorsInfo();
        var scope = editor.BeginOperationErrors(own);
        try { await Task.Run(() => scope.Dispose()); Assert.Same(own, editor.ErrorObject); }
        finally { scope.Dispose(); }
        Assert.Same(shared, editor.ErrorObject);
    }
    [Fact]
    public async Task Concurrent_operation_errors_cannot_replace_another_operations_result()
    {
        var (editor, _, shared) = Harness(); using var lifetime = editor;
        var first = new ErrorsInfo(); var second = new ErrorsInfo();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = Task.Run(async () =>
        {
            using var scope = editor.BeginOperationErrors(first); entered.SetResult(); await finish.Task;
            editor.AddLogMessage("Read", "First failure", DateTime.Now, 0, "fictional", Errors.Failed);
            Assert.Same(first, editor.ErrorObject);
        });
        await entered.Task;
        try
        {
            using var scope = editor.BeginOperationErrors(second);
            editor.AddLogMessage("Read", "Second success", DateTime.Now, 0, "fictional", Errors.Ok);
            Assert.Same(second, editor.ErrorObject);
        }
        finally { finish.SetResult(); await pending; }
        Assert.Equal(Errors.Failed, first.Flag); Assert.Equal(Errors.Ok, second.Flag); Assert.Same(shared, editor.ErrorObject);
    }
    [Fact]
    public void A_scoped_logger_fault_is_observable_without_poisoning_shared_outcomes()
    {
        var (editor, logger, shared) = Harness(); using var lifetime = editor;
        var fault = new IOException("Injected diagnostic fault"); logger.Setup(x => x.WriteLog(It.IsAny<string>())).Throws(fault);
        using (editor.BeginOperationErrors(new ErrorsInfo()))
            Assert.Same(fault, Assert.Throws<IOException>(() => editor.AddLogMessage("Read", "Failure", DateTime.Now, 0, "fictional", Errors.Failed)));
        Assert.Same(shared, editor.ErrorObject); Assert.Equal(Errors.Ok, shared.Flag);
    }
}
