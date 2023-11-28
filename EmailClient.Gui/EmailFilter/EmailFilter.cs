using System.Collections.Generic;
using EmailClient.Database;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;

namespace EmailClient.Gui;

public class EmailFilter
{
    public static void ApplyFilters(
        IList<EmailEntry> emails,
        IEnumerable<Configuration.Filter> filters,
        // Extremely ugly, but it gets the job done...
        DbSet<Database.Filter> dbFilters
    )
    {
        var luceneVersion = LuceneVersion.LUCENE_48;

        var indexDir = System.IO.Directory.CreateTempSubdirectory("lucene-index");
        using (var directory = FSDirectory.Open(indexDir))
        {
            var analyzer = new StandardAnalyzer(luceneVersion);
            using (var writer = new IndexWriter(directory, new(luceneVersion, analyzer)))
            {
                for (int i = 0; i < emails.Count; ++i)
                {
                    var email = emails[i].Email;
                    Document doc = new();
                    if (email.From != null)
                        doc.AddStringField("from", email.From.ToString(), Field.Store.NO);
                    if (email.Subject != null)
                        doc.AddTextField("subject", email.Subject, Field.Store.NO);
                    if (email.TextBody != null)
                        doc.AddTextField("body", email.TextBody, Field.Store.NO);
                    doc.AddStoredField("index", i);
                    writer.AddDocument(doc);
                }
            }
            using (var reader = DirectoryReader.Open(directory))
            {
                var searcher = new IndexSearcher(reader);
                foreach (var filter in filters)
                {
                    var dbFilter = dbFilters.Find(filter.Folder);
                    if (dbFilter == null) continue;
                    Query query;
                    switch (filter.Type)
                    {
                        case Configuration.FilterType.From:
                        {
                            var boolQuery = new BooleanQuery();
                            foreach (var keyword in filter.Keywords)
                                boolQuery.Add(new TermQuery(new("from", keyword.ToLower())), Occur.SHOULD);
                            query = boolQuery;
                        }
                        break;

                        case Configuration.FilterType.Subject:
                        {
                            var boolQuery = new BooleanQuery();
                            foreach (var keyword in filter.Keywords)
                                boolQuery.Add(new TermQuery(new("subject", keyword.ToLower())), Occur.SHOULD);
                            query = boolQuery;
                        }
                        break;

                        case Configuration.FilterType.Content:
                        {
                            var boolQuery = new BooleanQuery();
                            foreach (var keyword in filter.Keywords)
                                boolQuery.Add(new TermQuery(new("content", keyword.ToLower())), Occur.SHOULD);
                            query = boolQuery;
                        }
                        break;

                        case Configuration.FilterType.Spam:
                        {
                            var boolQuery = new BooleanQuery();
                            foreach (var keyword in filter.Keywords)
                            {
                                boolQuery.Add(new TermQuery(new("subject", keyword.ToLower())), Occur.SHOULD);
                                boolQuery.Add(new TermQuery(new("content", keyword.ToLower())), Occur.SHOULD);
                            }
                            query = boolQuery;
                        }
                        break;

                        default:
                            throw new ApplicationException("Unknown filter type");
                    }
                    foreach (var match in searcher.Search(query, emails.Count).ScoreDocs)
                    {
                        var doc = searcher.Doc(match.Doc);
                        var index = (int)doc.GetField("index").GetInt32Value()!;

                        emails[index].Filters.Add(dbFilter);
                    }
                }
            }
        }

        System.IO.Directory.Delete(indexDir.FullName, true);
    }
}