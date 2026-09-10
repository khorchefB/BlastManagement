namespace BlastManagement.Api.Application.Abstractions;

public interface IEventProjection
{
    void Project(StoredEvent storedEvent);
}
