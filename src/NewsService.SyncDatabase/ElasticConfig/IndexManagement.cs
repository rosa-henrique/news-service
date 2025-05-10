using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using NewsService.SyncDatabase.Models;

namespace NewsService.SyncDatabase.ElasticConfig;

public static class IndexManagement
{
    public static async Task CreateIndexIfNotExists(ElasticsearchClient client)
    {
        if (!(await client.Indices.ExistsAsync("news_state")).Exists)
        {
            var response = await client.Indices.CreateAsync<NewsState>("news_state", c => c
                .Mappings(m => m.Properties(p => p
                        .Keyword(k => k.Id)
                        .Text(t => t.TypeProcess)
                        .Text(t => t.Status)
                        .Nested(nameof(NewsState.Files),
                            new NestedProperty
                            {
                                Properties = new Properties(new Dictionary<PropertyName, IProperty>
                                {
                                    {
                                        nameof(NewsStateFile.FileId),
                                        new KeywordProperty()
                                    },
                                    {
                                        nameof(NewsStateFile.Status),
                                        new TextProperty()
                                    }
                                })
                            })
                    )
                )
            );

            if (!response.IsSuccess())
            {
                if (response.TryGetOriginalException(out var exception) && exception != null)
                {
                    throw exception;
                }

                exception = new Exception();

                exception.Data.Add("responseNewsState.ElasticsearchServerError?.Error",
                    response.ElasticsearchServerError?.Error);
                exception.Data.Add("responseNewsState.ElasticsearchServerError?.Status",
                    response.ElasticsearchServerError?.Status);
                throw exception;
            }
        }

        if (!(await client.Indices.ExistsAsync("news_state_audit")).Exists)
        {
            var response = await client.Indices.CreateAsync<NewsStateAudit>("news_state_audit", c => c
                .Mappings(m => m.Properties(p => p
                        .Keyword(k =>k.Id)
                        .Text(t => t.Status)
                        .Text(t => t.NewsId)
                        .Text(t => t.FileId)
                    )
                )
            );

            if (!response.IsSuccess())
            {
                if (response.TryGetOriginalException(out var exception) && exception != null)
                {
                    throw exception;
                }

                exception = new Exception();

                exception.Data.Add("responseNewsState.ElasticsearchServerError?.Error",
                    response.ElasticsearchServerError?.Error);
                exception.Data.Add("responseNewsState.ElasticsearchServerError?.Status",
                    response.ElasticsearchServerError?.Status);
                throw exception;
            }
        }
    }
}