namespace Net.Code.ADONet.Extensions
{
    public interface IQueryGenerator
    {
        string Insert { get; }
        string Delete { get; }
        string Update { get; }
        string Select { get; }
        string SelectAll { get; }
        string Count { get; }
    }

}