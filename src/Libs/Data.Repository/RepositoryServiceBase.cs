using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Data.Repository
{
    public class RepositoryServiceBase<TRepository, T, TQuery, TKey, TDto, TDetailedDto> : IService<T, TDto, TDetailedDto, TQuery, TKey>
        where TRepository : IRepository<T, TQuery, TKey>
        where T : class, IEntityKey<TKey>
        where TQuery : IPaginable
    {
        protected readonly TRepository repository;

        public RepositoryServiceBase(TRepository repository)
        {
            this.repository = repository;
        }

        public async Task<IEnumerable<TDto>> AllAsync()
        {
            //var list = await repository.AllInclude().ToListAsync();
            //return mapper.Map<List<TDto>>(list);
            throw new NotImplementedException();
        }

        public async Task<TDetailedDto> ByKey(TKey key)
        {
            //Stopwatch stopWatch = Stopwatch.StartNew();

            //Console.WriteLine("Begin request");
            //var item = await repository.ByKey(key).FirstOrDefaultAsync();
            //Console.WriteLine("Requested request" + stopWatch.Elapsed.TotalMilliseconds.ToString("0.0###"));

            //stopWatch = Stopwatch.StartNew();

            //var result = mapper.Map<TDetailedDto>(item);
            //Console.WriteLine("Mapped" + stopWatch.Elapsed.TotalMilliseconds.ToString("0.0###"));
            //return result;
            throw new NotImplementedException();
        }

        public async Task<bool> HasKey(TKey key)
        {
            return await repository.HasKey(key);
        }

        public async Task<PagedList<TDto>> ByQuery(TQuery query)
        {
            //var result = new PagedList<TDto>();
            //if (query?.Paging?.ReturnCount == true)
            //{
            //    var byQuery = repository.ByQuery(query);
            //    result.Total = await byQuery.CountAsync();
            //    result.Calculate(query.Paging);
            //}
            //query.Paging?.Calculate();
            //var list = await repository.ByQueryPaged(query).ToListAsync();
            //result.Result = mapper.Map<List<TDto>>(list);
            //return result;
            throw new NotImplementedException();
        }

        public async Task<T> AddAsync(TDto source)
        {
            //var entity = mapper.Map<T>(source);
            //await repository.AddAsync(entity);
            //return entity;
            throw new NotImplementedException();
        }

        public bool Update(TDto source)
        {
            //var entity = mapper.Map<T>(source);
            //return repository.Update(entity);
            throw new NotImplementedException();
        }

        public bool Remove(TDto source)
        {
            //var entity = mapper.Map<T>(source);
            //return repository.Remove(entity);
            throw new NotImplementedException();
        }

        public virtual async Task SaveChangesAsync()
        {
            await repository.SaveChangesAsync();
        }

        public virtual async Task<bool> RemoveAsync(TDto source)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAsync(TDto source)
        {
            throw new NotImplementedException();
        }
    }
}