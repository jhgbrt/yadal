namespace Net.Code.ADONet
{
    public interface IMappingConvention
    {
        string FromDb(string s);
        string ToDb(string s);
        string Parameter(string s);
    }
}
