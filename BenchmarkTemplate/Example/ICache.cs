namespace BenchmarkTemplate
{
    public interface ICache
    {
        int Get(string key);
        void Set(string key, int val);
    }
}
