using MongoDB.Driver;
using MongoDB.Bson;

public class MongoDBHandler
{
    private readonly IMongoDatabase _database;

    public MongoDBHandler(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    // 어떤 부재의 데이터베이스 컬렉션을 반환할지 결정
    private IMongoCollection<BsonDocument> GetCollectionForElement(string elementName)
    {
        if (elementName.StartsWith("Beam", StringComparison.OrdinalIgnoreCase))
        {
            return _database.GetCollection<BsonDocument>("Beam_DB");
        }
        else if (elementName.StartsWith("Column", StringComparison.OrdinalIgnoreCase))
        {
            return _database.GetCollection<BsonDocument>("Column_DB");
        }
        throw new InvalidOperationException($"Unknown element type for '{elementName}'.");
    }

    // 전체 요소 이름 조회 (Beam + Column 모두 조회)
    public async Task<List<string>> GetAllElementNames()
    {
        var beamNames = await GetElementNamesFromCollection("Beam_DB");
        var columnNames = await GetElementNamesFromCollection("Column_DB");

        return beamNames.Concat(columnNames).ToList();
    }

    private async Task<List<string>> GetElementNamesFromCollection(string collectionName)
    {
        var collection = _database.GetCollection<BsonDocument>(collectionName);
        var elements = await collection.Find(FilterDefinition<BsonDocument>.Empty)
                                       .Project(Builders<BsonDocument>.Projection.Include("ElementName"))
                                       .ToListAsync();
        return elements.Select(e => e["ElementName"].AsString).ToList();
    }

    // 특정 요소의 속성 조회
    public async Task<List<ElementProperty>> GetElementPropertiesAsync(string elementName)
    {
        var collection = GetCollectionForElement(elementName);

        var filter = Builders<BsonDocument>.Filter.Eq("ElementName", elementName);
        var document = await collection.Find(filter).FirstOrDefaultAsync();

        if (document == null) return null;

        var properties = document["ElementProperty"].AsBsonArray
            .Select(bson => new ElementProperty
            {
                Name = bson["PropertyName"].AsString,
                Unit = bson["PropertyUnit"].AsString,
                Value = bson["PropertyValue"].ToDouble()
            }).ToList();

        return properties;
    }

    // 특정 요소의 속성 업데이트
    public async Task<bool> UpdateElementPropertyAsync(string elementName, string propertyName, double newValue)
    {
        var collection = GetCollectionForElement(elementName);

        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("ElementName", elementName),
            Builders<BsonDocument>.Filter.ElemMatch<BsonDocument>("ElementProperty", Builders<BsonDocument>.Filter.Eq("PropertyName", propertyName))
        );

        var update = Builders<BsonDocument>.Update.Set("ElementProperty.$.PropertyValue", newValue);
        var result = await collection.UpdateOneAsync(filter, update);

        return result.ModifiedCount > 0;
    }
}
