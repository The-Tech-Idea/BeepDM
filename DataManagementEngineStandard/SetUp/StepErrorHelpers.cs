using System;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;

#nullable enable

namespace TheTechIdea.Beep.SetUp
{
    public static class StepErrorHelpers
    {
        public static IErrorsInfo Ok(string msg = "Ok") =>
            new ErrorsInfo { Flag = Errors.Ok, Message = msg };

        public static IErrorsInfo Fail(string msg, Exception? ex = null) =>
            new ErrorsInfo { Flag = Errors.Failed, Message = msg, Ex = ex };

        public static void Report(System.IProgress<PassedArgs>? progress, int pct, string msg) =>
            progress?.Report(new PassedArgs { ParameterInt1 = pct, Messege = msg });

        public static void Report(System.IProgress<PassedArgs>? progress, int pct, string subStep, string msg) =>
            progress?.Report(new PassedArgs
            {
                ParameterInt1 = pct,
                Messege = $"[{subStep}] {msg}",
                ParameterString1 = subStep
            });
    }
}
