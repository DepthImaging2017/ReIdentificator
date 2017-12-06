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
            Debug.WriteLine(obj.ID);
            callback(obj.ID);
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

    public class JointsData
    {
        [BsonElement("spineBase")]
        public float[] SpineBase { get; set; }
        [BsonElement("spineMid")]
        public float[] SpineMid { get; set; }
        [BsonElement("neck")]
        public float[] Neck { get; set; }
        [BsonElement("head")]
        public float[] Head { get; set; }
        [BsonElement("shoulderLeft")]
        public float[] ShoulderLeft { get; set; }
        [BsonElement("elbowLeft")]
        public float[] ElbowLeft { get; set; }
        [BsonElement("wristLeft")]
        public float[] WristLeft { get; set; }
        [BsonElement("handLeft")]
        public float[] HandLeft { get; set; }
        [BsonElement("shoulderRight")]
        public float[] ShoulderRight { get; set; }
        [BsonElement("wristRight")]
        public float[] WristRight { get; set; }
        [BsonElement("handRight")]
        public float[] HandRight { get; set; }
        [BsonElement("hipLeft")]
        public float[] HipLeft { get; set; }
        [BsonElement("kneeLeft")]
        public float[] KneeLeft { get; set; }
        [BsonElement("footLeft")]
        public float[] FootLeft { get; set; }
        [BsonElement("hipRight")]
        public float[] HipRight { get; set; }
        [BsonElement("kneeRight")]
        public float[] KneeRight { get; set; }
        [BsonElement("footRight")]
        public float[] FootRight { get; set; }
        [BsonElement("spineShoulder")]
        public float[] SpineShoulder { get; set; }
        [BsonElement("handTipLeft")]
        public float[] HandTipLeft { get; set; }
        [BsonElement("thumbLeft")]
        public float[] ThumbLeft { get; set; }
        [BsonElement("handTipRight")]
        public float[] HandTipRight { get; set; }
        [BsonElement("thumbRight")]
        public float[] ThumbRight { get; set; }
    }

    public class Individual
    {
        [BsonId]
        public ObjectId ID {get; set;}
        [BsonElement("x")]
        public float X { get; set; }
        [BsonElement("y")]
        public float Y { get; set; }
        [BsonElement("height")]
        public float Height { get; set; }
        [BsonElement("isHappy")]
        public string Is_Happy { get; set; }
        [BsonElement("isWearingGlasses")]
        public string Is_Wearing_Glasses { get; set; }
        [BsonElement("isMouthOpen")]
        public string Is_Mouth_Open { get; set; }
        [BsonElement("isMouthMoved")]
        public string Is_Mouth_Moved { get; set; }
        [BsonElement("pitch")]
        public float Pitch { get; set; }
        [BsonElement("yaw")]
        public float Yaw { get; set; }
        [BsonElement("roll")]
        public float Roll { get; set; }
        [BsonElement("leftArmLength")]
        public float Left_Arm_Length { get; set; }
        [BsonElement("rightArmLength")]
        public float Right_Arm_Length { get; set; }
        [BsonElement("shouldersLength")]
        public float Shoulders_Length { get; set; }
        [BsonElement("leftLegLength")]
        public float Left_Leg_Length { get; set; }
        [BsonElement("rightLegLength")]
        public float Right_Leg_Length { get; set; }
        [BsonElement("bodyLength")]
        public float Body_Length { get; set; }
        [BsonElement("bodyAngle")]
        public float Body_Angle { get; set; }
        [BsonElement("posture")]
        public string Posture { get; set; }
        public JointsData Joints_Data { get; set; }
    }
}
