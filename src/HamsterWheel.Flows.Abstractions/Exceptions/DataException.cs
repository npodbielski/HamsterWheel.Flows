namespace HamsterWheel.Flows;

//TODO: establish a way to mark all exceptions across all libraries as PlatformException without sharing base class for exception
public class DataException(string message) : Exception(message)
{
    
}