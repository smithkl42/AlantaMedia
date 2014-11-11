namespace Alanta.Client.Media
{
    public interface IObjectPool<T> where T : class
    {
        T GetNext();
        void Recycle(T obj);
    }
}
