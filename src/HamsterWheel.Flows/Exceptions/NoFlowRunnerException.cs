namespace HamsterWheel.Flows;

public class NoFlowRunnerException() : FlowException("No IFlowRunner registered in the service provider");
