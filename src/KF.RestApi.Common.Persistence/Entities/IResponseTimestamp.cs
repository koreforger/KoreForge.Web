namespace KF.RestApi.Common.Persistence.Entities;

internal interface IResponseTimestamp
{
    DateTimeOffset ResponseTimestampUtc { get; }
}