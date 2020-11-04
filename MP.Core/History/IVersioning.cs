namespace MP.Core.History
{
    public interface IVersioning
    {
        int ID { get; set; }
    }

    public interface IVersioning<T> : IVersioning
    {
        bool HasChanges(T newVersion);
        void ApplyChanges(T newVersion);
        void CompareAndChange(T newVersion);
    }
}
