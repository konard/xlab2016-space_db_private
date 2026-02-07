\*\*\*  SpaceDb  \*\*\*

Startup Project in Visual Studio: SpaceDb.csproj

Scripts to deploy and run necessary Docker containers for development and debugging:

* up-d.bat    - start main container
* up-db.bat   - container with PostgreSQL db (host:54410), MANDATORY
* up-redis.bat - bring up Redis container
* down.bat    - tear them down
* down-db.bat - tear down db
* down-redis.bat - tear down Redis

3rd-party containers:

* Elasticsearch (localhost:9200) - optional.

Necessary external connections:

Redis Stack - Docker images:

* redis/redis-stack contains both Redis Stack server and Redis Insight. This container is best for local development because you can use the embedded Redis Insight to visualize your data.
* redis/redis-stack-server provides Redis Stack server only. This container is best for production deployment.

Snippets for using Redis in the solution:

1. Inject RedisDictionary redisDictionary;
2. To write, use redisDictionary.Save(), redisDictionary.SaveCorrelation();
3. To read, use redisDictionary.Get(), redisDictionary.GetCorrelation();
