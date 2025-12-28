
public enum WebserviceLocation
{
    Production = 0,
    ExternalQA = 1,
    InternalDev = 2,
    Localhost = 3,
}

public enum VersionCheckType
{
    None = 0,
    OK = 1,
    OutOfDate = 2,
    Failed = 3,
}

public enum DebugLevel
{
    Debug = 0,
    Warning = 1,
    Error = 2
}

public enum ConfigStatus
{
    Unconfigured,
    Configured,
    Error
}