namespace Data.Repository
{
    public interface IQueryBase<T>
    {
        Paging Paging { get; }
        public IFilter Filter { get; }
        public FilterOperator? FilterOperator { get; set; }
        SortBase<T> Sort { get; }
    }
}
