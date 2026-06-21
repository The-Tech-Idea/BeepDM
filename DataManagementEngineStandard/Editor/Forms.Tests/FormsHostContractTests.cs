using System.Reflection;
using TheTechIdea.Beep.Editor.Forms.Hosts;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using Xunit;

namespace TheTechIdea.Beep.Editor.UOWManager.Tests;

public class FormsHostContractTests
{
    [Fact]
    public void IBeepFormsHost_ExposesCurrentBlockRecordIndex()
    {
        MethodInfo? method = typeof(IBeepFormsHost).GetMethod(
            "GetCurrentBlockRecordIndex",
            [typeof(string)]);

        Assert.NotNull(method);
        Assert.Equal(typeof(int), method.ReturnType);
    }

    [Fact]
    public void IBeepFormsHost_GetFieldValue_ReturnsNullableObject()
    {
        MethodInfo? method = typeof(IBeepFormsHost).GetMethod(
            "GetFieldValue",
            [typeof(string), typeof(string)]);

        Assert.NotNull(method);
        Assert.Equal(typeof(object), method.ReturnType);
        NullabilityInfo nullability = new NullabilityInfoContext().Create(method.ReturnParameter);
        Assert.Equal(NullabilityState.Nullable, nullability.ReadState);
    }

    [Fact]
    public void IBeepFormsHost_SetFieldValue_AcceptsNullableValue()
    {
        MethodInfo? method = typeof(IBeepFormsHost).GetMethod(
            "SetFieldValue",
            [typeof(string), typeof(string), typeof(object)]);

        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method.ReturnType);
        ParameterInfo valueParameter = method.GetParameters()[2];
        NullabilityInfo nullability = new NullabilityInfoContext().Create(valueParameter);
        Assert.Equal(NullabilityState.Nullable, nullability.ReadState);
    }

    [Fact]
    public void IUnitofWorksManager_GetAndSetFieldValue_UseNullableValueContract()
    {
        MethodInfo? getter = typeof(IUnitofWorksManager).GetMethod(
            "GetFieldValue",
            [typeof(object), typeof(string)]);
        MethodInfo? setter = typeof(IUnitofWorksManager).GetMethod(
            "SetFieldValue",
            [typeof(object), typeof(string), typeof(object)]);

        Assert.NotNull(setter);
        Assert.NotNull(getter);
        Assert.Equal(typeof(object), getter.ReturnType);

        NullabilityInfoContext nullabilityContext = new();
        NullabilityInfo getterNullability = nullabilityContext.Create(getter.ReturnParameter);
        ParameterInfo setterValueParameter = setter.GetParameters()[2];
        NullabilityInfo setterNullability = nullabilityContext.Create(setterValueParameter);

        Assert.Equal(NullabilityState.Nullable, getterNullability.ReadState);
        Assert.Equal(NullabilityState.Nullable, setterNullability.ReadState);
    }

    [Fact]
    public void IBeepFormsHost_ExposesIncrementOneThinHostOperations()
    {
        Type host = typeof(IBeepFormsHost);

        Assert.NotNull(host.GetMethod("GetBlockTriggers"));
        Assert.NotNull(host.GetMethod("ExecuteQueryByExampleAsync"));
        Assert.NotNull(host.GetMethod("SetMessage"));
        Assert.NotNull(host.GetMethod("ClearMessage"));
        Assert.NotNull(host.GetMethod("ShowAlertAsync"));
        Assert.NotNull(host.GetMethod("LockCurrentRecordAsync"));
        Assert.NotNull(host.GetMethod("CreateSavepoint"));
        Assert.NotNull(host.GetMethod("RollbackToSavepointAsync"));
        Assert.NotNull(host.GetMethod("NavigateBackAsync"));
        Assert.NotNull(host.GetMethod("SetBookmark"));
        Assert.NotNull(host.GetMethod("CreateTimer"));
        Assert.NotNull(host.GetMethod("GetNextSequence"));
        Assert.NotNull(host.GetMethod("CreateRecordGroup"));
        Assert.NotNull(host.GetMethod("CreateParameterList"));
        Assert.NotNull(host.GetMethod("CallFormAsync"));
        Assert.NotNull(host.GetMethod("SetGlobalVariable"));
        Assert.NotNull(host.GetMethod("PostMessage"));
        Assert.NotNull(host.GetMethod("SaveFormState"));
        Assert.NotNull(host.GetMethod("RestoreFormStateAsync"));
        Assert.NotNull(host.GetMethod("GetComputedValues"));
        Assert.NotNull(host.GetMethod("ExportBlockToJsonAsync"));
        Assert.NotNull(host.GetMethod("GetBlockStatus"));
        Assert.NotNull(host.GetEvent("FormMessageReceived"));
        Assert.NotNull(host.GetEvent("TimerFired"));
        Assert.NotNull(host.GetEvent("MessageRaised"));
        Assert.NotNull(host.GetEvent("MessageCleared"));
    }

    [Fact]
    public void AdvancedHostContract_DoesNotAcceptManagerInstances()
    {
        Type managerType = typeof(IUnitofWorksManager);

        MethodInfo[] featureMethods = typeof(IBeepFormsHost)
            .GetMethods()
            .Where(method => method.Name != "set_FormsManager")
            .ToArray();
        Type[] parameterTypes = featureMethods
            .SelectMany(method => method.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.DoesNotContain(parameterTypes,
            type => managerType.IsAssignableFrom(type));
    }

    [Fact]
    public void BlockAndFieldContracts_ExposeTriggerRelayAndQueryCriteria()
    {
        Assert.NotNull(typeof(IBlockView).GetMethod("RaiseTriggerExecuting"));
        Assert.NotNull(typeof(IBlockView).GetMethod("RaiseTriggerExecuted"));
        Assert.NotNull(typeof(IFieldPresenter).GetProperty("QueryValue"));
        Assert.Equal(
            typeof(QueryOperator),
            typeof(IFieldPresenter).GetProperty("QueryOperator")?.PropertyType);
        Assert.NotNull(typeof(IFieldPresenter).GetProperty("IsQueryEnabled"));
    }

    [Fact]
    public void ManagerContract_ExposesEngineOwnedRuntimeProviders()
    {
        Assert.Equal(
            typeof(ITimerManager),
            typeof(IUnitofWorksManager).GetProperty("Timers")?.PropertyType);
        Assert.Equal(
            typeof(ISequenceProvider),
            typeof(IUnitofWorksManager).GetProperty("Sequences")?.PropertyType);
    }

}
