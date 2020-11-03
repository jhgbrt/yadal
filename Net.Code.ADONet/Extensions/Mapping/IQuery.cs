namespace Net.Code.ADONet.Extensions.Mapping
{
    public interface IQuery
    {
        string Insert { get; }
        string Delete { get; }
        string Update { get; }
        string Select { get; }
        string SelectAll { get; }
        string Count { get; }
    }
}