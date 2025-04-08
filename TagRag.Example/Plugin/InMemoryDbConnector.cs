using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serina.TagMemory.Interfaces;
using TagRag.Example.Db;


namespace TagRag.Example.Plugin
{
    public sealed class InMemoryDbConnector : IDbConnector
    {

        private ConversationContext db;

        public InMemoryDbConnector(ConversationContext db)
        {
            this.db = db;
        }

        public async Task<IEnumerable<dynamic>> ExecuteQueryAsync(string query)
        {
            var list = new List<dynamic>();

            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = query;

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var expando = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    expando[reader.GetName(i)] = reader.GetValue(i);
                }

                list.Add(expando);
            }

            return list;
        }

        public Task<bool> TestConnectionAsync()
        {
            return Task.FromResult(true);   
        }
    }
}
