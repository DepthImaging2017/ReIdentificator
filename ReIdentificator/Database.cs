using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;
using MongoDB.Bson.Serialization.Attributes;

namespace ReIdentificator
{
    class Database
    {
        protected static IMongoClient _client;
        protected static IMongoDatabase _database;
        protected static String _dbname;
        protected static String _collectionName;

        public Database(string url, string dbname, string collectionname)
        {
            _client = new MongoClient(url);
            _dbname = dbname;
            _database = _client.GetDatabase(_dbname);
            _collectionName = collectionname;
        }

        public async void AddEntry(Individual obj, Action<ObjectId> callback)
        {
            var collection = _database.GetCollection<Individual>(_collectionName);
            await collection.InsertOneAsync(obj);
            Debug.WriteLine("Inserted Document");
            //Debug.WriteLine(obj.ID);
            //callback(obj.ID);
        }

        public async void UpdateEntry(string id, string key, float val, Action<Individual> callback)
        {
            var collection = _database.GetCollection<Individual>(_collectionName);
            var dbId = ObjectId.Parse(id);
            var filter = Builders<Individual>.Filter.Eq("_id", dbId);
            var update = Builders<Individual>.Update
                .Set(key, val);
            await collection.UpdateOneAsync(filter, update);
            using (IAsyncCursor<Individual> cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    IEnumerable<Individual> batch = cursor.Current;
                    foreach (Individual document in batch)
                    {
                        callback(document);
                    }
                }
            }
        }

        public async void GetAllEntries(Action<List<Individual>> callback)
        {
            List<Individual> results = new List<Individual>();
            var filter = new BsonDocument();
            var collection = _database.GetCollection<Individual>(Database._collectionName);
            using (IAsyncCursor<Individual> cursor = await collection.FindAsync(new BsonDocument()))
            {
                while (await cursor.MoveNextAsync())
                {
                    IEnumerable<Individual> batch = cursor.Current;
                    foreach (Individual document in batch)
                    {
                        results.Add(document);
                    }
                    callback(results);
                }
            }
        }

        public void DropDB()
        {
            _database.DropCollection(_collectionName);
            Debug.WriteLine("Dropped Collection");
        }
    }

    public class Individual
    {
        [BsonId]
        public ObjectId ID {get; set;}
        [BsonElement("face_age")]
        public float face_age { get; set; }
        [BsonElement("face_gender")]
        public bool face_gender { get; set; }
        [BsonElement("face_hair_bald")]
        public int face_hair_bald { get; set; }
        [BsonElement("face_hair_blonde")]
        public int face_hair_blonde { get; set; }
        [BsonElement("face_hair_black")]
        public int face_hair_black { get; set; }
        [BsonElement("face_hair_brown")]
        public int face_hair_brown { get; set; }
        [BsonElement("face_hair_red")]
        public int face_hair_red { get; set; }
        [BsonElement("face_glasses")]
        public string face_glasses { get; set; }

        [BsonElement("image_color_shoulderleft")]
        public int image_color_shoulderleft { get; set; }
        [BsonElement("image_color_shoulderright")]
        public int image_color_shoulderright { get; set; }
        [BsonElement("image_color_torso")]
        public int image_color_torso { get; set; }
        [BsonElement("image_color_shoulderhistogram")]
        public int[] image_color_shoulderhistogram { get; set; }
        [BsonElement("image_color_spinehistogram")]
        public int[] image_color_spinehistogram { get; set; }
        [BsonElement("image_areacount_armleft")]
        public int image_areacount_armleft { get; set; }
        [BsonElement("image_areacount_armright")]
        public int image_areacount_armright { get; set; }
        [BsonElement("image_areacount_legleft")]
        public int image_areacount_legleft { get; set; }
        [BsonElement("image_areacount_legright")]
        public int image_areacount_legright { get; set; }
        [BsonElement("image_wears_watch")]
        public bool image_wears_watch { get; set; }
        [BsonElement("image_wears_shorts")]
        public bool image_wears_shorts { get; set; }
        [BsonElement("image_wears_shirt")]
        public bool image_wears_shirt { get; set; }

        [BsonElement("height")] 
        public double height { get; set; }
        [BsonElement("torsoHeight")]
        public double torsoHeight { get; set; }
        [BsonElement("neckToSpineMid")]
        public double neckToSpineMid { get; set; }
        [BsonElement("spineMidToSpineBase")]
        public double spineMidToSpineBase { get; set; }
        [BsonElement("neckToLeftShoulder")]
        public double neckToLeftShoulder { get; set; }
        [BsonElement("neckToRightShoulder")]
        public double neckToRightShoulder { get; set; }
        [BsonElement("leftHipToSpineBase")]
        public double leftHipToSpineBase { get; set; }
        [BsonElement("rightHipToSpineBase")]
        public double rightHipToSpineBase { get; set; }
        [BsonElement("spineMidToLeftShoulder")]
        public double spineMidToLeftShoulder { get; set; }
        [BsonElement("spineMidToRightShoulder")]
        public double spineMidToRightShoulder { get; set; }
        [BsonElement("bodyWidth")]
        public double bodyWidth { get; set; }
        }
}
